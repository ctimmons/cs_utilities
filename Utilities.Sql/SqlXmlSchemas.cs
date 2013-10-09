using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Xml;
using System.Xml.Schema;

namespace Utilities.Sql
{
  /// <summary>
  /// A cache of all of the XML collection schemas (XmlSchemaSets) in all databases(*) on an SQL Server instance.
  /// <para>Client-side code can use these cached XmlSchemaSets to perform client-side validation
  /// of XML documents and fragments before inserting/updating an SQL Server database column of type XML.</para>
  /// <para>(*) All databases except the master, model, msdb, and tempdb databases.</para>
  /// </summary>
  public class SqlXmlSchemas
  {
    public static SqlXmlSchemas Instance = null;

    public static void InitializeInstance(SqlConnection connection)
    {
      if (Instance == null)
        Instance = new SqlXmlSchemas(connection);
    }
    
    private class ThreeStringTupleEqualityComparer : IEqualityComparer<Tuple<String, String, String>>
    {
      public Boolean Equals(Tuple<String, String, String> x, Tuple<String, String, String> y)
      {
        if (Object.ReferenceEquals(x, y))
          return true;
        else if ((x == null) || (y == null))
          return false;
        else
          return
            x.Item1.Equals(y.Item1, StringComparison.CurrentCultureIgnoreCase) &&
            x.Item2.Equals(y.Item2, StringComparison.CurrentCultureIgnoreCase) &&
            x.Item3.Equals(y.Item3, StringComparison.CurrentCultureIgnoreCase);
      }

      public int GetHashCode(Tuple<String, String, String> x)
      {
        return (x == null) ? 0 : (x.Item1.GetHashCode() ^ x.Item2.GetHashCode() ^ x.Item3.GetHashCode());
      }
    }

    private Dictionary<Tuple<String, String, String>, XmlSchemaSet> _databases = new Dictionary<Tuple<String, String, String>, XmlSchemaSet>(new ThreeStringTupleEqualityComparer());

    public SqlXmlSchemas(SqlConnection connection)
      : base()
    {
      var sql = @"
SELECT
    [SCHEMA_NAME] = S.name,
    XML_COLLECTION_NAME = COALESCE(XMLCOLL.name, ''),
    XSD = XML_SCHEMA_NAMESPACE(S.[name], COALESCE(XMLCOLL.name, ''))
  FROM
    sys.schemas AS S
    INNER JOIN sys.xml_schema_collections AS XMLCOLL ON XMLCOLL.schema_id = S.schema_id
  WHERE
    S.name <> 'sys'";

      var databases =
        connection
        .GetSchema("Databases")
        .Select("database_name not in ('master', 'model', 'msdb', 'tempdb')");

      foreach (DataRow row in databases)
      {
        var databaseName = row["database_name"].ToString();
        connection.ExecuteUnderDatabaseInvariant(databaseName,
          () =>
          {
            using (var command = new SqlCommand() { Connection = connection, CommandType = CommandType.Text, CommandText = sql })
            {
              using (var sqlDataReader = command.ExecuteReader())
              {
                while (sqlDataReader.Read())
                {
                  var tuple = Tuple.Create(
                    databaseName,
                    sqlDataReader.GetValueOrDefault<String>("SCHEMA_NAME"),
                    sqlDataReader.GetValueOrDefault<String>("XML_COLLECTION_NAME"));
                  var xmlSchemaSet = this.GetXmlSchemaSetForColumn(sqlDataReader, "XSD");

                  this._databases.Add(tuple, xmlSchemaSet);
                }
              }
            }
          });
      }
    }

    private XmlSchemaSet GetXmlSchemaSetForColumn(SqlDataReader sqlDataReader, String columnName)
    {
      using (var xmlReader = sqlDataReader.GetSqlXml(sqlDataReader.GetOrdinal(columnName)).CreateReader())
      {
        var schemaSet = new XmlSchemaSet();

        while (xmlReader.Read())
          if (xmlReader.NodeType == XmlNodeType.Element)
            schemaSet.Add(null /* Use the targetNamespace specified in the schema. */, xmlReader);

        schemaSet.Compile();
        return schemaSet;
      }
    }

    public XmlSchemaSet GetXmlSchemaSet(String databaseName, String schemaName, String xmlCollectionName)
    {
      return this._databases[Tuple.Create(databaseName, schemaName, xmlCollectionName)];
    }
  }
}
