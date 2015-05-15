using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using Utilities.Core;

namespace Utilities.Sql.SqlServer
{
  /*
    multiple result sets
    HasResultSet property
    Func<Boolean, T> booleanConverter
  
  */

  public class StoredProcedure : BaseSqlServerObject
  {
    public Schema Schema { get; private set; }
    public Int32 VersionNumber { get; private set; }
    public SqlParameter[] SqlParameters { get; private set; }
    public Boolean HasResultSet { get; private set; }

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
        return String.Concat(this.Schema.BracketedName, ".", this.BracketedName);
      }
    }

    public override String TargetLanguageIdentifier
    {
      get
      {
        return String.Concat(this.Schema.Name, "_", this.Name, "_", this.VersionNumber.ToString());
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
      this.Name = IdentifierHelper.GetStrippedSqlIdentifier(name);
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
