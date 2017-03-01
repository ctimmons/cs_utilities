/* See the LICENSE.txt file in the root folder for license details. */

using System.Collections.Generic;
using System.Data;

namespace Utilities.Sql.SqlServer
{
  /// <summary>
  /// Server and its related classes allow T4 templates to
  /// use SQL Server metadata to easily generate database access code.
  /// <para>The class hierarchy is quite simple:  A Server contains zero or more Schemas,
  /// a schema contains zero or more Databases,
  /// a Database contains zero or more Tables, and a Table contains one or more Columns.
  /// (The Table class is used for both tables and views.  They're differentiated by the Table.IsView property.)</para>
  /// <para>For more examples, see the https://github.com/ctimmons/t4_sql_examples solution.</para>
  /// <example>
  /// To create a Server instance, create a connection and open it.  Then create a Configuration
  /// instance and pass it to the Server constructor.
  /// <code>
  /// using (var connection = new SqlConnection("Data Source=laptop2;Initial Catalog=AdventureWorks2012;Integrated Security=true;"))
  /// {
  ///   connection.Open();
  ///    
  ///   var configuration =
  ///     new Configuration()
  ///     {
  ///       Connection = connection,
  ///       XmlSystem = XmlSystem.Linq_XDocument,
  ///       TargetLanguage = TargetLanguage.CSharp,
  ///       XmlValidationLocation = XmlValidationLocation.PropertySetter
  ///     };
  /// 
  ///   var server = new Server(configuration);
  ///  
  ///   // Code that uses the server instance here...
  /// }
  /// </code>
  /// </example>
  /// <example>
  /// Once a Server instance is created, it can be used to retrieve metadata for the databases,
  /// tables, and columns on that server.
  /// <code>
  /// // Find a specific table in a database:
  /// var personTable =
  ///   server
  ///   .Databases["AdventureWorks2012"] // Case-insensitive names.
  ///   .Schemas["Person"]
  ///   .Tables["Person"]
  ///   .First();
  ///   
  /// // Get a ready-made list of target language method parameter declarations
  /// // for use in an update method:
  /// var parameterDeclarations = personTable.Columns.GetTargetLanguageMethodIdentifiersAndTypes(ColumnType.CanAppearInUpdateSetClause));
  /// </code>
  /// </example>
  /// </summary>
  public class Server
  {
    public Configuration Configuration { get; private set; }

    private List<Database> _databases = null;
    public List<Database> Databases
    {
      get
      {
        if (this._databases == null)
        {
          this._databases = new List<Database>();
          var table = this.Configuration.Connection.GetSchema("Databases");
          foreach (DataRow row in table.Rows)
            this._databases.Add(new Database(this, row["database_name"].ToString()));
        }

        return this._databases;
      }
    }

    public Server(Configuration configuration)
      : base()
    {
      IdentifierHelper.Init(configuration);
      this.Configuration = configuration;
    }
  }
}
