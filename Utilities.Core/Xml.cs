/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
      xml.Check("xml");

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

      SerializeObjectToXDocument(value).Save(filename);
    }

    public static T DeserializeObjectFromXmlFile<T>(String filename)
    {
      using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
        return (T) XmlSerializerCache.GetXmlSerializer(typeof(T)).Deserialize(fs);
    }

    public static XDocument SerializeObjectToXDocument<T>(T value)
    {
      value.CheckForNull("value");

      var doc = new XDocument();
      using (var writer = doc.CreateWriter())
        XmlSerializerCache.GetXmlSerializer(typeof(T)).Serialize(writer, value);

      /* This must be executed after writer has been closed. */
      InsertXmlComments(value, doc.Elements(), 1);
      return doc;
    }

    public static T DeserializeObjectFromXDocument<T>(XDocument value)
    {
      var doc = new XDocument(value);
      using (var reader = doc.CreateReader())
        return (T) XmlSerializerCache.GetXmlSerializer(typeof(T)).Deserialize(reader);
    }

    public static String SerializeObjectToXmlString<T>(T value)
    {
      return SerializeObjectToXDocument(value).ToString();
    }

    public static T DeserializeObjectFromXmlString<T>(String s)
    {
      using (var sr = new StringReader(s))
        return (T) XmlSerializerCache.GetXmlSerializer(typeof(T)).Deserialize(sr);
    }

    private static readonly Type _xmlCommentAttributeType = typeof(XmlCommentAttribute);
    private static readonly Boolean _shouldSearchInheritanceChain = false;

    /* Any XmlCommentAttributes in obj are inserted into xElements as XML comments (XComment objects).
    
       This method assumes xElements is a serialization of obj.
       If that's not the case, the behavior of this method is unpredictable. */
    private static void InsertXmlComments(Object obj, IEnumerable<XElement> xElements, Int32 level)
    {
      /* Base case. */
      if ((obj == null) || !xElements.Any())
        return;

      Func<XmlCommentAttribute, XComment> getXCommentWithIndentedText =
        xmlCommentAttribute =>
        {
          if (xmlCommentAttribute.ShouldIndent)
          {
            var prefixSpaces = " ".Repeat((level * xmlCommentAttribute.IndentSize) + "<!-- ".Length);
            return new XComment(xmlCommentAttribute.Value.Replace(Environment.NewLine, Environment.NewLine + prefixSpaces));
          }
          else
          {
            return new XComment(xmlCommentAttribute.Value);
          }
        };

      /* Recursive case. */
      foreach (var propertyInfo in obj.GetType().GetProperties())
      {
        if (propertyInfo.CanRead && propertyInfo.GetIndexParameters().Length == 0)
        {
          if (propertyInfo.IsDefined(_xmlCommentAttributeType, _shouldSearchInheritanceChain))
          {
            var xmlComment =
              propertyInfo
              .GetCustomAttributes(_xmlCommentAttributeType, _shouldSearchInheritanceChain)
              .Cast<XmlCommentAttribute>()
              .Single();

            xElements
            .Elements(propertyInfo.Name)
            .Single()
            .AddBeforeSelf(getXCommentWithIndentedText(xmlComment));
          }

          InsertXmlComments(propertyInfo.GetValue(obj, null), xElements.Elements(propertyInfo.Name), level + 1);
        }
      }
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
     into the current app domain.  When the XmlSerializer instance is garbage collected,
     the assembly it created is not unloaded from the app domain, nor can the assembly
     be re-used, so a memory leak occurs.  This cache class prevents those memory leaks
     by re-using the XmlSerializers based on they type they were constructed for. */
  public static class XmlSerializerCache
  {
    private static readonly ConcurrentDictionary<Type, XmlSerializer> _xmlSerializers = new ConcurrentDictionary<Type, XmlSerializer>();

    public static XmlSerializer GetXmlSerializer(Type type)
    {
      return _xmlSerializers.GetOrAdd(type, new XmlSerializer(type));
    }
  }

  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
  public class XmlCommentAttribute : Attribute
  {
    private String _value = String.Empty;

    public XmlCommentAttribute(String value)
      : base()
    {
      this._value = value;
      this.ShouldIndent = true;
      this.IndentSize = 2;
    }

    public String Value
    {
      /* If the return value isn't surrounded with spaces,
         the XML comment comes out looking like this:
         
           <!--Xml comment-->
           
       */
      get { return " " + this._value + " "; }
      set { this._value = value; }
    }

    public Boolean ShouldIndent { get; set; }
    public Int32 IndentSize { get; set; }
  }
}
