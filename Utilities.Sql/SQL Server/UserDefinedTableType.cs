using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Utilities.Core;

namespace Utilities.Sql.SqlServer
{
  public class UserDefinedTableType : BaseSqlServerObject
  {
    private Configuration _configuration;

    public Schema Schema { get; private set; }

    private Columns _columns = null;
    public Columns Columns
    {
      get
      {
        if (this._columns == null)
          this.Schema.Database.Server.Configuration.Connection.ExecuteUnderDatabaseInvariant(this.Schema.Database.Name, () => this._columns = null /* new Columns(this) */);

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

    private UserDefinedTableType()
      : base()
    {
    }

    public UserDefinedTableType(String name)
      : this()
    {
      this.Name = name;
    }
  }
}
