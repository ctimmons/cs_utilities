using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using Utilities.Core;

namespace Utilities.Sql.SqlServer
{
  /// <summary>
  /// Some of the methods in the classes in this namespace select a set of columns.
  /// This enumeration allows for selecting a subset of a table's columns
  /// based on the column's key type.
  /// <para>These values used to be separate properties in the Column object,
  /// but it turned out to be more flexible to package them all in an
  /// enumeration.</para>
  /// </summary>
  [Flags]
  public enum ColumnType
  {
    /// <summary>
    /// A column should never have this value.  However, this value is useful as
    /// a starting point when building a ColumnType for a particular column.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A column that is not a primary or a foreign key, nor an ID column.
    /// </summary>
    NonKeyAndNonID = 1,

    /// <summary>
    /// A primary key column.
    /// </summary>
    PrimaryKey = 2,

    /// <summary>
    /// A foreign key column.
    /// </summary>
    ForeignKey = 4,

    /// <summary>
    /// An identity column.
    /// </summary>
    ID = 8,

    /// <summary>
    /// Columns with the XML type cannot be used in a WHERE clause.
    /// </summary>
    CanAppearInSqlWhereClause = 16,

    /// <summary>
    /// Columns with the TIMESTAMP type and ID columns cannot appear in an UPDATE statement's SET clause.
    /// </summary>
    CanAppearInUpdateSetClause = 32,

    /// <summary>
    /// Columns with the TIMESTAMP type and ID columns cannot appear in an INSERT statement.
    /// </summary>
    CanAppearInInsertStatement = 64,

    /// <summary>
    /// Combination of CanAppearInInsertStatement and CanAppearInUpdateSetClause.
    /// </summary>
    CanAppearInMergeSelectList = CanAppearInUpdateSetClause | CanAppearInInsertStatement,

    /// <summary>
    /// All column types.
    /// </summary>
    All = NonKeyAndNonID | PrimaryKey | ForeignKey | ID | CanAppearInSqlWhereClause | CanAppearInUpdateSetClause | CanAppearInInsertStatement | CanAppearInMergeSelectList
  }

  /// <summary>
  /// The Column class contains the majority of primitive properties and methods needed to generate
  /// TSQL, C#, F# and VB target code.
  /// </summary>
  public class Column : BaseSqlServerObject
  {
    /* Only one of these will be the parent of this column. */
    public StoredProcedure StoredProcedure { get; private set; }
    public Table Table { get; private set; }

    /// <summary>
    /// The position of this column in the list of columns in the parent table or view.
    /// </summary>
    public Int32 Ordinal { get; set; }

    /// <summary>
    /// An enumeration indicating what kind of column this is.
    /// </summary>
    public ColumnType ColumnType { get; set; }

    /// <summary>
    /// The SQL Server native data type name (E.g. VARCHAR(50), INT, DATETIME, etc.), or aliased data type name (E.g. CustomerAddress, ZipCode, etc.).
    /// </summary>
    public String ServerDataTypeName { get; set; }

    /// <summary>
    /// The SQL Server native data type name.  If this column's <see cref="Utilities.Sql.Column.ServerDataTypeName">ServerDataTypeName</see> property
    /// is an aliased type name, this property will contain the underlying native data type name.  E.g. if this column has
    /// a ServerDataTypeName of "CustomerAddress", this property will contain the underlying native data type name of NVARCHAR(50).
    /// </summary>
    public String NativeServerDataTypeName { get; set; }

    /// <summary>
    /// The number of bytes the column occupies on the server.
    /// </summary>
    public Int32 PhysicalLength { get; set; }

    /// <summary>
    /// The same as <see cref="Utilities.Sql.Column.PhysicalLength">PhysicalLength</see>, unless the column's type
    /// is something like VARBINARY(MAX), N/VARCHAR(MAX) or XML.
    /// </summary>
    public Int32 LogicalLength { get; set; }

    public Int32 Precision { get; set; }
    public Int32 Scale { get; set; }
    public Boolean IsNullable { get; set; }
    public Boolean IsXmlDocument { get; set; }
    public String XmlCollectionName { get; set; }

    /// <summary>
    /// Some primary keys consist of multiple columns.  If <see cref="Utilities.Sql.Column.ColumnType">ColumnType</see> contains
    /// the PrimaryKey flag, this property
    /// indicates this column's position in the set of columns that make up the primary key.
    /// </summary>
    public Int32 PrimaryKeyOrdinal { get; set; }

    /// <summary>
    /// 'ASC' for an ascending primary key, 'DESC' for a descending primary key.
    /// An empty string if this column is not part of a primary key definition.
    /// </summary>
    public String PrimaryKeyDirection { get; set; }

    /// <summary>
    /// If <see cref="Utilities.Sql.Column.ColumnType">ColumnType</see> contains the ForeignKey flag, the name of the schema that owns the table this foreign key references.
    /// </summary>
    public String PrimaryKeySchema { get; set; }

    /// <summary>
    /// If <see cref="Utilities.Sql.Column.ColumnType">ColumnType</see> contains the ForeignKey flag, the name of the table this foreign key references.
    /// </summary>
    public String PrimaryKeyTable { get; set; }

    /// <summary>
    /// If <see cref="Utilities.Sql.Column.ColumnType">ColumnType</see> contains the ForeignKey flag, the name of the column this foreign key references.
    /// </summary>
    public String PrimaryKeyColumn { get; set; }

    private String _clrTypeName = null;
    /// <summary>
    /// The fully qualified CLR type name for this column's <see cref="Utilities.Sql.Column.NativeServerDataTypeName">NativeServerDataTypeName</see>.
    /// E.g. this property would contain System.String for a column type of NVARCHAR(50).
    /// </summary>
    public String ClrTypeName
    {
      get
      {
        if (this._clrTypeName == null)
          this._clrTypeName = this.GetClrTypeNameFromNativeSqlType();

        return this._clrTypeName;
      }
    }

    private String _sqlDbTypeEnumName = null;
    public String SqlDbTypeEnumName
    {
      get
      {
        if (this._sqlDbTypeEnumName == null)
          this._sqlDbTypeEnumName = this.GetSqlDbTypeFromNativeSqlType();

        return this._sqlDbTypeEnumName;
      }
    }

    private String _targetLanguageBackingStoreDeclaration = null;
    /// <summary>
    /// A simple target language backing store declaration.  For use when
    /// generating properties that require a backing store.  The scope of
    /// the declaration is always private (e.g. "private String _customername").
    /// </summary>
    public String TargetLanguageBackingStoreDeclaration
    {
      get
      {
        if (this._targetLanguageBackingStoreDeclaration == null)
          this._targetLanguageBackingStoreDeclaration = this.GetTargetLanguageBackingStoreDeclaration();

        return this._targetLanguageBackingStoreDeclaration;
      }
    }

    /// <summary>
    /// Sometimes the code for a particular target language's property definition returned by GetTargetLanguageProperty
    /// has a dependency on an associated backing store.
    /// This property can be used to determine whether or not TargetLanguageBackingStoreDeclaration
    /// needs to be used to place the backing store in any generated code.
    /// </summary>
    public Boolean DoesTargetLanguagePropertyNeedBackingStore
    {
      get
      {
        var isXmlRelatedProperty =
          !String.IsNullOrWhiteSpace(this.XmlCollectionName) &&
          this._configuration.XmlValidationLocation.HasFlag(XmlValidationLocation.PropertySetter);

        return !this._configuration.TargetLanguage.DoesSupportAutoProperties() || isXmlRelatedProperty;
      }
    }

    private String _sqlIdentifier = null;
    /// <summary>
    /// This column's name, converted to a valid identifier for use in TSQL.
    /// </summary>
    public String SqlIdentifier
    {
      get
      {
        if (this._sqlIdentifier == null)
          this._sqlIdentifier = "@" + this.Name.Replace(" ", "_");

        return this._sqlIdentifier;
      }
    }

    private String _sqlIdentifierTypeAndSize = null;
    /// <summary>
    /// This column's SQL Server data type name and size (if applicable).  E.g. VARCHAR(10), INT, DECIMAL(18, 5), etc.
    /// </summary>
    public String SqlIdentifierTypeAndSize
    {
      get
      {
        if (this._sqlIdentifierTypeAndSize == null)
          this._sqlIdentifierTypeAndSize = this.GetSqlParameterTypeAndSize();

        return this._sqlIdentifierTypeAndSize;
      }
    }

    private Nullable<Boolean> _isTrimmable = null;
    /// <summary>
    /// Can this column be trimmed (i.e. is this column's underlying type a string)?
    /// </summary>
    public Boolean IsTrimmable
    {
      get
      {
        if (!this._isTrimmable.HasValue)
          this._isTrimmable = this.IsDataTypeTrimmable();

        return this._isTrimmable.Value;
      }
    }

    private String _sqlExpressionToConvertToString = null;
    /// <summary>
    /// The TSQL expression to convert this column to an NVARCHAR(MAX).
    /// </summary>
    public String SqlExpressionToConvertToString
    {
      get
      {
        if (this._sqlExpressionToConvertToString == null)
          this._sqlExpressionToConvertToString = this.GetSqlExpressionToConvertToString();

        return this._sqlExpressionToConvertToString;
      }
    }

    private String _targetLanguageSqlParameterValue = null;
    /// <summary>
    /// Target language expression to assign this column to the Value property of an <see cref="System.Data.SqlClient.SqlParameter">SqlParameter</see> instance.
    /// </summary>
    public String TargetLanguageSqlParameterValue
    {
      get
      {
        if (this._targetLanguageSqlParameterValue == null)
          this._targetLanguageSqlParameterValue = this.GetTargetLanguageSqlParameterValue();

        return this._targetLanguageSqlParameterValue;
      }
    }

    public XmlNodeType XmlNodeType
    {
      get
      {
        return (this.IsXmlDocument ? XmlNodeType.Document : XmlNodeType.DocumentFragment);
      }
    }

    private String _keyIdentificationComment = null;
    /// <summary>
    /// It can be helpful to annotate the generated code of primary and foreign key columns with a comment, simply because of their importance.
    /// <para>This property contains such a comment for this column.  For primary keys, the comment will contain the text "primary key"
    /// followed by a number indicating the column's position within a multipart key.  For foreign keys, the comment simply contains
    /// the text "foreign key".  For columns that are both a primary and foreign key, the comment will contain both kinds of text.</para>
    /// </summary>
    public String KeyIdentificationComment
    {
      get
      {
        if (this._keyIdentificationComment == null)
          this._keyIdentificationComment = this.GetKeyIdentificationComment();

        return this._keyIdentificationComment;
      }
    }

    private Configuration _configuration;

    private Column()
      : base()
    {
    }

    public Column(Table table, String name)
      : this()
    {
      this.Table = table;
      this.Name = SqlServerUtilities.GetStrippedSqlIdentifier(name);
      this._configuration = table.Schema.Database.Server.Configuration;
    }

    public Column(StoredProcedure storedProcedure, String name)
      : this()
    {
      this.StoredProcedure = storedProcedure;
      this.Name = SqlServerUtilities.GetStrippedSqlIdentifier(name);
      this._configuration = storedProcedure.Schema.Database.Server.Configuration;
    }

    /// <summary>
    /// An expression that compares this column's Name property with its SqlIdentifier property.
    /// <para>This expression is valid TSQL and can be used in a WHERE clause.</para>
    /// </summary>
    /// <param name="tableAlias">An optional string representing a table alias that will be prepended to the column name.</param>
    /// <returns>A String</returns>
    public String GetSqlWhereClause(String tableAlias = "")
    {
      var format = "";

      switch (this.NativeServerDataTypeName)
      {
        case "GEOGRAPHY":
        case "GEOMETRY":
          format = "({0}{1}.STEquals({2}) = 1)";
          break;
        case "HIERARCHYID":
        case "IMAGE":
          format = "CAST({0}{1} AS VARBINARY(MAX)) = CAST({2} AS VARBINARY(MAX))";
          break;
        case "NTEXT":
        case "TEXT":
          format = "CAST({0}{1} AS NVARCHAR(MAX)) = CAST({2} AS NVARCHAR(MAX))";
          break;
        default:
          format = "{0}{1} = {2}";
          break;
      }

      return String.Format(format, (String.IsNullOrWhiteSpace(tableAlias) ? "" : tableAlias + "."), this.BracketedName, this.SqlIdentifier);
    }

    private String GetClrTypeNameFromNativeSqlType()
    {
      Func<String, String> getAppropriateClrType =
        clrType =>
        {
          if (this._configuration.TargetLanguage.IsCSharp() || this._configuration.TargetLanguage.IsFSharp())
            return this.IsNullable ? "System.Nullable<System." + clrType + ">" : "System." + clrType;
          else if (this._configuration.TargetLanguage.IsVisualBasic())
            return this.IsNullable ? "System.Nullable(Of System." + clrType + ")" : "System." + clrType;
          else
            throw new NotImplementedException(String.Format(Properties.Resources.UnknownTargetLanguageValue, this._configuration.TargetLanguage));
        };

      switch (this.NativeServerDataTypeName)
      {
        case "BIGINT":
          return getAppropriateClrType("Int64");
        case "BINARY":
        case "FILESTREAM":
        case "IMAGE":
        case "ROWVERSION":
        case "TIMESTAMP":
        case "VARBINARY":
          if (this._configuration.TargetLanguage.IsCSharp() || this._configuration.TargetLanguage.IsFSharp())
            return "System.Byte[]";
          else if (this._configuration.TargetLanguage.IsVisualBasic())
            return "System.Byte()";
          else
            throw new NotImplementedException(String.Format(Properties.Resources.UnknownTargetLanguageValue, this._configuration.TargetLanguage));
        case "BIT":
          return getAppropriateClrType("Boolean");
        case "CURSOR":
          return "";
        case "DATE":
        case "DATETIME":
        case "DATETIME2":
        case "SMALLDATETIME":
          return getAppropriateClrType("DateTime");
        case "DATETIMEOFFSET":
          return getAppropriateClrType("DateTimeOffset");
        case "DECIMAL":
        case "MONEY":
        case "NUMERIC":
        case "SMALLMONEY":
          return getAppropriateClrType("Decimal");
        case "FLOAT":
          return getAppropriateClrType("Double");
        case "GEOGRAPHY":
          return "Microsoft.SqlServer.Types.SqlGeography";
        case "GEOMETRY":
          return "Microsoft.SqlServer.Types.SqlGeometry";
        case "HIERARCHYID":
          return "Microsoft.SqlServer.Types.SqlHierarchyId";
        case "INT":
          return getAppropriateClrType("Int32");
        case "CHAR":
        case "NCHAR":
        case "NTEXT":
        case "NVARCHAR":
        case "TEXT":
        case "VARCHAR":
          return "System.String";
        case "XML":
          switch (this._configuration.XmlSystem)
          {
            case XmlSystem.AsString:
              return "System.String";
            case XmlSystem.Linq_XDocument:
              if (this.IsXmlDocument)
                return "System.Xml.Linq.XDocument";
              else
                return "System.Xml.Linq.XElement";
            case XmlSystem.NonLinq_XmlDocument:
              return "System.Xml.XmlDocument";
            default:
              return String.Format(Properties.Resources.UnknownXmlSystemValue, this._configuration.XmlSystem);
          }
        case "REAL":
          return getAppropriateClrType("Single");
        case "SMALLINT":
          return getAppropriateClrType("Int16");
        case "SQL_VARIANT":
          return "System.Object";
        case "TIME":
          return getAppropriateClrType("TimeSpan");
        case "TINYINT":
          return getAppropriateClrType("Byte");
        case "UNIQUEIDENTIFIER":
          return getAppropriateClrType("Guid");
        default:
          return String.Format("ERROR - Can't find CLR type that corresponds to SQL Server type {0}.", this.NativeServerDataTypeName);
      }
    }

    /// <summary>
    /// Given the name of an SqlDataReader instance, return an expression that can be used
    /// to safely get the column's value out of the reader.
    /// </summary>
    /// <param name="readerName">The name of an SqlDataReader instance.</param>
    /// <returns>A String.</returns>
    public String GetTargetLanguageDataReaderExpression(String readerName)
    {
      if (this._configuration.TargetLanguage.IsCSharp() || this._configuration.TargetLanguage.IsFSharp())
        return String.Format("{0}.GetValueOrDefault<{1}>(\"{2}\")", readerName, this.ClrTypeName, this.Name);
      else if (this._configuration.TargetLanguage.IsVisualBasic())
        return String.Format("{0}.GetValueOrDefault(Of {1})(\"{2}\")", readerName, this.ClrTypeName, this.Name);
      else
        throw new NotImplementedException(String.Format(Properties.Resources.UnknownTargetLanguageValue, this._configuration.TargetLanguage));
    }

    private String GetSqlDbTypeFromNativeSqlType()
    {
      var result = "";

      /* Some of this code may look redundant, but the case of the returned
         string is important. This is because the text returned by this function
         will be used to generate source code, probably for a case-sensitive language
         like C#. */

      switch (this.NativeServerDataTypeName.ToUpper())
      {
        case "BIGINT":
          result = "BigInt";
          break;
        case "BINARY":
        case "FILESTREAM":
        case "VARBINARY":
          result = "VarBinary";
          break;
        case "ROWVERSION":
        case "TIMESTAMP":
          result = "Timestamp";
          break;
        case "BIT":
          result = "Bit";
          break;
        case "GEOGRAPHY":
        case "GEOMETRY":
        case "HIERARCHYID":
          result = "Udt";
          break;
        case "DATE":
          result = "Date";
          break;
        case "DATETIME":
        case "SMALLDATETIME":
          result = "DateTime";
          break;
        case "DATETIME2":
          result = "DateTime2";
          break;
        case "DATETIMEOFFSET":
          result = "DateTimeOffset";
          break;
        case "DECIMAL":
        case "NUMERIC":
          result = "Decimal";
          break;
        case "MONEY":
          result = "Money";
          break;
        case "SMALLMONEY":
          result = "SmallMoney";
          break;
        case "FLOAT":
          result = "Float";
          break;
        case "IMAGE":
          result = "Binary";
          break;
        case "INT":
          result = "Int";
          break;
        case "CHAR":
          result = "Char";
          break;
        case "NCHAR":
          result = "NChar";
          break;
        case "NTEXT":
          result = "NText";
          break;
        case "NVARCHAR":
          result = "NVarChar";
          break;
        case "TEXT":
          result = "Text";
          break;
        case "VARCHAR":
          result = "VarChar";
          break;
        case "XML":
          result = "Xml";
          break;
        case "REAL":
          result = "Real";
          break;
        case "SMALLINT":
          result = "SmallInt";
          break;
        case "SQL_VARIANT":
          result = "Variant";
          break;
        case "TIME":
          result = "Time";
          break;
        case "TINYINT":
          result = "TinyInt";
          break;
        case "UNIQUEIDENTIFIER":
          result = "UniqueIdentifier";
          break;
        default:
          result = "";
          break;
      }

      if (!String.IsNullOrWhiteSpace(result))
        return "System.Data.SqlDbType." + result;
      else
        return "";
    }

    private String GetSqlParameterTypeAndSize()
    {
      switch (this.ServerDataTypeName)
      {
        case "DATETIME2":
        case "DATETIMEOFFSET":
        case "TIME":
          return String.Format("{0}({1})", this.ServerDataTypeName, this.Scale);

        case "DECIMAL":
        case "NUMERIC":
          return String.Format("{0}({1}, {2})", this.ServerDataTypeName, this.Precision, this.Scale);

        case "VARBINARY":
        case "VARCHAR":
        case "NVARCHAR":
          return String.Format("{0}({1})", this.ServerDataTypeName, (this.LogicalLength == -1 ? "MAX" : this.LogicalLength.ToString()));

        case "BINARY":
        case "CHAR":
        case "NCHAR":
          return String.Format("{0}({1})", this.ServerDataTypeName, this.LogicalLength);

        case "XML":
          if (String.IsNullOrWhiteSpace(this.XmlCollectionName))
            return this.ServerDataTypeName;
          else
            return String.Format("{0}({1}, {2})", this.ServerDataTypeName, this.IsXmlDocument ? "DOCUMENT" : "CONTENT", this.XmlCollectionName);

        default:
          return this.ServerDataTypeName;
      }
    }

    private Boolean IsDataTypeTrimmable()
    {
      return "CHAR/VARCHAR/NCHAR/NVARCHAR/TEXT/NTEXT".Contains(this.NativeServerDataTypeName);
    }

    private String GetSqlExpressionToConvertToString()
    {
      switch (this.NativeServerDataTypeName)
      {
        case "CHAR":
        case "NCHAR":
        case "NVARCHAR":
        case "SYSNAME":
        case "TIMESTAMP":
        case "VARCHAR":
          return this.SqlIdentifier;

        case "BIGINT":
        case "BIT":
        case "DATE":
        case "DATETIME2":
        case "DATETIMEOFFSET":
        case "DECIMAL":
        case "FLOAT":
        case "INT":
        case "MONEY":
        case "NTEXT":
        case "NUMERIC":
        case "REAL":
        case "SMALLINT":
        case "SMALLMONEY":
        case "TEXT":
        case "TINYINT":
        case "UNIQUEIDENTIFIER":
        case "XML":
          return String.Format("CONVERT(NVARCHAR(MAX), {0})", this.SqlIdentifier);

        case "DATETIME":
        case "SMALLDATETIME":
          return String.Format("CONVERT(NVARCHAR(MAX), {0}, 121)", this.SqlIdentifier);

        case "TIME":
          return String.Format("CONVERT(NVARCHAR(MAX), {0}, 14)", this.SqlIdentifier);

        case "GEOGRAPHY":
        case "GEOMETRY":
          return String.Format("{0}.STAsText()", this.SqlIdentifier);

        case "HIERARCHYID":
          return String.Format("{0}.ToString()", this.SqlIdentifier);

        case "BINARY":
        case "IMAGE":
        case "VARBINARY":
          return String.Format("'{0} with length ' + CONVERT(NVARCHAR(MAX), DATALENGTH({1})) + '.'", this.NativeServerDataTypeName, this.SqlIdentifier);

        case "SQL_VARIANT":
          return String.Format("dbo.util_Get_SqlVariant_As_NVarCharMax({0})", this.SqlIdentifier);

        default:
          return String.Format("Don't know how to convert type {0} to a string.", this.NativeServerDataTypeName);
      }
    }

    /// <summary>
    /// Returns a string that can be used in generated code to create a new SqlParameter instance for this column.
    /// </summary>
    /// <param name="includeKeyIdentificationComment">An enum value indicating whether or not to include
    /// a comment identifying the column as a primary and/or foreign key.</param>
    /// <returns>A String.</returns>
    public String GetTargetLanguageSqlParameterText(IncludeKeyIdentificationComment includeKeyIdentificationComment = IncludeKeyIdentificationComment.Yes)
    {
      var comment =
        ((includeKeyIdentificationComment == IncludeKeyIdentificationComment.Yes) && !String.IsNullOrWhiteSpace(this.KeyIdentificationComment))
        ? " " + this.KeyIdentificationComment
        : "";

      if (this._configuration.TargetLanguage.IsCSharp())
      {
        if (this.SqlDbTypeEnumName == "System.Data.SqlDbType.Udt")
          return String.Format("new SqlParameter() {{ ParameterName = \"{0}\", SqlDbType = {1}, UdtTypeName = \"{2}\", Value = {3} }}{4}",
            this.SqlIdentifier, this.SqlDbTypeEnumName, this.NativeServerDataTypeName, this.TargetLanguageSqlParameterValue, comment).Trim();
        else
          return String.Format("new SqlParameter() {{ ParameterName = \"{0}\", SqlDbType = {1}, Value = {2} }}{3}",
            this.SqlIdentifier, this.SqlDbTypeEnumName, this.TargetLanguageSqlParameterValue, comment).Trim();
      }
      else if (this._configuration.TargetLanguage.IsFSharp())
      {
        if (this.SqlDbTypeEnumName == "System.Data.SqlDbType.Udt")
          return String.Format("new SqlParameter(ParameterName = \"{0}\", SqlDbType = {1}, UdtTypeName = \"{2}\", Value = {3}){4}",
            this.SqlIdentifier, this.SqlDbTypeEnumName, this.NativeServerDataTypeName, this.TargetLanguageSqlParameterValue, comment).Trim();
        else
          return String.Format("new SqlParameter(ParameterName = \"{0}\", SqlDbType = {1}, Value = {2}){3}",
            this.SqlIdentifier, this.SqlDbTypeEnumName, this.TargetLanguageSqlParameterValue, comment).Trim();
      }
      else if (this._configuration.TargetLanguage.IsVisualBasic())
      {
        if (this.SqlDbTypeEnumName == "System.Data.SqlDbType.Udt")
          return String.Format("new SqlParameter() With {{ .ParameterName = \"{0}\", .SqlDbType = {1}, .UdtTypeName = \"{2}\", .Value = {3} }}{4}",
            this.SqlIdentifier, this.SqlDbTypeEnumName, this.NativeServerDataTypeName, this.TargetLanguageSqlParameterValue, comment).Trim();
        else
          return String.Format("new SqlParameter() With {{ .ParameterName = \"{0}\", .SqlDbType = {1}, .Value = {2} }}{3}",
            this.SqlIdentifier, this.SqlDbTypeEnumName, this.TargetLanguageSqlParameterValue, comment).Trim();
      }
      else
        throw new NotImplementedException(String.Format(Properties.Resources.UnknownTargetLanguageValue, this._configuration.TargetLanguage));
    }

    private String GetTargetLanguageSqlParameterValue()
    {
      if (this.ServerDataTypeName == "XML")
        switch (this._configuration.XmlSystem)
        {
          case XmlSystem.AsString:
          case XmlSystem.Linq_XDocument:
            return String.Format("{0}.GetSqlXml()", this.TargetLanguageIdentifier);
          case XmlSystem.NonLinq_XmlDocument:
            return String.Format("{0}.GetSqlXml(XmlNodeType.{1})", this.TargetLanguageIdentifier, this.XmlNodeType);
          default:
            return String.Format(Properties.Resources.UnknownXmlSystemValue, this._configuration.XmlSystem);
        }
      else
        return this.TargetLanguageIdentifier;
    }

    private String GetKeyIdentificationComment()
    {
      var comments = new List<String>();

      if (this.ColumnType.HasFlag(ColumnType.ID))
        comments.Add("identity");

      if (this.ColumnType.HasFlag(ColumnType.PrimaryKey))
        comments.Add(String.Format("primary key {0}", this.PrimaryKeyOrdinal));

      if (this.ColumnType.HasFlag(ColumnType.ForeignKey))
        comments.Add(String.Format("foreign key ({0}.{1}({2}))", this.PrimaryKeySchema, this.PrimaryKeyTable, this.PrimaryKeyColumn));

      var format = "";

      if (this._configuration.TargetLanguage.IsCSharp())
        format = "/* {0} */";
      else if (this._configuration.TargetLanguage.IsFSharp())
        format = "(* {0} *)";
      else if (this._configuration.TargetLanguage.IsVisualBasic())
        format = "' {0}";
      else
        throw new NotImplementedException(String.Format(Properties.Resources.UnknownTargetLanguageValue, this._configuration.TargetLanguage));

      return (comments.Count == 0) ? "" : String.Format(format, String.Join(", ", comments));
    }

    /// <summary>
    /// Return a string that can be used in generated code to represent this column as a target language method parameter.
    /// </summary>
    /// <param name="includeKeyIdentificationComment">An enum value indicating whether or not to include
    /// a comment identifying the column as a primary and/or foreign key.</param>
    /// <returns>A String.</returns>
    public String GetTargetLanguageMethodParameterNameAndType(IncludeKeyIdentificationComment includeKeyIdentificationComment = IncludeKeyIdentificationComment.Yes)
    {
      var format = "";

      if (this._configuration.TargetLanguage.IsCSharp())
        format = "{0} {1}{2}";
      else if (this._configuration.TargetLanguage.IsFSharp())
        format = "({1} : {0}{2})";
      else if (this._configuration.TargetLanguage.IsVisualBasic())
        format = "{1} As {0}{2}";
      else
        throw new NotImplementedException(String.Format(Properties.Resources.UnknownTargetLanguageValue, this._configuration.TargetLanguage));

      return String.Format(format, this.ClrTypeName, this.TargetLanguageIdentifier,
        (((includeKeyIdentificationComment == IncludeKeyIdentificationComment.Yes) && !String.IsNullOrWhiteSpace(this.KeyIdentificationComment))
          ? " " + this.KeyIdentificationComment
          : "")).Trim();
    }

    private String GetTargetLanguageBackingStoreDeclaration()
    {
      if (this._configuration.TargetLanguage.IsCSharp())
        return String.Format("private {0} {1};", this.ClrTypeName, this.TargetLanguageBackingStoreIdentifier);
      else if (this._configuration.TargetLanguage.IsFSharp())
        return String.Format("let mutable {1} = Unchecked.defaultof<{0}>", this.ClrTypeName, this.TargetLanguageBackingStoreIdentifier);
      else if (this._configuration.TargetLanguage.IsVisualBasic())
        return String.Format("Private Dim {1} As {0}", this.ClrTypeName, this.TargetLanguageBackingStoreIdentifier);
      else
        throw new NotImplementedException(String.Format(Properties.Resources.UnknownTargetLanguageValue, this._configuration.TargetLanguage));
    }

    /// <summary>
    /// Return a string that can be used in generated code to represent this column as a target language class property declaration.
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="includeKeyIdentificationComment">An enum value indicating whether or not to include
    /// a comment identifying the column as a primary and/or foreign key.</param>
    /// <returns>A String.</returns>
    public String GetTargetLanguageProperty(String scope, IncludeKeyIdentificationComment includeKeyIdentificationComment = IncludeKeyIdentificationComment.Yes)
    {
      var keyIdentificationComment =
        ((includeKeyIdentificationComment == IncludeKeyIdentificationComment.Yes) && !String.IsNullOrWhiteSpace(this.KeyIdentificationComment))
        ? this.KeyIdentificationComment
        : "";
      var targetLanguage = this._configuration.TargetLanguage;

      if (String.IsNullOrWhiteSpace(this.XmlCollectionName) || !this._configuration.XmlValidationLocation.HasFlag(XmlValidationLocation.PropertySetter))
      {
        var format = "";

        if (targetLanguage.IsCSharp())
        {
          if (targetLanguage.DoesSupportAutoProperties())
            format = "{0} {1} {2} {{ get; set; }} {3}";
          else
            format = @"
{3}
{0} {1} {2}
{{
  get {{ return this.{4}; }}
  set {{ this.{4} = value; }}
}}
";
        }
        else if (targetLanguage.IsFSharp())
        {
          if (targetLanguage.DoesSupportAutoProperties())
            format = "member val {0} {2} = Unchecked.defaultof<{1}> with get, set {3}";
          else
            format = @"
{3}
member {0} this.{2} with get() = {4}
member {0} this.{2} with set value = {4} <- value
";
        }
        else if (targetLanguage.IsVisualBasic())
        {
          if (targetLanguage.DoesSupportAutoProperties())
            format = "{0} Property {2}() As {1} {3}";
          else
            format = @"
{3}
{0} Property {2}() As {1}
  Get
    Return {4}
  End Get
  Set (Value As {1})
    {4} = value
  End Set
End Property
";
        }
        else
          throw new NotImplementedException(String.Format(Properties.Resources.UnknownTargetLanguageValue, this._configuration.TargetLanguage));

        return String.Format(format, scope, this.ClrTypeName, this.TargetLanguageIdentifier, keyIdentificationComment, this.TargetLanguageBackingStoreIdentifier);
      }
      else
      {
        if (this._configuration.TargetLanguage.IsCSharp())
          return this.GetXmlValidatedPropertyForCSharp(scope, keyIdentificationComment);
        else if (this._configuration.TargetLanguage.IsFSharp())
          return this.GetXmlValidatedPropertyForFSharp(scope, keyIdentificationComment);
        else if (this._configuration.TargetLanguage.IsVisualBasic())
          return this.GetXmlValidatedPropertyForVisualBasic(scope, keyIdentificationComment);
        else
          throw new NotImplementedException(String.Format(Properties.Resources.UnknownTargetLanguageValue, this._configuration.TargetLanguage));
      }
    }

    private String GetXmlValidatedPropertyForCSharp(String scope, String keyIdentificationComment)
    {
      var propertyTemplate = @"
{3}
{0} {1} {2}
{{
  get
  {{
    return this.{4};
  }}
  set
  {{
    var xsd = SqlXmlSchemas.Instance.GetXmlSchemaSet(""{5}"", ""{6}"", ""{7}"");
    {8}
    this.{4} = value;
  }}
}}
";
      String xmlValidationCode = null;

      switch (this._configuration.XmlSystem)
      {
        case XmlSystem.AsString:
          xmlValidationCode = @"
if (xsd != null)
{
  var xDocument = XDocument.Parse(value);
  xDocument.Validate(xsd, null);
}
";
          break;

        case XmlSystem.Linq_XDocument:
          xmlValidationCode = @"
if (xsd != null)
{
  value.Validate(xsd, null);
}
";
          break;

        case XmlSystem.NonLinq_XmlDocument:
          xmlValidationCode = @"
if (xsd != null)
{
  value.Schemas = xsd;
  value.Validate(null);
}
";
          break;

        default:
          throw new NotImplementedException(String.Format(Properties.Resources.UnknownXmlSystemValue, this._configuration.XmlSystem));
      }

      var schema = this.Table.Schema;

      return String.Format(propertyTemplate, scope, this.GetClrTypeNameFromNativeSqlType(), this.TargetLanguageIdentifier, keyIdentificationComment,
        this.TargetLanguageBackingStoreIdentifier, schema.Database.Name, schema.Name, this.XmlCollectionName, xmlValidationCode.Indent(4));
    }

    private String GetXmlValidatedPropertyForFSharp(String scope, String keyIdentificationComment)
    {
      var propertyTemplate = @"
{0}
member {1} this.{2} with get() = {3}
member {1} this.{2}
  with set value =
    let xsd = SqlXmlSchemas.Instance.GetXmlSchemaSet(""{4}"", ""{5}"", ""{6}"")
    {7}
    {3} <- value
";
      String xmlValidationCode = null;

      switch (this._configuration.XmlSystem)
      {
        case XmlSystem.AsString:
          xmlValidationCode = @"
if xsd <> null
  let xDocument = XDocument.Parse(value)
  xDocument.Validate(xsd, null)
";
          break;

        case XmlSystem.Linq_XDocument:
          xmlValidationCode = @"
if xsd <> null
  value.Validate(xsd, null)
";
          break;

        case XmlSystem.NonLinq_XmlDocument:
          xmlValidationCode = @"
if xsd <> null
  value.Schemas <- xsd
  value.Validate(null)
";
          break;

        default:
          throw new NotImplementedException(String.Format(Properties.Resources.UnknownXmlSystemValue, this._configuration.XmlSystem));
      }

      var schema = this.Table.Schema;

      return String.Format(propertyTemplate, keyIdentificationComment, scope, this.TargetLanguageIdentifier, this.TargetLanguageBackingStoreIdentifier,
        schema.Database.Name, schema.Name, this.XmlCollectionName, xmlValidationCode.Indent(4));
    }

    private String GetXmlValidatedPropertyForVisualBasic(String scope, String keyIdentificationComment)
    {
      var propertyTemplate = @"
{0}
{1} Property {2}() As {3}
  Get
    Return {4}
  End Get
  Set (Value As {3})
    Dim xsd As XmlSchemaSet = SqlXmlSchemas.Instance.GetXmlSchemaSet(""{5}"", ""{6}"", ""{7}"")
    {8}
    {4} = value
  End Set
End Property
";
      String xmlValidationCode = null;

      /* The awkward "If Not xsd Is Nothing Then" syntax is used instead of
         of "If xsd IsNot Nothing Then" because this code should be able
         to be run w/ VB 7.0 and 7.1, neither of which has the IsNot operator. */
      switch (this._configuration.XmlSystem)
      {
        case XmlSystem.AsString:
          xmlValidationCode = @"
If Not xsd Is Nothing Then
  Dim xDocument As XDocument = XDocument.Parse(value)
  xDocument.Validate(xsd, Null)
End If
";
          break;

        case XmlSystem.Linq_XDocument:
          xmlValidationCode = @"
If Not xsd Is Nothing Then
  value.Validate(xsd, Null)
End If
";
          break;

        case XmlSystem.NonLinq_XmlDocument:
          xmlValidationCode = @"
If Not xsd Is Nothing Then
  value.Schemas = xsd
  value.Validate(Null)
End If
";
          break;

        default:
          throw new NotImplementedException(String.Format(Properties.Resources.UnknownXmlSystemValue, this._configuration.XmlSystem));
      }

      var schema = this.Table.Schema;

      return String.Format(propertyTemplate, keyIdentificationComment, scope, this.TargetLanguageIdentifier, this.GetClrTypeNameFromNativeSqlType(),
        this.TargetLanguageBackingStoreIdentifier, schema.Database.Name, schema.Name, this.XmlCollectionName, xmlValidationCode.Indent(4));
    }

    /// <summary>
    /// Return a string that can be used in generated code to represent this column as a TSQL stored procedure parameter.
    /// </summary>
    /// <param name="includeKeyIdentificationComment">An enum value indicating whether or not to include
    /// a comment identifying the column as a primary and/or foreign key.</param>
    /// <returns>A String.</returns>
    public String GetStoredProcedureParameterDeclaration(IncludeKeyIdentificationComment includeKeyIdentificationComment = IncludeKeyIdentificationComment.Yes)
    {
      return String.Format("{0} {1}{2}", this.SqlIdentifier, this.SqlIdentifierTypeAndSize,
        ((includeKeyIdentificationComment == IncludeKeyIdentificationComment.Yes) && this.KeyIdentificationComment.Trim().Any()) ? " " + this.KeyIdentificationComment : "");
    }

    public String GetCreateTableColumnDeclaration()
    {
      return String.Format("{0} {1}{2}", this.BracketedName, this.SqlIdentifierTypeAndSize, this.IsNullable ? " NULL" : "");
    }
  }

  public static class ColumnExtensions
  {
    public static Column GetByName(this IEnumerable<Column> columns, String name)
    {
      return columns.Where(column => column.Name.EqualsCI(name)).FirstOrDefault();
    }
  }
}
