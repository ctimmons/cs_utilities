/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Utilities.Core;

namespace Utilities.Sql.SqlServer
{
  public class Schema : BaseSqlServerObject
  {
    public Database Database { get; private set; }

    public Boolean IsDefaultSchema { get; private set; }
    public List<StoredProcedure> StoredProcedures { get; private set; }

    private List<Table> _tables = null;
    public List<Table> Tables
    {
      get
      {
        if (this._tables == null)
        {
          this.Database.Server.Configuration.Connection.ExecuteUnderDatabaseInvariant(this.Database.Name,
            () =>
            {
              /* SQL Server's SCHEMA_ID() function does not accept schema names that are
                 surrounded by square brackets (e.g. '[Person]'), even if the string contains
                 a valid schema identifier (which *can* be surrounded by square brackets). */
              var normalizedSchemaName = this.Name.Trim("[]".ToCharArray());

              /* The table_type column values map directly to the TableType enum values,
                 to make the C# code a little cleaner by using Enum.Parse().

                 However, this is generally considered a bad programming practice,
                 as it introduces tight coupling between the SQL code and C# code.

                 But a little won't hurt, right? ...Right? */

              var sql =
$@"SELECT
    table_name = [name],
    table_schema = schema_name([schema_id]),
    table_type =
      CASE
        WHEN [temporal_type] = 0 THEN 'Table'
        WHEN [temporal_type] = 1 THEN 'HistoryTable'
        WHEN [temporal_type] = 2 THEN 'SystemVersionedTemporalTable'
      END
  FROM
    sys.tables
  WHERE
    [schema_id] = SCHEMA_ID('{normalizedSchemaName}')

UNION

SELECT
    [name],
    schema_name([schema_id]),
    'View'
  FROM
    sys.views
  WHERE
    [schema_id] = SCHEMA_ID('{normalizedSchemaName}');";

              this._tables = new List<Table>();
              var table = this.Database.Server.Configuration.Connection.GetDataSet(sql).Tables[0];
              foreach (DataRow row in table.Rows)
              {
                var tableType = (TableType) Enum.Parse(typeof(TableType), row["table_type"].ToString());
                this._tables.Add(new Table(this, row["table_name"].ToString(), tableType));
              }
            });
        }

        return this._tables;
      }
    }

    private List<UserDefinedTableType> _userDefinedTableTypes = null;
    public List<UserDefinedTableType> UserDefinedTableTypes
    {
      get
      {
        if (this._userDefinedTableTypes == null)
        {
          this.Database.Server.Configuration.Connection.ExecuteUnderDatabaseInvariant(this.Database.Name,
            () =>
            {
              var sql = 
$@"
SELECT
    T.name
  FROM
    sys.types AS T
    INNER JOIN sys.schemas AS S ON T.schema_id = S.schema_id
  WHERE
    T.is_table_type = 1
    AND S.name = '{this.Name}';";

              var table = this.Database.Server.Configuration.Connection.GetDataSet(sql).Tables[0];
              this._userDefinedTableTypes =
                table
                .Rows
                .Cast<DataRow>()
                .Select(row => new UserDefinedTableType(row["name"].ToString(), this))
                .ToList();
            });
        }

        return this._userDefinedTableTypes;
      }
    }

    public Schema(Database database, String name, Boolean isDefaultSchema)
      : base()
    {
      this.Database = database;
      this.Name = IdentifierHelper.GetStrippedSqlIdentifier(name);
      this.IsDefaultSchema = isDefaultSchema;
      this.StoredProcedures = new List<StoredProcedure>();
    }

    public StoredProcedure AddStoredProcedure(String name, params SqlParameter[] sqlParameters)
    {
      return this.AddStoredProcedure(name, 1, sqlParameters);
    }

    public StoredProcedure AddStoredProcedure(String name, Int32 versionNumber, params SqlParameter[] sqlParameters)
    {
      name.Name("name").NotNullEmptyOrOnlyWhitespace();
      versionNumber.Name("versionNumber").GreaterThan(0);

      name = IdentifierHelper.GetBracketedSqlIdentifier(name);

      if (name.Contains("."))
        throw new ArgumentExceptionFmt(Properties.Resources.InvalidStoredProcedureNameForSchema, name);

      if (this.StoredProcedures.GetByName(name, versionNumber) == null)
      {
        var sp = new StoredProcedure(this, name, versionNumber, sqlParameters);
        this.StoredProcedures.Add(sp);
        return sp;
      }
      else
      {
        throw new ExceptionFmt(Properties.Resources.StoredProcedureAlreadyExists, name, versionNumber);
      }
    }
  }

  public static class SchemaExtensions
  {
    public static Schema GetByName(this IEnumerable<Schema> schemas, String name)
    {
      return schemas.Where(schema => schema.Name.EqualsCI(name)).FirstOrDefault();
    }

    public static Schema GetDefaultSchema(this IEnumerable<Schema> schemas)
    {
      /* There's always at least one schema in a database, and one of those schemas is a default schema, so this should always work. */
      return schemas.Where(schema => schema.IsDefaultSchema).First();
    }
  }
}
