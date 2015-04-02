using System;

namespace Utilities.Sql.SqlServer
{
  public class Table
  {
    public Schema Schema { get; private set; }

    /// <summary>
    /// The table name as it appears on the database server.
    /// </summary>
    public String Name { get; private set; }

    /// <summary>
    /// This class is named Table, but it handles both tables and views.  This property indicates what a Table instance really contains.
    /// </summary>
    public Boolean IsView { get; private set; }

    /// <summary>
    /// In SQL Server 2005 and later, a table name in a database is not necessarily unique,
    /// because the same table name can be used in different schemas.
    /// <para>This property produces a unique table identifier by combining the schema name
    /// and table name.  The two components are also rendered safe by wrapping them in 
    /// square brackets.  This makes it useful when generating TSQL code.</para>
    /// </summary>
    public String SchemaNameAndTableName
    {
      get { return String.Format("[{0}].[{1}]", this.Schema.Name, this.Name); }
    }

    private String _targetLanguageTableIdentifier = null;
    /// <summary>
    /// The table name converted for use as a valid identifier in generated target language code.
    /// </summary>
    public String TargetLanguageTableIdentifier
    {
      get
      {
        if (this._targetLanguageTableIdentifier == null)
          this._targetLanguageTableIdentifier = IdentifierHelper.GetTargetLanguageIdentifier(this.Schema.Name + "_" + this.Name);

        return this._targetLanguageTableIdentifier;
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

    public Table(Schema schema, String name, Boolean isView)
      : base()
    {
      this.Schema = schema;
      this.Name = name;
      this.IsView = isView;
    }
  }
}
