using System;

namespace Utilities.Sql.SqlServer
{
  public class BaseSqlServerObject
  {
    /// <summary>
    /// The undelimited name of the SQL Server object.
    /// </summary>
    public String Name { get; set; }

    private String _bracketedName = null;
    /// <summary>
    /// The delimited name of the SQL Server object, where the name is surrounded with square brackets (e.g. [My Table]).
    /// </summary>
    public String BracketedName
    {
      get
      {
        if (this._bracketedName == null)
          this._bracketedName = IdentifierHelper.GetBracketedSqlIdentifier(this.Name);

        return this._bracketedName;
      }
    }

    private String _sqlIdentifier = null;
    /// <summary>
    /// This SQL Server object's name, converted to a valid identifier for use in TSQL.
    /// </summary>
    public virtual String SqlIdentifier
    {
      get
      {
        if (this._sqlIdentifier == null)
          this._sqlIdentifier = "@" + this.Name.Replace(" ", "_");

        return this._sqlIdentifier;
      }
    }

    private String _targetLanguageIdentifier = null;
    /// <summary>
    /// This SQL Server object's name, converted to a valid identifier in the target language.
    /// </summary>
    public virtual String TargetLanguageIdentifier
    {
      get
      {
        if (this._targetLanguageIdentifier == null)
          this._targetLanguageIdentifier = IdentifierHelper.GetTargetLanguageIdentifier(this.Name);

        return this._targetLanguageIdentifier;
      }
    }

    /// <summary>
    /// A simple target language identifier primarily used as the name for a property's backing store (e.g. "_customername" in "private String _customername").
    /// </summary>
    public String TargetLanguageBackingStoreIdentifier
    {
      get { return "_" + this.TargetLanguageIdentifier; }
    }
  }
}
