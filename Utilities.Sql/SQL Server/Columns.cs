using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using Utilities.Core;

namespace Utilities.Sql.SqlServer
{
  public class Columns : List<Column>
  {
    private static Int32 _setNumber = 1;

    /// <summary>
    /// A parent might contain multiple instances of this class (e.g. StoredProcedure contains multiple Columns).
    /// This property automatically gives each set of Columns a unique number.
    /// </summary>
    public Int32 SetNumber { get; private set; }

    public String Name { get; set; }

    private String _targetLanguageIdentifier = null;
    /// <summary>
    /// This columns' name, converted to a valid identifier in the target language.
    /// </summary>
    public virtual String TargetLanguageIdentifier
    {
      get
      {
        if (this._targetLanguageIdentifier == null)
          this._targetLanguageIdentifier = IdentifierHelper.GetTargetLanguageIdentifier(this.Name);

        return this._targetLanguageIdentifier;
      }
    }

    private Columns()
      : base()
    {
      this.SetNumber = _setNumber++;
    }

    public Columns(Table table)
      : this()
    {
      this.Name = "";

      var sql = @"
;WITH foreign_keys_CTE (FOREIGN_KEY_TABLE, FOREIGN_KEY_COLUMN, PRIMARY_KEY_SCHEMA, PRIMARY_KEY_TABLE, PRIMARY_KEY_COLUMN)
AS
(
  SELECT
      FOREIGN_KEY_TABLE = OBJECT_NAME(FKC.parent_object_id),
      FOREIGN_KEY_COLUMN = C.NAME,
      PRIMARY_KEY_SCHEMA = OBJECT_SCHEMA_NAME(FKC.referenced_object_id),
      PRIMARY_KEY_TABLE = OBJECT_NAME(FKC.referenced_object_id),
      PRIMARY_KEY_COLUMN = CREF.NAME
    FROM
      sys.foreign_key_columns AS FKC
      INNER JOIN sys.columns AS C ON FKC.parent_column_id = C.column_id AND FKC.parent_object_id = c.object_id
      INNER JOIN sys.columns AS CREF ON FKC.referenced_column_id = CREF.column_id AND FKC.referenced_object_id = cref.object_id
),
primary_keys_CTE (OBJECT_ID, COLUMN_ID, PRIMARY_KEY_ORDINAL, PRIMARY_KEY_DIRECTION)
AS
(
  SELECT
      i.object_id,
      c.column_id,
      ic.key_ordinal,
      CASE
        WHEN ic.is_descending_key = 0 THEN 'ASC'
        ELSE 'DESC'
      END
    FROM
      sys.indexes AS i
      INNER JOIN sys.index_columns AS ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id and i.is_primary_key = 1
      INNER JOIN sys.columns AS C ON C.[object_id] = IC.[object_id] AND c.[column_id] = ic.column_id
),
column_type_CTE (USER_TYPE_ID, SERVER_DATATYPE_NAME, NATIVE_SERVER_DATATYPE_NAME)
AS
(
  SELECT
      T1.user_type_id,
      SERVER_DATATYPE_NAME = UPPER(T1.name),
      NATIVE_SERVER_DATATYPE_NAME = UPPER(COALESCE(T2.name, T1.name))
    FROM
      sys.types AS T1
      LEFT OUTER JOIN sys.types AS T2 ON T2.user_type_id = T1.system_type_id AND T1.is_table_type = 0
)
SELECT
  DISTINCT
    S.schema_id,
    [SCHEMA_NAME] = S.[name],
    TABLE_NAME = TBL.name,
    COLUMN_NAME = C.[name],
    COLUMN_ORDINAL = C.column_id - 1,
    C.user_type_id,
    C.system_type_id,
    SERVER_DATATYPE_NAME = CT_CTE.SERVER_DATATYPE_NAME,
    NATIVE_SERVER_DATATYPE_NAME = CT_CTE.NATIVE_SERVER_DATATYPE_NAME,
    PHYSICAL_LENGTH = C.max_length,
    C.[precision],
    C.scale,
    IS_NULLABLE = CASE C.is_nullable WHEN 0 THEN 'N' ELSE 'Y' END,
    IS_IDENTITY = CASE C.is_identity WHEN 0 THEN 'N' ELSE 'Y' END,
    IS_XML_DOCUMENT = CASE C.is_xml_document WHEN 0 THEN 'N' ELSE 'Y' END,
    XML_COLLECTION_NAME = COALESCE(XMLCOLL.name, ''),
    IS_PRIMARY_KEY = CASE WHEN (PK_CTE.primary_key_ordinal IS NULL) THEN 'N' ELSE 'Y' END,
    PRIMARY_KEY_ORDINAL = COALESCE(PK_CTE.primary_key_ordinal, -1),
    PRIMARY_KEY_DIRECTION = COALESCE(PK_CTE.PRIMARY_KEY_DIRECTION, ''),
    IS_FOREIGN_KEY = CASE WHEN (FK_CTE.foreign_key_table IS NULL) THEN 'N' ELSE 'Y' END,
    PRIMARY_KEY_SCHEMA = COALESCE(FK_CTE.primary_key_schema, ''),
    PRIMARY_KEY_TABLE = COALESCE(FK_CTE.primary_key_table, ''),
    PRIMARY_KEY_COLUMN = COALESCE(FK_CTE.primary_key_column, '')
  FROM
    sys.schemas AS S
    INNER JOIN sys.{0} AS TBL ON TBL.schema_id = S.schema_id
    INNER JOIN sys.columns AS C ON TBL.[object_id] = C.[object_id]
    LEFT OUTER JOIN sys.xml_schema_collections AS XMLCOLL ON XMLCOLL.xml_collection_id = C.xml_collection_id
    LEFT OUTER JOIN foreign_keys_CTE AS FK_CTE ON (FK_CTE.foreign_key_table = TBL.[name]) AND (FK_CTE.foreign_key_column = C.[name])
    LEFT OUTER JOIN primary_keys_CTE AS PK_CTE ON PK_CTE.object_id = TBL.object_id AND PK_CTE.column_id = C.column_id
    LEFT OUTER JOIN column_type_CTE AS CT_CTE ON CT_CTE.USER_TYPE_ID = C.user_type_id
  WHERE
    S.[schema_id] = SCHEMA_ID('{1}')
    AND TBL.[name] = '{2}';";

      var select = String.Format(sql, (table.IsView ? "views" : "tables"), table.Schema.Name, table.Name);
      var t = table.Schema.Database.Server.Configuration.Connection.GetDataSet(select).Tables[0];
      foreach (DataRow row in t.Rows)
      {
        var columnType = ColumnType.Unknown;

        if (row["IS_IDENTITY"].ToString().EqualsCI("Y"))
          columnType |= ColumnType.ID;

        if (row["IS_PRIMARY_KEY"].ToString().EqualsCI("Y"))
          columnType |= ColumnType.PrimaryKey;

        if (row["IS_FOREIGN_KEY"].ToString().EqualsCI("Y"))
          columnType |= ColumnType.ForeignKey;

        if (columnType == ColumnType.Unknown)
          columnType |= ColumnType.NonKeyAndNonID;

        var nativeServerDataTypeName = row["NATIVE_SERVER_DATATYPE_NAME"].ToString();

        if (nativeServerDataTypeName.NotEqualsCI("TIMESTAMP") && !columnType.HasFlag(ColumnType.ID))
          columnType |= (ColumnType.CanAppearInInsertStatement | ColumnType.CanAppearInUpdateSetClause);

        if (nativeServerDataTypeName != "XML")
          columnType |= ColumnType.CanAppearInSqlWhereClause;

        var physicalLength = Convert.ToInt32(row["PHYSICAL_LENGTH"]);

        this.Add(
          new Column(table, row["COLUMN_NAME"].ToString())
          {
            Ordinal = Convert.ToInt32(row["COLUMN_ORDINAL"]),
            ColumnType = columnType,
            ServerDataTypeName = row["SERVER_DATATYPE_NAME"].ToString(),
            NativeServerDataTypeName = nativeServerDataTypeName,
            PhysicalLength = physicalLength,
            LogicalLength = this.GetLogicalLength(nativeServerDataTypeName, physicalLength),
            Precision = Convert.ToInt32(row["PRECISION"]),
            Scale = Convert.ToInt32(row["SCALE"]),
            IsNullable = row["IS_NULLABLE"].ToString().EqualsCI("Y"),
            IsXmlDocument = row["IS_XML_DOCUMENT"].ToString().EqualsCI("Y"),
            XmlCollectionName = row["XML_COLLECTION_NAME"].ToString(),
            PrimaryKeyOrdinal = Convert.ToInt32(row["PRIMARY_KEY_ORDINAL"]),
            PrimaryKeyDirection = row["PRIMARY_KEY_DIRECTION"].ToString(),
            PrimaryKeySchema = row["PRIMARY_KEY_SCHEMA"].ToString(),
            PrimaryKeyTable = row["PRIMARY_KEY_TABLE"].ToString(),
            PrimaryKeyColumn = row["PRIMARY_KEY_COLUMN"].ToString()
          });
      }
    }

    public Columns(StoredProcedure storedProcedure, DataTable table)
      : this()
    {
      this.Name = String.Format("{0}_ResultSet_{1}", storedProcedure.TargetLanguageIdentifier, this.SetNumber);

      foreach (DataColumn column in table.Columns)
      {
        var columnType = ColumnType.NonKeyAndNonID;

        var nativeServerDataTypeName = this.GetSqlServerDataTypeFromDataColumn(column);

        if (nativeServerDataTypeName.NotEqualsCI("TIMESTAMP") && !columnType.HasFlag(ColumnType.ID))
          columnType |= (ColumnType.CanAppearInInsertStatement | ColumnType.CanAppearInUpdateSetClause);

        if (nativeServerDataTypeName != "XML")
          columnType |= ColumnType.CanAppearInSqlWhereClause;

        this.Add(
          new Column(storedProcedure, column.ColumnName)
          {
            Ordinal = column.Ordinal,
            ColumnType = columnType,
            ServerDataTypeName = nativeServerDataTypeName,
            NativeServerDataTypeName = nativeServerDataTypeName,
            PhysicalLength = column.MaxLength,
            LogicalLength = this.GetLogicalLength(nativeServerDataTypeName, column.MaxLength),
            Precision = 0,
            Scale = 0,
            IsNullable = false,
            IsXmlDocument = false,
            XmlCollectionName = "",
            PrimaryKeyOrdinal = -1,
            PrimaryKeyDirection = "",
            PrimaryKeySchema = "",
            PrimaryKeyTable = "",
            PrimaryKeyColumn = ""
          });
      }
    }

    public Columns(UserDefinedTableType userDefinedTableType)
      : this()
    {
      this.Name = "";

      var sql = @"
;WITH foreign_keys_CTE (FOREIGN_KEY_TABLE, FOREIGN_KEY_COLUMN, PRIMARY_KEY_SCHEMA, PRIMARY_KEY_TABLE, PRIMARY_KEY_COLUMN)
AS
(
  SELECT
      FOREIGN_KEY_TABLE = OBJECT_NAME(FKC.parent_object_id),
      FOREIGN_KEY_COLUMN = C.NAME,
      PRIMARY_KEY_SCHEMA = OBJECT_SCHEMA_NAME(FKC.referenced_object_id),
      PRIMARY_KEY_TABLE = OBJECT_NAME(FKC.referenced_object_id),
      PRIMARY_KEY_COLUMN = CREF.NAME
    FROM
      sys.foreign_key_columns AS FKC
      INNER JOIN sys.columns AS C ON FKC.parent_column_id = C.column_id AND FKC.parent_object_id = c.object_id
      INNER JOIN sys.columns AS CREF ON FKC.referenced_column_id = CREF.column_id AND FKC.referenced_object_id = cref.object_id
),
primary_keys_CTE (OBJECT_ID, COLUMN_ID, PRIMARY_KEY_ORDINAL, PRIMARY_KEY_DIRECTION)
AS
(
  SELECT
      i.object_id,
      c.column_id,
      ic.key_ordinal,
      CASE
        WHEN ic.is_descending_key = 0 THEN 'ASC'
        ELSE 'DESC'
      END
    FROM
      sys.indexes AS i
      INNER JOIN sys.index_columns AS ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id and i.is_primary_key = 1
      INNER JOIN sys.columns AS C ON C.[object_id] = IC.[object_id] AND c.[column_id] = ic.column_id
),
column_type_CTE (USER_TYPE_ID, SERVER_DATATYPE_NAME, NATIVE_SERVER_DATATYPE_NAME)
AS
(
  SELECT
      T1.user_type_id,
      SERVER_DATATYPE_NAME = UPPER(T1.name),
      NATIVE_SERVER_DATATYPE_NAME = UPPER(COALESCE(T2.name, T1.name))
    FROM
      sys.types AS T1
      LEFT OUTER JOIN sys.types AS T2 ON T2.user_type_id = T1.system_type_id AND T1.is_table_type = 0
)
SELECT
  DISTINCT
    TABLE_TYPE_NAME = TT.name,
    COLUMN_NAME = C.[name],
    COLUMN_ORDINAL = C.column_id - 1,
    C.user_type_id,
    C.system_type_id,
    SERVER_DATATYPE_NAME = CT_CTE.SERVER_DATATYPE_NAME,
    NATIVE_SERVER_DATATYPE_NAME = CT_CTE.NATIVE_SERVER_DATATYPE_NAME,
    PHYSICAL_LENGTH = C.max_length,
    C.[precision],
    C.scale,
    IS_NULLABLE = CASE C.is_nullable WHEN 0 THEN 'N' ELSE 'Y' END,
    IS_IDENTITY = CASE C.is_identity WHEN 0 THEN 'N' ELSE 'Y' END,
    IS_XML_DOCUMENT = CASE C.is_xml_document WHEN 0 THEN 'N' ELSE 'Y' END,
    XML_COLLECTION_NAME = COALESCE(XMLCOLL.name, ''),
    IS_PRIMARY_KEY = CASE WHEN (PK_CTE.primary_key_ordinal IS NULL) THEN 'N' ELSE 'Y' END,
    PRIMARY_KEY_ORDINAL = COALESCE(PK_CTE.primary_key_ordinal, -1),
    PRIMARY_KEY_DIRECTION = COALESCE(PK_CTE.PRIMARY_KEY_DIRECTION, ''),
    IS_FOREIGN_KEY = CASE WHEN (FK_CTE.foreign_key_table IS NULL) THEN 'N' ELSE 'Y' END,
    PRIMARY_KEY_SCHEMA = COALESCE(FK_CTE.primary_key_schema, ''),
    PRIMARY_KEY_TABLE = COALESCE(FK_CTE.primary_key_table, ''),
    PRIMARY_KEY_COLUMN = COALESCE(FK_CTE.primary_key_column, '')
  FROM
    sys.table_types AS TT
    INNER JOIN sys.columns AS C ON TT.type_table_object_id = C.object_id
    LEFT OUTER JOIN sys.xml_schema_collections AS XMLCOLL ON XMLCOLL.xml_collection_id = C.xml_collection_id
    LEFT OUTER JOIN foreign_keys_CTE AS FK_CTE ON (FK_CTE.foreign_key_table = TT.[name]) AND (FK_CTE.foreign_key_column = C.[name])
    LEFT OUTER JOIN primary_keys_CTE AS PK_CTE ON PK_CTE.object_id = TT.type_table_object_id AND PK_CTE.column_id = C.column_id
    LEFT OUTER JOIN column_type_CTE AS CT_CTE ON CT_CTE.USER_TYPE_ID = C.user_type_id
  WHERE
    TT.[name] = '{0}';";

      var select = String.Format(sql, userDefinedTableType.Name);
      var t = userDefinedTableType.Schema.Database.Server.Configuration.Connection.GetDataSet(select).Tables[0];
      foreach (DataRow row in t.Rows)
      {
        var columnType = ColumnType.Unknown;

        if (row["IS_IDENTITY"].ToString().EqualsCI("Y"))
          columnType |= ColumnType.ID;

        if (row["IS_PRIMARY_KEY"].ToString().EqualsCI("Y"))
          columnType |= ColumnType.PrimaryKey;

        if (row["IS_FOREIGN_KEY"].ToString().EqualsCI("Y"))
          columnType |= ColumnType.ForeignKey;

        if (columnType == ColumnType.Unknown)
          columnType |= ColumnType.NonKeyAndNonID;

        var nativeServerDataTypeName = row["NATIVE_SERVER_DATATYPE_NAME"].ToString();

        if (nativeServerDataTypeName.NotEqualsCI("TIMESTAMP") && !columnType.HasFlag(ColumnType.ID))
          columnType |= (ColumnType.CanAppearInInsertStatement | ColumnType.CanAppearInUpdateSetClause);

        if (nativeServerDataTypeName != "XML")
          columnType |= ColumnType.CanAppearInSqlWhereClause;

        var physicalLength = Convert.ToInt32(row["PHYSICAL_LENGTH"]);

        this.Add(
          new Column(userDefinedTableType, row["COLUMN_NAME"].ToString())
          {
            Ordinal = Convert.ToInt32(row["COLUMN_ORDINAL"]),
            ColumnType = columnType,
            ServerDataTypeName = row["SERVER_DATATYPE_NAME"].ToString(),
            NativeServerDataTypeName = nativeServerDataTypeName,
            PhysicalLength = physicalLength,
            LogicalLength = this.GetLogicalLength(nativeServerDataTypeName, physicalLength),
            Precision = Convert.ToInt32(row["PRECISION"]),
            Scale = Convert.ToInt32(row["SCALE"]),
            IsNullable = row["IS_NULLABLE"].ToString().EqualsCI("Y"),
            IsXmlDocument = row["IS_XML_DOCUMENT"].ToString().EqualsCI("Y"),
            XmlCollectionName = row["XML_COLLECTION_NAME"].ToString(),
            PrimaryKeyOrdinal = Convert.ToInt32(row["PRIMARY_KEY_ORDINAL"]),
            PrimaryKeyDirection = row["PRIMARY_KEY_DIRECTION"].ToString(),
            PrimaryKeySchema = row["PRIMARY_KEY_SCHEMA"].ToString(),
            PrimaryKeyTable = row["PRIMARY_KEY_TABLE"].ToString(),
            PrimaryKeyColumn = row["PRIMARY_KEY_COLUMN"].ToString()
          });
      }
    }

    private Int32 GetLogicalLength(String serverDataTypeName, Int32 maxLength)
    {
      if ((serverDataTypeName.EqualsCI("CHAR") || serverDataTypeName.StartsWithCI("VARCHAR")) && (maxLength > -1))
        return maxLength / 2;
      else
        return maxLength;
    }

    private String GetSqlServerDataTypeFromDataColumn(DataColumn column)
    {
      /* The convertable CLR types are those allowed in the DataColumn.DataType property
         (listed in the 'Remarks' section at https://msdn.microsoft.com/en-us/library/system.data.datacolumn.datatype%28v=vs.110%29.aspx).
      
         Most of these conversions are taken from the "SQL-CLR Type Mapping"
         page on MSDN (https://msdn.microsoft.com/en-us/library/bb386947.aspx). */

      if (column.DataType == typeof(Boolean))
        return "BIT";
      else if (column.DataType == typeof(Byte))
        return "TINYINT";
      else if (column.DataType == typeof(Byte[]))
        return "BINARY";
      else if (column.DataType == typeof(Char))
        return "NCHAR";
      else if (column.DataType == typeof(DateTime))
        return "DATETIME";
      else if (column.DataType == typeof(Decimal))
        return "DECIMAL(29, 4)";
      else if (column.DataType == typeof(Double))
        return "FLOAT";
      else if (column.DataType == typeof(Guid))
        return "UNIQUEIDENTIFIER";
      else if (column.DataType == typeof(Int16))
        return "SMALLINT";
      else if (column.DataType == typeof(Int32))
        return "INT";
      else if (column.DataType == typeof(Int64))
        return "BIGINT";
      else if (column.DataType == typeof(SByte))
        return "SMALLINT";
      else if (column.DataType == typeof(Single))
        return "REAL";
      else if (column.DataType == typeof(String))
        return "NVARCHAR";
      else if (column.DataType == typeof(TimeSpan))
        return "TIME";
      else if (column.DataType == typeof(UInt16))
        return "INT";
      else if (column.DataType == typeof(UInt32))
        return "BIGINT";
      else if (column.DataType == typeof(UInt64))
        return "DECIMAL(20)";
      else
        throw new ExceptionFmt("Unknown conversion from CLR type '{0}' to SQL Server type.", column.DataType.Name);
    }

    public List<String> GetCreateTableColumnDeclarations()
    {
      return
        this
        .OrderByDescending(column => column.ColumnType.HasFlag(ColumnType.ID))
        .ThenByDescending(column => column.ColumnType.HasFlag(ColumnType.PrimaryKey))
        .ThenBy(column => column.PrimaryKeyOrdinal)
        .Select(column => column.GetCreateTableColumnDeclaration())
        .ToList();
    }

    /// <summary>
    /// Return a list of strings that can be used as a parameter declarations in a stored procedure.
    /// <para>Primary and foreign key columns may optionally be documented with a comment.</para>
    /// </summary>
    /// <example>
    /// Executing "String.Join("," + Environment.NewLine, columns.GetStoredProcedureParameters())"
    /// on the AdventureWorks2012 Person.Person table will generate this string:
    /// <code>
    /// @BusinessEntityID INT  /* primary key 1, foreign key */,
    /// @AdditionalContactInfo XML(CONTENT, AdditionalContactInfoSchemaCollection),
    /// @Demographics XML(CONTENT, IndividualSurveySchemaCollection),
    /// @EmailPromotion INT,
    /// @FirstName NAME,
    /// @LastName NAME,
    /// @MiddleName NAME,
    /// @ModifiedDate DATETIME,
    /// @NameStyle NAMESTYLE,
    /// @PersonType NCHAR(2),
    /// @rowguid UNIQUEIDENTIFIER,
    /// @Suffix NVARCHAR(10),
    /// @Title NVARCHAR(8)
    /// </code>
    /// </example>
    /// <param name="columnType">An enum value indicating what kind of column type(s) to include in the return value.</param>
    /// <param name="includeKeyIdentificationComment">An enum value indicating whether or not to include
    /// a comment identifying the column as a primary and/or foreign key (see example code).</param>
    /// <returns>A <see cref="System.Collections.Generic.List{T}">List&lt;String&gt;</see>.</returns>
    public List<String> GetStoredProcedureParameters(ColumnType columnType, IncludeKeyIdentificationComment includeKeyIdentificationComment = IncludeKeyIdentificationComment.Yes)
    {
      return
        this
        .Where(column => (column.ColumnType & columnType) > 0)
        .OrderByDescending(column => column.ColumnType.HasFlag(ColumnType.ID))
        .ThenByDescending(column => column.ColumnType.HasFlag(ColumnType.PrimaryKey))
        .ThenBy(column => column.PrimaryKeyOrdinal)
        .Select(column => column.GetStoredProcedureParameterDeclaration(includeKeyIdentificationComment))
        .ToList();
    }

    /// <summary>
    /// Return a list of strings which contain primary key column names, along with their direction -
    /// ASC for ascending, or DESC for descending.
    /// </summary>
    /// <example>
    /// Executing "String.Join("," + Environment.NewLine, columns.GetTSQLPrimaryKeyColumns())"
    /// on the AdventureWorks2012 [HumanResources].[EmployeeDepartmentHistory] table will generate this string:
    /// <code>
    /// [BusinessEntityID] ASC,
    /// [DepartmentID] ASC,
    /// [ShiftID] ASC,
    /// [StartDate] ASC
    /// </code>
    /// </example>
    /// <returns>A <see cref="System.Collections.Generic.List{T}">List&lt;String&gt;</see>.</returns>
    public List<String> GetTSQLPrimaryKeyColumns()
    {
      return
        this
        .Where(column => column.ColumnType.HasFlag(ColumnType.PrimaryKey))
        .OrderBy(column => column.PrimaryKeyOrdinal)
        .Select(column => String.Format("{0} {1}", column.BracketedName, column.PrimaryKeyDirection))
        .ToList();
    }

    /// <summary>
    /// Returns a list of strings that can be used in the SELECT clause of an SQL Server SELECT statement.
    /// </summary>
    /// <example>
    /// Executing "String.Join("," + Environment.NewLine, columns.GetSelectColumnList("T"))"
    /// on the AdventureWorks2012 Person.Person table will generate this string:
    /// <code>
    /// T.[AdditionalContactInfo],
    /// T.[BusinessEntityID],
    /// T.[Demographics],
    /// T.[EmailPromotion],
    /// T.[FirstName],
    /// T.[LastName],
    /// T.[MiddleName],
    /// T.[ModifiedDate],
    /// T.[NameStyle],
    /// T.[PersonType],
    /// T.[rowguid],
    /// T.[Suffix],
    /// T.[Title]
    /// </code>
    /// </example>
    /// <param name="tableAlias">An optional string representing a table alias that will be prepended to each column name.</param>
    /// <returns>A <see cref="System.Collections.Generic.List{T}">List&lt;String&gt;</see>.</returns>
    public List<String> GetSelectColumnList(String tableAlias = "")
    {
      return
        this
        .OrderBy(column => column.Name)
        .Select(column => String.Format("{0}{1}", (String.IsNullOrWhiteSpace(tableAlias) ? "" : tableAlias.Trim() + "."), column.BracketedName))
        .ToList();
    }

    private IEnumerable<Column> GetOrderedListBasedOnColumnType(ColumnType columnType)
    {
      return
        this
        .Where(column => (column.ColumnType & columnType) > 0)
        .OrderBy(column => column.Name);
    }

    /// <summary>
    /// Returns a list of strings that can be used in the SELECT statement of a MERGE statement's CTE USING clause.
    /// </summary>
    /// <returns>A <see cref="System.Collections.Generic.List{T}">List&lt;String&gt;</see>.</returns>
    public List<String> GetMergeSelectList()
    {
      return
        this
        .GetOrderedListBasedOnColumnType(ColumnType.CanAppearInMergeSelectList)
        .Select(column => String.Format("{0} = {1}", column.BracketedName, column.SqlIdentifier))
        .ToList();
    }

    /// <summary>
    /// Returns a list of strings that can be used in column matching logic of a MERGE statement's CTE USING clause.
    /// </summary>
    /// <returns>A <see cref="System.Collections.Generic.List{T}">List&lt;String&gt;</see>.</returns>
    public List<String> GetMergeTargetAndSourceMatchingExpressions()
    {
      return
        this
        .Where(column => column.ColumnType.HasFlag(ColumnType.PrimaryKey))
        .OrderBy(column => column.PrimaryKeyOrdinal)
        .Select(column => String.Format("Target.{0} = Source.{0}", column.BracketedName))
        .ToList();
    }

    /// <summary>
    /// Returns a list of strings that can be used in the UPDATE statement of a MERGE statement.
    /// </summary>
    /// <returns>A <see cref="System.Collections.Generic.List{T}">List&lt;String&gt;</see>.</returns>
    public List<String> GetMergeUpdateColumnList()
    {
      return
        this
        .GetOrderedListBasedOnColumnType(ColumnType.CanAppearInUpdateSetClause)
        .Select(column => String.Format("{0} = Source.{0}", column.BracketedName))
        .ToList();
    }

    /// <summary>
    /// Returns a list of strings that can be used in the INSERT statement's VALUE clause of a MERGE statement.
    /// </summary>
    /// <returns>A <see cref="System.Collections.Generic.List{T}">List&lt;String&gt;</see>.</returns>
    public List<String> GetMergeInsertValueList()
    {
      return
        this
        .GetOrderedListBasedOnColumnType(ColumnType.CanAppearInInsertStatement)
        .Select(column => String.Format("Source.{0}", column.BracketedName))
        .ToList();
    }

    /// <summary>
    /// Returns a list of strings that can be used in the column list of an SQL Server INSERT statement.
    /// </summary>
    /// <example>
    /// Executing "String.Join("," + Environment.NewLine, columns.GetInsertColumnList())"
    /// on the AdventureWorks2012 Person.Person table will generate this string:
    /// <code>
    /// [AdditionalContactInfo],
    /// [BusinessEntityID],
    /// [Demographics],
    /// [EmailPromotion],
    /// [FirstName],
    /// [LastName],
    /// [MiddleName],
    /// [ModifiedDate],
    /// [NameStyle],
    /// [PersonType],
    /// [rowguid],
    /// [Suffix],
    /// [Title]
    /// </code>
    /// </example>
    /// <returns>A <see cref="System.Collections.Generic.List{T}">List&lt;String&gt;</see>.</returns>
    public List<String> GetInsertColumnList()
    {
      return
        this
        .GetOrderedListBasedOnColumnType(ColumnType.CanAppearInInsertStatement)
        .Select(column => String.Format("{0}", column.BracketedName))
        .ToList();
    }

    /// <summary>
    /// Returns a list of strings that can be used in the VALUES clause of an SQL Server INSERT statement.
    /// </summary>
    /// <example>
    /// Executing "String.Join("," + Environment.NewLine, columns.GetInsertValuesList())"
    /// on the AdventureWorks2012 Person.Person table will generate this string:
    /// <code>
    /// @AdditionalContactInfo,
    /// @BusinessEntityID,
    /// @Demographics,
    /// @EmailPromotion,
    /// @FirstName,
    /// @LastName,
    /// @MiddleName,
    /// @ModifiedDate,
    /// @NameStyle,
    /// @PersonType,
    /// @rowguid,
    /// @Suffix,
    /// @Title
    /// </code>
    /// </example>
    /// <returns>A <see cref="System.Collections.Generic.List{T}">List&lt;String&gt;</see>.</returns>
    public List<String> GetInsertValuesList()
    {
      return
        this
        .GetOrderedListBasedOnColumnType(ColumnType.CanAppearInInsertStatement)
        .Select(column => String.Format("{0}", column.SqlIdentifier))
        .ToList();
    }

    /// <summary>
    /// Returns a list of strings that can be used in the SET clause of an SQL Server UPDATE statement.
    /// </summary>
    /// <example>
    /// Executing "String.Join("," + Environment.NewLine, columns.GetUpdateColumnList())"
    /// on the AdventureWorks2012 Person.Person table will generate this string:
    /// <code>
    /// [AdditionalContactInfo] = @AdditionalContactInfo,
    /// [BusinessEntityID] = @BusinessEntityID,
    /// [Demographics] = @Demographics,
    /// [EmailPromotion] = @EmailPromotion,
    /// [FirstName] = @FirstName,
    /// [LastName] = @LastName,
    /// [MiddleName] = @MiddleName,
    /// [ModifiedDate] = @ModifiedDate,
    /// [NameStyle] = @NameStyle,
    /// [PersonType] = @PersonType,
    /// [rowguid] = @rowguid,
    /// [Suffix] = @Suffix,
    /// [Title] = @Title
    /// </code>
    /// </example>
    /// <returns>A <see cref="System.Collections.Generic.List{T}">List&lt;String&gt;</see>.</returns>
    public List<String> GetUpdateColumnList()
    {
      return
        this
        .GetOrderedListBasedOnColumnType(ColumnType.CanAppearInUpdateSetClause)
        .Select(column => String.Format("{0} = {1}", column.BracketedName, column.SqlIdentifier))
        .ToList();
    }

    /// <summary>
    /// Returns a list of strings that can be used in the SET clause of an SQL Server UPDATE statement.
    /// </summary>
    /// <example>
    /// Executing "String.Join(Environment.NewLine + "AND ", table.Columns.GetWhereClauseColumnList("T"))"
    /// on the AdventureWorks2012 Person.Person table will generate this string:
    /// <code>
    /// T.[BusinessEntityID] = @BusinessEntityID
    /// AND T.[EmailPromotion] = @EmailPromotion
    /// AND T.[FirstName] = @FirstName
    /// AND T.[LastName] = @LastName
    /// AND T.[MiddleName] = @MiddleName
    /// AND T.[ModifiedDate] = @ModifiedDate
    /// AND T.[NameStyle] = @NameStyle
    /// AND T.[PersonType] = @PersonType
    /// AND T.[rowguid] = @rowguid
    /// AND T.[Suffix] = @Suffix
    /// AND T.[Title] = @Title;
    /// </code>
    /// </example>
    /// <param name="columnType">An enum value indicating what kind of column type(s) to include in the return value.</param>
    /// <param name="tableAlias">An optional string representing a table alias that will be prepended to each column name.</param>
    /// <returns>A <see cref="System.Collections.Generic.List{T}">List&lt;String&gt;</see>.</returns>
    public List<String> GetWhereClauseColumnList(ColumnType columnType, String tableAlias = "")
    {
      return
        this
        .Where(column => column.ColumnType.HasFlag(ColumnType.CanAppearInSqlWhereClause))
        .Where(column => (column.ColumnType & columnType) > 0)
        .OrderBy(column => column.Name)
        .Select(column => column.GetSqlWhereClause(tableAlias))
        .ToList();
    }

    /// <summary>
    /// Returns a list of strings that represent a set of class property declarations in the specified target language,
    /// one for each column.
    /// </summary>
    /// <example>
    /// Assuming the configuration's XmlSystem is set to Linq_XDocument, and TargetLanguage is set to CSharp,
    /// executing "String.Join(" { get; set; }" + Environment.NewLine + "    ", table.Columns.GetClassPropertyDeclarations("public")) + " { get; set; }""
    /// on the AdventureWorks2012 Person.Person table will generate this string:
    /// <code>
    /// public System.Int32 BusinessEntityID  /* primary key 1, foreign key */ { get; set; }
    /// public System.Xml.Linq.XElement AdditionalContactInfo { get; set; }
    /// public System.Xml.Linq.XElement Demographics { get; set; }
    /// public System.Int32 EmailPromotion { get; set; }
    /// public System.String FirstName { get; set; }
    /// public System.String LastName { get; set; }
    /// public System.String MiddleName { get; set; }
    /// public System.DateTime ModifiedDate { get; set; }
    /// public System.Boolean NameStyle { get; set; }
    /// public System.String PersonType { get; set; }
    /// public System.Guid rowguid { get; set; }
    /// public System.String Suffix { get; set; }
    /// public System.String Title { get; set; }
    /// </code>
    /// </example>
    /// <param name="scope">A target language keyword indicating the scope of the class property declarations. Can be blank.</param>
    /// <param name="includeKeyIdentificationComment">An enum value indicating whether or not to include
    /// a comment identifying the column as a primary and/or foreign key (see example code).</param>
    /// <returns>A <see cref="System.Collections.Generic.List{T}">List&lt;String&gt;</see>.</returns>
    public List<String> GetClassPropertyDeclarations(String scope, IncludeKeyIdentificationComment includeKeyIdentificationComment = IncludeKeyIdentificationComment.Yes)
    {
      return
        this
        .OrderByDescending(column => column.ColumnType.HasFlag(ColumnType.ID))
        .ThenByDescending(column => column.ColumnType.HasFlag(ColumnType.PrimaryKey))
        .ThenBy(column => column.PrimaryKeyOrdinal)
        .ThenByDescending(column => column.ColumnType.HasFlag(ColumnType.ForeignKey))
        .Select(column => column.GetTargetLanguageProperty(scope, includeKeyIdentificationComment))
        .ToList();
    }

    /// <summary>
    /// Returns a list of strings that represent a set of method parameter declarations in the specified target language,
    /// one for each column.
    /// </summary>
    /// <example>
    /// Assuming the configuration's XmlSystem is set to Linq_XDocument, and TargetLanguage is set to CSharp,
    /// executing "String.Join("," + Environment.NewLine, table.Columns.GetTargetLanguageMethodParameterNamesAndTypes())"
    /// on the AdventureWorks2012 Person.Person table will generate this string:
    /// <code>
    /// System.Int32 BusinessEntityID  /* primary key 1, foreign key */,
    /// System.Xml.Linq.XElement AdditionalContactInfo,
    /// System.Xml.Linq.XElement Demographics,
    /// System.Int32 EmailPromotion,
    /// System.String FirstName,
    /// System.String LastName,
    /// System.String MiddleName,
    /// System.DateTime ModifiedDate,
    /// System.Boolean NameStyle,
    /// System.String PersonType,
    /// System.Guid rowguid,
    /// System.String Suffix,
    /// System.String Title
    /// </code>
    /// </example>
    /// <param name="columnType">An enum value indicating what kind of column type(s) to include in the return value.</param>
    /// <param name="includeKeyIdentificationComment">An enum value indicating whether or not to include
    /// a comment identifying the column as a primary and/or foreign key (see example code).</param>
    /// <returns>A <see cref="System.Collections.Generic.List{T}">List&lt;String&gt;</see>.</returns>
    public List<String> GetTargetLanguageMethodIdentifiersAndTypes(ColumnType columnType, IncludeKeyIdentificationComment includeKeyIdentificationComment = IncludeKeyIdentificationComment.Yes)
    {
      return
        this
        .Where(column => (column.ColumnType & columnType) > 0)
        .OrderByDescending(column => column.ColumnType.HasFlag(ColumnType.ID))
        .ThenByDescending(column => column.ColumnType.HasFlag(ColumnType.PrimaryKey))
        .ThenBy(column => column.PrimaryKeyOrdinal)
        .ThenByDescending(column => column.ColumnType.HasFlag(ColumnType.ForeignKey))
        .Select(column => column.GetTargetLanguageMethodParameterNameAndType(includeKeyIdentificationComment))
        .ToList();
    }

    /// <summary>
    /// Returns a list of strings that represent a set of SqlParameter constructor declarations in the specified target language,
    /// one for each column.
    /// </summary>
    /// <example>
    /// Assuming the configuration's XmlSystem is set to Linq_XDocument, and TargetLanguage is set to CSharp,
    /// executing ""command.Parameters.Add(" + String.Join(");" + Environment.NewLine + "command.Parameters.Add(", table.Columns.GetTargetLanguageSqlParameterText()) + ");""
    /// on the AdventureWorks2012 Person.Person table will generate this string:
    /// <code>
    /// command.Parameters.Add(new SqlParameter() { ParameterName = "@BusinessEntityID", SqlDbType = System.Data.SqlDbType.Int, Value = BusinessEntityID }  /* primary key 1, foreign key */);
    /// command.Parameters.Add(new SqlParameter() { ParameterName = "@AdditionalContactInfo", SqlDbType = System.Data.SqlDbType.Xml, Value = AdditionalContactInfo.GetSqlXml() });
    /// command.Parameters.Add(new SqlParameter() { ParameterName = "@Demographics", SqlDbType = System.Data.SqlDbType.Xml, Value = Demographics.GetSqlXml() });
    /// command.Parameters.Add(new SqlParameter() { ParameterName = "@EmailPromotion", SqlDbType = System.Data.SqlDbType.Int, Value = EmailPromotion });
    /// command.Parameters.Add(new SqlParameter() { ParameterName = "@FirstName", SqlDbType = System.Data.SqlDbType.NVarChar, Value = FirstName });
    /// command.Parameters.Add(new SqlParameter() { ParameterName = "@LastName", SqlDbType = System.Data.SqlDbType.NVarChar, Value = LastName });
    /// command.Parameters.Add(new SqlParameter() { ParameterName = "@MiddleName", SqlDbType = System.Data.SqlDbType.NVarChar, Value = MiddleName });
    /// command.Parameters.Add(new SqlParameter() { ParameterName = "@ModifiedDate", SqlDbType = System.Data.SqlDbType.DateTime, Value = ModifiedDate });
    /// command.Parameters.Add(new SqlParameter() { ParameterName = "@NameStyle", SqlDbType = System.Data.SqlDbType.Bit, Value = NameStyle });
    /// command.Parameters.Add(new SqlParameter() { ParameterName = "@PersonType", SqlDbType = System.Data.SqlDbType.NChar, Value = PersonType });
    /// command.Parameters.Add(new SqlParameter() { ParameterName = "@rowguid", SqlDbType = System.Data.SqlDbType.UniqueIdentifier, Value = rowguid });
    /// command.Parameters.Add(new SqlParameter() { ParameterName = "@Suffix", SqlDbType = System.Data.SqlDbType.NVarChar, Value = Suffix });
    /// command.Parameters.Add(new SqlParameter() { ParameterName = "@Title", SqlDbType = System.Data.SqlDbType.NVarChar, Value = Title });
    /// </code>
    /// </example>
    /// <param name="includeKeyIdentificationComment">An enum value indicating whether or not to include
    /// a comment identifying the column as a primary and/or foreign key (see example code).</param>
    /// <returns>A <see cref="System.Collections.Generic.List{T}">List&lt;String&gt;</see>.</returns>
    public List<String> GetTargetLanguageSqlParameterText(ColumnType columnType, IncludeKeyIdentificationComment includeKeyIdentificationComment = IncludeKeyIdentificationComment.Yes)
    {
      return
        this
        .Where(column => (column.ColumnType & columnType) > 0)
        .OrderByDescending(column => column.ColumnType.HasFlag(ColumnType.ID))
        .ThenByDescending(column => column.ColumnType.HasFlag(ColumnType.PrimaryKey))
        .ThenBy(column => column.PrimaryKeyOrdinal)
        .ThenByDescending(column => column.ColumnType.HasFlag(ColumnType.ForeignKey))
        .ThenBy(column => column.Name)
        .Select(column => column.GetTargetLanguageSqlParameterText(includeKeyIdentificationComment))
        .ToList();
    }

    /// <summary>
    /// Return a list of strings that contain the backing store declarations for all of the columns in a table.
    /// <para>Since auto-properties don't require a backing store, this method returns backing store declarations
    /// for only those non-auto properties that need them.</para>
    /// </summary>
    /// <example>
    /// For the Person.Person table in AdventureWorks2012, with the configuration's TargetLanguage set to CSharp and
    /// XmlSystem set to NonLinq_XmlDocument, this method will return a list with these strings:
    /// <code>
    /// private System.Xml.XmlDocument _additionalcontactinfo;
    /// private System.Xml.XmlDocument _demographics;
    /// </code>
    /// </example>
    /// <returns>A <see cref="System.Collections.Generic.List{T}">List&lt;String&gt;</see>.</returns>
    public List<String> GetNecessaryTargetLanguageBackingStoreDeclarations()
    {
      return
        this
       .Where(column => column.DoesTargetLanguagePropertyNeedBackingStore)
       .Select(column => column.TargetLanguageBackingStoreDeclaration)
       .ToList();
    }
  }
}
