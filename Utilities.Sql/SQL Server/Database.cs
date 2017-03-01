/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Utilities.Core;

namespace Utilities.Sql.SqlServer
{
  public class Database : BaseSqlServerObject
  {
    private readonly SqlConnection _connection;

    public Server Server { get; private set; }

    private List<Schema> _schemas = null;
    public List<Schema> Schemas
    {
      get
      {
        if (this._schemas == null)
        {
          this._connection.ExecuteUnderDatabaseInvariant(this.Name,
            () =>
            {
              this._schemas = new List<Schema>();
              var sql = @"
SELECT
    [SCHEMA_NAME] = S.[name],
    is_default_schema = CASE WHEN S.schema_id = SCHEMA_ID() THEN 'Y' ELSE 'N' END
  FROM
    sys.schemas AS S
    INNER JOIN sys.database_principals AS U ON U.principal_id = S.principal_id
  WHERE
    U.is_fixed_role = 0
    AND U.sid IS NOT NULL
    AND DATALENGTH(U.sid) > 0
    AND LOWER(S.[Name]) NOT IN ('sys', 'guest');";

              var schemas = this._connection.GetDataSet(sql).Tables[0];
              foreach (DataRow row in schemas.Rows)
                this._schemas.Add(new Schema(this, row["SCHEMA_NAME"].ToString(), row["is_default_schema"].ToString().AsBoolean()));
            });
        }

        return this._schemas;
      }
    }

    public Database(Server server, String name)
      : base()
    {
      this.Server = server;
      this.Name = IdentifierHelper.GetStrippedSqlIdentifier(name);

      this._connection = this.Server.Configuration.Connection;
    }

    public StoredProcedure AddStoredProcedure(String name, params SqlParameter[] sqlParameters)
    {
      return AddStoredProcedure(name, 1, sqlParameters);
    }

    /// <summary>
    /// Create and add a StoredProcedure instance to this Database.
    /// <para>The name parameter must be a valid one-part or two-part T-SQL identifier.  Square brackets around the name parts are optional.</para>
    /// <para>E.g. "my_stored_proc", "[my_stored_proc]", "my_schema.my_stored_proc", and "[my_schema].[my_stored_proc]" are all valid name identifiers.</para>
    /// <para>Names with more than two parts, or missing parts, are considered errors.</para>
    /// </summary>
    public StoredProcedure AddStoredProcedure(String name, Int32 versionNumber, params SqlParameter[] sqlParameters)
    {
      name.Name("name").NotNullEmptyOrOnlyWhitespace();
      versionNumber.Name("versionNumber").GreaterThan(0);

      name = IdentifierHelper.GetStrippedSqlIdentifier(name);
      var nameParts = name.Split(".".ToCharArray(), StringSplitOptions.None);

      /* It's an error if any of the name parts are empty,
         and this method only accepts one-part ([object name]) or
         two-part ([schema name].[object name]) T-SQL identifiers. */
      if (nameParts.Any(s => s.IsEmpty()) || (nameParts.Length > 2))
        throw new ArgumentExceptionFmt(Properties.Resources.InvalidStoredProcedureName, name);

      /* At this point, all of the name parts have been validated for correct form. */

      if (nameParts.Length == 1)
      {
        return this.Schemas.GetDefaultSchema().AddStoredProcedure(name, versionNumber, sqlParameters);
      }
      else
      {
        var schemaName = nameParts[0];
        var schema = this.Schemas.GetByName(schemaName);
        if (schema == null)
          throw new ExceptionFmt(Properties.Resources.SchemaNameNotFound, schemaName);

        var storedProcedureName = nameParts[1];

        return schema.AddStoredProcedure(storedProcedureName, versionNumber, sqlParameters);
      }
    }
  }

  public static class DatabaseExtensions
  {
    public static Database GetByName(this IEnumerable<Database> databases, String name)
    {
      return databases.Where(database => database.Name.EqualsCI(name)).FirstOrDefault();
    }
  }
}
