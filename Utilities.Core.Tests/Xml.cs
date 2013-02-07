/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  [Serializable]
  public class TestClass
  {
    [XmlComment(@"
Multi-line comment.
Another line.
")]
    public String StringProperty1 { get; set; }

    [XmlComment(@"Single-line comment.")]
    public Int32 Int32Property1 { get; set; }

    [XmlComment(@"
Multi-line comment.
Another line.
")]
    public List<Int32> ListInt32Property1 { get; set; }

    public TestClass TestClassInstance { get; set; }

    public static TestClass GetInstance()
    {
      var testClass =
        new TestClass()
        {
          StringProperty1 = "Hello, world!",
          Int32Property1 = 42,
          ListInt32Property1 = new List<Int32>() { 1, 2, 3 },
          TestClassInstance =
            new TestClass()
            {
              StringProperty1 = "foo bar baz quux",
              Int32Property1 = 138,
              ListInt32Property1 = new List<Int32>() { 4, 5, 6 },
              TestClassInstance = null
            }
        };

      return testClass;
    }

    public static Boolean AreTestClassInstancesEqual(TestClass expected, TestClass actual)
    {
      return
        (expected.StringProperty1 == actual.StringProperty1) &&
        (expected.Int32Property1 == actual.Int32Property1) &&
        (expected.ListInt32Property1.Count == actual.ListInt32Property1.Count) &&
        Enumerable.SequenceEqual(expected.ListInt32Property1, actual.ListInt32Property1) &&
        (((expected.TestClassInstance != null) && (actual.TestClassInstance != null))
          ? AreTestClassInstancesEqual(expected.TestClassInstance, actual.TestClassInstance)
          : true);
    }
  }

  [TestFixture]
  public class Xml
  {
    public Xml() : base() { }

    [Test]
    public void BinaryFileToFromTest()
    {
      var filename = Path.GetTempFileName();

      XmlUtils.SerializeObjectToBinaryFile(TestClass.GetInstance(), filename);
      try
      {
        var testClass = XmlUtils.DeserializeObjectFromBinaryFile<TestClass>(filename);
        Assert.IsTrue(TestClass.AreTestClassInstancesEqual(TestClass.GetInstance(), testClass));
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

      XmlUtils.SerializeObjectToXmlFile(TestClass.GetInstance(), filename);
      try
      {
        var testClass = XmlUtils.DeserializeObjectFromXmlFile<TestClass>(filename);
        Assert.IsTrue(TestClass.AreTestClassInstancesEqual(TestClass.GetInstance(), testClass));
      }
      finally
      {
        File.Delete(filename);
      }
    }

    [Test]
    public void XDocumentToFromTest()
    {
      var xDocument = XmlUtils.SerializeObjectToXDocument(TestClass.GetInstance());
      var testClass = XmlUtils.DeserializeObjectFromXDocument<TestClass>(xDocument);
      Assert.IsTrue(TestClass.AreTestClassInstancesEqual(TestClass.GetInstance(), testClass));
    }

    [Test]
    public void XmlStringToFromTest()
    {
      var s = XmlUtils.SerializeObjectToXmlString(TestClass.GetInstance());
      var testClass = XmlUtils.DeserializeObjectFromXmlString<TestClass>(s);
      Assert.IsTrue(TestClass.AreTestClassInstancesEqual(TestClass.GetInstance(), testClass));
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

    [Test]
    public void XmlCommentFormattingTest()
    {
      var expected = @"<TestClass xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <!-- 
       Multi-line comment.
       Another line.
        -->
  <StringProperty1>Hello, world!</StringProperty1>
  <!-- Single-line comment. -->
  <Int32Property1>42</Int32Property1>
  <!-- 
       Multi-line comment.
       Another line.
        -->
  <ListInt32Property1>
    <int>1</int>
    <int>2</int>
    <int>3</int>
  </ListInt32Property1>
  <TestClassInstance>
    <!-- 
         Multi-line comment.
         Another line.
          -->
    <StringProperty1>foo bar baz quux</StringProperty1>
    <!-- Single-line comment. -->
    <Int32Property1>138</Int32Property1>
    <!-- 
         Multi-line comment.
         Another line.
          -->
    <ListInt32Property1>
      <int>4</int>
      <int>5</int>
      <int>6</int>
    </ListInt32Property1>
  </TestClassInstance>
</TestClass>";

      var actual = XmlUtils.SerializeObjectToXmlString(TestClass.GetInstance());
      
      /* The two XML strings, 'expected' and 'actual', cannot be directly compared like this:
        
           Assert.AreEqual(expected, actual);

         This is because the serialization process inserts XML namespace attributes
         into the final XML string, and the order of the namespaces is not guaranteed
         nor predictable.

         Since all this test cares about are the XML comments, a regular expression
         and some LINQ magic are used to pluck out the comments and compare them. */

      var xmlCommentRegex = new Regex(@"\<!--.*?--\>", RegexOptions.Singleline);
      var expectedXmlComments = xmlCommentRegex.Matches(expected).Cast<Match>().Select(m => m.Value);
      var actualXmlComments = xmlCommentRegex.Matches(actual).Cast<Match>().Select(m => m.Value);
      Assert.IsTrue(expectedXmlComments.SequenceEqual(actualXmlComments));
    }
  }
}
