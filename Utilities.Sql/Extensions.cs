/* See UNLICENSE.txt file for license details. */

using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Utilities.Sql
{
  public static class SqlUtilityExtensionMethods
  {
    /// <summary>
    /// Executes the given sql on the connection, and
    /// returns the result wrapped in a <see cref="System.Data.DataSet">DataSet</see>.
    /// </summary>
    /// <param name="connection"><see cref="System.Data.SqlClient.SqlConnection">SqlConnection</see> the sql is sent to.  The connection must be opened before calling this method.</param>
    /// <param name="sql"><see cref="System.String">String</see> containing sql to execute.</param>
    /// <returns></returns>
    public static DataSet GetDataSet(this SqlConnection connection, String sql)
    {
      using (var command = new SqlCommand() { Connection = connection, CommandType = CommandType.Text, CommandText = sql })
      {
        var dataSet = new DataSet();

        using (var adapter = new SqlDataAdapter())
        {
          adapter.SelectCommand = command;
          adapter.Fill(dataSet);
        }

        return dataSet;
      }
    }

    public static XDocument GetXDocument(this SqlDataReader sqlDataReader, String columnName)
    {
      return sqlDataReader.GetXDocument(sqlDataReader.GetOrdinal(columnName));
    }

    /// <summary>
    /// Retrieving an <see cref="System.Xml.Linq.XDocument">XDocument</see>
    /// from a <see cref="System.Data.SqlClient.SqlDataReader">SqlDataReader</see> involves
    /// creating an intermediate <see cref="System.Xml.XmlReader">XmlReader</see>.
    /// <para>This method takes care of that so the caller doesn't have to.</para>
    /// </summary>
    /// <param name="sqlDataReader"></param>
    /// <param name="columnIndex"></param>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when columnIndex is out of range.</exception>
    /// <returns></returns>
    public static XDocument GetXDocument(this SqlDataReader sqlDataReader, Int32 columnIndex)
    {
      using (var xmlReader = sqlDataReader.GetXmlReader(columnIndex))
        return XDocument.Load(xmlReader);
    }

    public static XElement GetXElement(this SqlDataReader sqlDataReader, String columnName)
    {
      return sqlDataReader.GetXElement(sqlDataReader.GetOrdinal(columnName));
    }

    /// <summary>
    /// Retrieving an <see cref="System.Xml.Linq.XElement">XElement</see>
    /// from a <see cref="System.Data.SqlClient.SqlDataReader">SqlDataReader</see> involves
    /// creating an intermediate <see cref="System.Xml.XmlReader">XmlReader</see>.
    /// <para>This method takes care of that so the caller doesn't have to.</para>
    /// </summary>
    /// <param name="sqlDataReader"></param>
    /// <param name="columnIndex"></param>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when columnIndex is out of range.</exception>
    /// <returns></returns>
    public static XElement GetXElement(this SqlDataReader sqlDataReader, Int32 columnIndex)
    {
      using (var xmlReader = sqlDataReader.GetXmlReader(columnIndex))
        return XElement.Load(xmlReader);
    }

    public static XmlDocument GetXmlDocument(this SqlDataReader sqlDataReader, String columnName)
    {
      return sqlDataReader.GetXmlDocument(sqlDataReader.GetOrdinal(columnName));
    }

    /// <summary>
    /// Retrieving an <see cref="System.Xml.XmlDocument">XmlDocument</see>
    /// from a <see cref="System.Data.SqlClient.SqlDataReader">SqlDataReader</see> involves
    /// creating an intermediate <see cref="System.Xml.XmlReader">XmlReader</see>.
    /// <para>This method takes care of that so the caller doesn't have to.</para>
    /// </summary>
    /// <param name="sqlDataReader"></param>
    /// <param name="columnIndex"></param>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when columnIndex is out of range.</exception>
    /// <returns></returns>
    public static XmlDocument GetXmlDocument(this SqlDataReader sqlDataReader, Int32 columnIndex)
    {
      using (var xmlReader = sqlDataReader.GetXmlReader(columnIndex))
      {
        var xmlDocument = new XmlDocument();
        xmlDocument.Load(xmlReader);
        return xmlDocument;
      }
    }

    /// <summary>
    /// Convert a string containing xml to an <see cref="System.Data.SqlTypes.SqlXml">SqlXml</see> instance.
    /// </summary>
    /// <param name="xml">A <see cref="System.String">String</see> containing xml.</param>
    /// <returns></returns>
    public static SqlXml GetSqlXml(this String xml)
    {
      using (var stringReader = new StringReader(xml))
        using (var xmlTextReader = new XmlTextReader(stringReader))
          return new SqlXml(xmlTextReader);
    }

    /// <summary>
    /// Convert an XNode to an <see cref="System.Data.SqlTypes.SqlXml">SqlXml</see> instance.
    /// </summary>
    /// <param name="xNode">An <see cref="System.Xml.Linq.XNode">XNode</see>.</param>
    /// <returns></returns>
    public static SqlXml GetSqlXml(this XNode xNode)
    {
      using (var xmlReader = xNode.CreateReader())
        return new SqlXml(xmlReader);
    }

    /// <summary>
    /// Convert an XmlDocument to an <see cref="System.Data.SqlTypes.SqlXml">SqlXml</see> instance.
    /// </summary>
    /// <param name="xmlDocument">An <see cref="System.Xml.XmlDocument">XmlDocument</see>.</param>
    /// <param name="xmlNodeType">An <see cref="System.Xml.XmlNodeType">XmlNodeType</see>.</param>
    /// <returns></returns>
    public static SqlXml GetSqlXml(this XmlDocument xmlDocument, XmlNodeType xmlNodeType)
    {
      using (var xmlTextReader = new XmlTextReader(xmlDocument.InnerXml, xmlNodeType, null as XmlParserContext))
        return new SqlXml(xmlTextReader);
    }

    public static T GetValueOrDefault<T>(this DbDataReader dbDataReader, String columnName)
    {
      return dbDataReader.GetValueOrDefault<T>(dbDataReader.GetOrdinal(columnName));
    }

    /// <summary>
    /// Generic method to get a value from a DbDataReader instance.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dbDataReader">A descendent of <see cref="System.Data.Common.DbDataReader">DbDataReader</see>.</param>
    /// <param name="columnIndex"></param>
    /// <returns></returns>
    /// <exception cref="System.InvalidCastException">Thrown when the data returned by dbDataReader is NULL, and T is not a nullable type.</exception>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when columnIndex is out of range.</exception>
    public static T GetValueOrDefault<T>(this DbDataReader dbDataReader, Int32 columnIndex)
    {
      /* If T is not a nullable type, and dbDataReader returns a null value,
         throw an InvalidCastException.
         (See http://msdn.microsoft.com/en-us/library/ms366789%28v=VS.90%29.aspx for info on
         how to correctly identify a nullable type in C#.) */

      var type = typeof(T);
      var isNullableType = (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>)));
      var isNullValue = dbDataReader.IsDBNull(columnIndex);

      if (isNullableType)
        return isNullValue ? default(T) : (T) dbDataReader[columnIndex];
      else if (!isNullValue)
        return (T) dbDataReader[columnIndex];
      else
        throw new InvalidCastException(String.Format(Properties.Resources.NonNullableCast, type.FullName, dbDataReader.GetName(columnIndex), columnIndex));
    }
  }
}
