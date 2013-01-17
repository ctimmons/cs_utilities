/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  [TestFixture]
  public class Xml
  {
    public Xml() : base() { }

    [Serializable]
    public class TestClass
    {
      public String StringProperty { get; set; }
      public Int32 Int32Property { get; set; }
      public List<Int32> ListInt32Property { get; set; }

      public TestClass()
        : base()
      {
      }
    }

    private TestClass _testClassInstance = new TestClass()
    {
      StringProperty = "Hello, world!",
      Int32Property = 42,
      ListInt32Property = new List<Int32>() { 1, 2, 3 }
    };

    private Boolean AreTestClassInstancesEqual(TestClass testClass1, TestClass testClass2)
    {
      /* TODO: Write generic method to compare the values of any two class' instance and static members.
               Add code to detect cycles to prevent runaway recursion. */
      var result =
        (testClass1.StringProperty == testClass2.StringProperty) &&
        (testClass1.Int32Property == testClass2.Int32Property) &&
        (testClass1.ListInt32Property.Count == testClass2.ListInt32Property.Count);

      if (result)
        for (var n = 0; n < testClass1.ListInt32Property.Count; n++)
          if (testClass1.ListInt32Property[n] != testClass2.ListInt32Property[n])
            return false;

      return result;
    }

    [Test]
    public void BinaryFileToFromTest()
    {
      var filename = Path.GetTempFileName();

      XmlUtils.SerializeObjectToBinaryFile(this._testClassInstance, filename);
      try
      {
        var testClass = XmlUtils.DeserializeObjectFromBinaryFile<TestClass>(filename);
        Assert.IsTrue(AreTestClassInstancesEqual(this._testClassInstance, testClass));
      }
      finally
      {
        File.Delete(filename);
      }
    }

    [Test]
    public void XmlFileToFromTest()
    {
      var filename = Path.GetTempFileName();

      XmlUtils.SerializeObjectToXmlFile(this._testClassInstance, filename);
      try
      {
        var testClass = XmlUtils.DeserializeObjectFromXmlFile<TestClass>(filename);
        Assert.IsTrue(AreTestClassInstancesEqual(this._testClassInstance, testClass));
      }
      finally
      {
        File.Delete(filename);
      }
    }

    [Test]
    public void XDocumentToFromTest()
    {
      var xDocument = XmlUtils.SerializeObjectToXDocument(this._testClassInstance);
      var testClass = XmlUtils.DeserializeObjectFromXDocument<TestClass>(xDocument);
      Assert.IsTrue(AreTestClassInstancesEqual(this._testClassInstance, testClass));
    }

    [Test]
    public void XmlStringToFromTest()
    {
      var s = XmlUtils.SerializeObjectToXmlString(this._testClassInstance);
      var testClass = XmlUtils.DeserializeObjectFromXmlString<TestClass>(s);
      Assert.IsTrue(AreTestClassInstancesEqual(this._testClassInstance, testClass));
    }

    [Test]
    public void GetFormattedXmlTest()
    {
      var input = @"<test><element>value 1</element><element>value 2</element><element>value 3</element></test>";
      var expectedOutput = 
@"<test>
  <element>value 1</element>
  <element>value 2</element>
  <element>value 3</element>
</test>";

      Assert.AreEqual(expectedOutput, XmlUtils.GetFormattedXml(input));
    }

    private XmlWriterSettings _xmlWriterSettings = new XmlWriterSettings() { Indent = true, IndentChars = "  ", NewLineOnAttributes = true };

    [Test]
    public void WriteStartAndEndElementsTest()
    {
      var actual = new StringBuilder();
      var expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Root>
  <Child>
    <Grandchild>text</Grandchild>
  </Child>
  <Child>
    <Grandchild>text</Grandchild>
  </Child>
</Root>";

      using (var writer = XmlWriter.Create(actual, this._xmlWriterSettings))
      {
        writer.WriteStartElements("Root", "Child", "Grandchild");
        writer.WriteString("text");
        writer.WriteEndElements(2);
        writer.WriteStartElements("Child", "Grandchild");
        writer.WriteString("text");
        writer.WriteEndElements(3);
        writer.Flush();

        Assert.AreEqual(expected, actual.ToString());
      }
    }

    [Test]
    public void WriteCDataElementTest()
    {
      var actual = new StringBuilder();
      var expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Root>
  <Child>
    <CDATA_Test
      type=""text""><![CDATA[<script>alert('Hello, world!');</script>]]></CDATA_Test>
  </Child>
</Root>";

      using (var writer = XmlWriter.Create(actual, this._xmlWriterSettings))
      {
        writer.WriteStartElements("Root", "Child");
        writer.WriteCDataElement("CDATA_Test", "<script>alert('Hello, world!');</script>");
        writer.WriteEndElements(2);
        writer.Flush();

        Assert.AreEqual(expected, actual.ToString());
      }
    }

    [Test]
    public void GetLastChildsInnerTextTest()
    {
      var xmlText = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Root>
  <Child>first text</Child>
  <Child>middle text</Child>
  <Child>last text</Child>
</Root>";

      var xmlDoc = new XmlDocument();
      xmlDoc.LoadXml(xmlText);
      Assert.AreEqual("last text", xmlDoc.GetLastChildsInnerText("/Root"));
    }

    [Test]
    public void GetNodesInnerTextTest()
    {
      var xmlText = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Root>
  <Child>first text</Child>
  <Child>middle text</Child>
  <Child>last text</Child>
</Root>";

      var xmlDoc = new XmlDocument();
      xmlDoc.LoadXml(xmlText);
      Assert.AreEqual("middle text", xmlDoc.GetNodesInnerText("/Root/Child[2]"));
    }
  }
}
