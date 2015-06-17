using System;

namespace Utilities.Sql.SqlServer
{
  public class UserDefinedTableType : BaseSqlServerObject
  {
    public Schema Schema { get; private set; }

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

    public override string TargetLanguageIdentifier
    {
      get
      {
        return this.Schema.Name + "_" + base.TargetLanguageIdentifier;
      }
    }

    private UserDefinedTableType()
      : base()
    {
    }

    public UserDefinedTableType(String name, Schema schema)
      : this()
    {
      this.Name = name;
      this.Schema = schema;
    }
  }
}
