using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Utilities.Core;

namespace Utilities.Sql.SqlServer
{
  public class Schema
  {
    public Database Database { get; private set; }

    /// <summary>
    /// The schema name as it appears on the database server.
    /// </summary>
    public String Name { get; private set; }

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
              this._tables = new List<Table>();
              var table = this.Database.Server.Configuration.Connection.GetSchema("Tables");
              foreach (DataRow row in table.Rows)
              {
                if (this.Name.Trim("[]".ToCharArray()).EqualsCI(row["table_schema"].ToString()))
                {
                  this._tables.Add(new Table(this, row["table_name"].ToString(), row["table_type"].ToString().EqualsCI("VIEW")));
                }
              }
            });
        }

        return this._tables;
      }
    }

    public Schema(Database database, String name, Boolean isDefaultSchema)
      : base()
    {
      this.Database = database;
      this.Name = name;
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

      name = SqlServerUtilities.GetNormalizedSqlIdentifier(name);

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
      return schemas.Where(s => s.Name.EqualsCI(name)).FirstOrDefault();
    }

    public static Schema GetDefaultSchema(this IEnumerable<Schema> schemas)
    {
      // There's always at least one schema in a database, and one of those schemas is a default schema, so this should always work. 
      return schemas.Where(s => s.IsDefaultSchema).First();
    }
  }
}
