/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Utilities.Core
{
  public static class XmlUtils
  {
    public static String GetFormattedXml(String xml)
    {
      using (var sw = new StringWriter())
      {
        using (var xmlWriter = new XmlTextWriter(sw))
        {
          xmlWriter.Formatting = Formatting.Indented;

          var xmlDocument = new XmlDocument();
          xmlDocument.LoadXml(xml);
          xmlDocument.WriteTo(xmlWriter);

          return sw.ToString();
        }
      }
    }

    public static T DeserializeObjectFromBinaryFile<T>(String filename)
    {
      using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
        return (T) new BinaryFormatter().Deserialize(fs);
    }

    public static void SerializeObjectToBinaryFile<T>(T value, String filename)
    {
      Directory.CreateDirectory(Path.GetDirectoryName(filename));

      using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
        new BinaryFormatter().Serialize(stream, value);
    }

    public static void SerializeObjectToXmlFile<T>(T value, String filename)
    {
      Directory.CreateDirectory(Path.GetDirectoryName(filename));

      using (var writer = new StreamWriter(filename))
        XmlSerializerCache.GetXmlSerializer(typeof(T)).Serialize(writer, value);
    }

    public static T DeserializeObjectFromXmlFile<T>(String filename)
    {
      using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
        return (T) XmlSerializerCache.GetXmlSerializer(typeof(T)).Deserialize(fs);
    }

    public static XDocument SerializeObjectToXDocument<T>(T value)
    {
      var doc = new XDocument();
      using (var writer = doc.CreateWriter())
      {
        XmlSerializerCache.GetXmlSerializer(typeof(T)).Serialize(writer, value);
        return doc;
      }
    }

    public static T DeserializeObjectFromXDocument<T>(XDocument value)
    {
      var doc = new XDocument(value);
      using (var reader = doc.CreateReader())
        return (T) XmlSerializerCache.GetXmlSerializer(typeof(T)).Deserialize(reader);
    }

    public static String SerializeObjectToXmlString<T>(T value)
    {
      using (var sw = new StringWriter())
      {
        XmlSerializerCache.GetXmlSerializer(typeof(T)).Serialize(sw, value);
        return sw.ToString();
      }
    }

    public static T DeserializeObjectFromXmlString<T>(String s)
    {
      using (var sr = new StringReader(s))
        return (T) XmlSerializerCache.GetXmlSerializer(typeof(T)).Deserialize(sr);
    }

    public static void WriteStartElements(this XmlWriter xmlWriter, params String[] tagNames)
    {
      foreach (var tagName in tagNames)
        xmlWriter.WriteStartElement(tagName);
    }

    public static void WriteEndElements(this XmlWriter xmlWriter, Int32 count)
    {
      for (var i = 0; i < count; i++)
        xmlWriter.WriteEndElement();
    }

    public static void WriteCDataElement(this XmlWriter xmlWriter, String elementName, String cData)
    {
      xmlWriter.WriteStartElement(elementName);
      xmlWriter.WriteAttributeString("type", "text");
      xmlWriter.WriteCData(cData);
      xmlWriter.WriteEndElement();
    }

    public static String GetLastChildsInnerText(this XmlNode node, String xpath)
    {
      var singleNode = node.SelectSingleNode(xpath);
      return (singleNode == null) ? String.Empty : singleNode.LastChild.InnerText;
    }

    public static String GetNodesInnerText(this XmlNode node, String xpath)
    {
      var singleNode = node.SelectSingleNode(xpath);
      return (singleNode == null) ? String.Empty : singleNode.InnerText;
    }
  }

  /* Each construction of an XmlSerializer instance creates an assembly which is loaded
     into the current app domain.  Such assemblies are not re-used, nor can they be unloaded
     from the app domain, so a memory leak occurs.  This cache class prevents those memory leaks
     by re-using the XmlSerializers based on they type they were constructed for. */
  public static class XmlSerializerCache
  {
    private static readonly ConcurrentDictionary<Type, XmlSerializer> _xmlSerializers = new ConcurrentDictionary<Type, XmlSerializer>();

    public static XmlSerializer GetXmlSerializer(Type type)
    {
      return _xmlSerializers.GetOrAdd(type, new XmlSerializer(type));
    }
  }

  [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
  public class XmlCommentAttribute : Attribute
  {
    private String _value = String.Empty;
    public string Value
    {
      /* If the return value isn't surrounded with spaces,
         the XML comment comes out looking like this:
         
           <!--Xml comment-->
           
       */
      get { return " " + this._value + " "; }
      set { this._value = value; }
    }
  }
}
