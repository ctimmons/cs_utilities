/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.Collections.Generic;
using System.Linq;

using Utilities.Core;

namespace Utilities.Sql.SqlServer
{
  [Flags]
  public enum TableType
  {
    View = 0,
    Table = 0b0001,
    HistoryTable = 0b0010,
    SystemVersionedTemporalTable = 0b0100
  }

  public class Table : BaseSqlServerObject
  {
    public Schema Schema { get; private set; }

    public TableType TableType { get; private set; }

    /// <summary>
    /// This class is named Table, but it handles both tables and views.  This property indicates what a Table instance really contains.
    /// </summary>
    public Boolean IsView => (this.TableType == TableType.View);

    public Boolean IsHistoryTable => (this.TableType == TableType.HistoryTable);

    /// <summary>
    /// In SQL Server 2005 and later, a table name in a database is not necessarily unique,
    /// because the same table name can be used in different schemas.
    /// <para>This property produces a unique table identifier by combining the schema name
    /// and table name.  The two components are also rendered safe by wrapping them in 
    /// square brackets.  This makes it useful when generating TSQL code.</para>
    /// </summary>
    public String SchemaNameAndTableName
    {
      get { return String.Format("{0}.{1}", this.Schema.BracketedName, this.BracketedName); }
    }

    public override string TargetLanguageIdentifier
    {
      get
      {
        return this.Schema.Name + "_" + base.TargetLanguageIdentifier;
      }
    }

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

    public Table(Schema schema, String name, TableType tableType)
      : base()
    {
      this.Schema = schema;
      this.Name = IdentifierHelper.GetStrippedSqlIdentifier(name);
      this.TableType = tableType;
    }
  }

  public static class TableExtensions
  {
    public static Table GetByName(this IEnumerable<Table> tables, String name)
    {
      return tables.Where(table => table.Name.EqualsCI(name)).FirstOrDefault();
    }
  }
}
