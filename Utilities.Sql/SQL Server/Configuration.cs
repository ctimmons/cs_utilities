using System.Data.SqlClient;

namespace Utilities.Sql.SqlServer
{
  /// <summary>
  /// An instance of this class is passed into the <see cref="Utilities.Sql.Server">Server</see> constructor.
  /// </summary>
  public class Configuration
  {
    /// <summary>
    /// A valid <see cref="System.Data.SqlClient.SqlConnection">SqlConnection</see>.
    /// <para>The connection must be open before being passed to the <see cref="Utilities.Sql.Server">Server</see> constructor.</para>
    /// </summary>
    public SqlConnection Connection { get; set; }

    /// <summary>
    /// Specify what kind of CLR datatype (String, XmlDocument, or XDocument) should be used to handle
    /// SQL Server columns of type 'xml'.
    /// </summary>
    public XmlSystem XmlSystem { get; set; }

    /// <summary>
    /// If and where any XML validation code should be generated.
    /// </summary>
    public XmlValidationLocation XmlValidationLocation { get; set; }

    /// <summary>
    /// If your code only calls methods that return TSQL code, then this setting has no affect.
    /// </summary>
    public TargetLanguage TargetLanguage { get; set; }
  }
}
