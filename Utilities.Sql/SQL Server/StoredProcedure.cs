using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using Utilities.Core;

namespace Utilities.Sql.SqlServer
{
  public class StoredProcedure
  {
    public Schema Schema { get; private set; }
    public String Name { get; private set; }
    public Int32 VersionNumber { get; private set; }
    public SqlParameter[] SqlParameters { get; private set; }

    private static Char[] _braces = "[]".ToCharArray();

    private Columns _columns = null;
    public Columns Columns
    {
      get
      {
        if (this._columns == null)
          this.Schema.Database.Server.Configuration.Connection.ExecuteUnderDatabaseInvariant(this.Schema.Database.Name, () => this._columns = new Columns(this));

        return this._columns;
      }
    }

    public String SqlIdentifier
    {
      get
      {
        return String.Concat(this.Schema.Name, ".", this.Name);
      }
    }

    public String TargetLanguageIdentifier
    {
      get
      {
        return String.Concat(this.Schema.Name.Trim(_braces), "_", this.Name.Trim(_braces), "_", this.VersionNumber);
      }
    }

    private StoredProcedure()
      : base()
    {
    }

    public StoredProcedure(Schema schema, String name, Int32 versionNumber, SqlParameter[] sqlParameters)
      : this()
    {
      this.Schema = schema;
      this.Name = name;
      this.VersionNumber = versionNumber;
      this.SqlParameters = sqlParameters;
    }
  }

  public static class StoredProcedureExtensions
  {
    public static StoredProcedure GetByName(this IEnumerable<StoredProcedure> storedProcedures, String name)
    {
      return storedProcedures.GetByName(name, 1);
    }

    public static StoredProcedure GetByName(this IEnumerable<StoredProcedure> storedProcedures, String name, Int32 versionNumber)
    {
      return storedProcedures.Where(sp => sp.Name.EqualsCI(name) && (sp.VersionNumber == versionNumber)).FirstOrDefault();
    }
  }
}
