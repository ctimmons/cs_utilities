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
          this._bracketedName = IdentifierHelper.GetNormalizedSqlIdentifier(this.Name);

        return this._bracketedName;
      }
    }

    private String _targetLanguageIdentifier = null;
    /// <summary>
    /// This column's name, converted to a valid identifier in the target language.
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
