/* See the LICENSE.txt file in the root folder for license details. */

using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  [TestFixture]
  public class TextGeneratorTests
  {
    public TextGeneratorTests() : base() { }

    [Test]
    public void ClearIndentTest()
    {
      var tg = new TextGenerator();

      tg.PushIndent(4);
      tg.ClearIndent();
      tg.Write("x");
      Assert.AreEqual("x", tg.Content);
    }

    [Test]
    public void PushIndentTest1()
    {
      var tg = new TextGenerator();

      tg.Write("x");
      Assert.AreEqual("x", tg.Content);

      tg.PushIndent(4);
      tg.Write("x");
      Assert.AreEqual("x    x", tg.Content);
    }

    [Test]
    public void PopIndentTest()
    {
      var tg = new TextGenerator();

      tg.Write("x");
      Assert.AreEqual("x", tg.Content);

      tg.PushIndent(4);
      tg.Write("x");
      Assert.AreEqual("x    x", tg.Content);

      tg.PushIndent(2);
      tg.Write("x");
      Assert.AreEqual("x    x      x", tg.Content);

      tg.PopIndent(); // -2.  indent should now be 4.
      tg.Write("x");
      Assert.AreEqual("x    x      x    x", tg.Content);

      tg.PopIndent(); // -4.  indent should now be 0.
      tg.Write("x");
      Assert.AreEqual("x    x      x    xx", tg.Content);

      /* Trying to pop the indent when it's zero should be a no-op. */
      tg.PopIndent();
      tg.Write("x");
      Assert.AreEqual("x    x      x    xxx", tg.Content);
    }

    [Test]
    public void ClearContentTest()
    {
      var tg = new TextGenerator();

      tg.Write("x");
      Assert.AreEqual("x", tg.Content);

      tg.ClearContent();
      Assert.AreEqual("", tg.Content);

      tg.Write("x");
      Assert.AreEqual("x", tg.Content);
    }

    [Test]
    public void SetStandardIndentStringTest()
    {
      var tg = new TextGenerator();

      tg.Write("x");
      tg.SetStandardIndentString(4);
      tg.PushIndent();
      tg.Write("x");
      Assert.AreEqual("x    x", tg.Content);
    }

    [Test]
    public void PushIndentTest2()
    {
      var expected = @"namespace Foo
{
  public class Bar
  {
      // Hello, world!
  }
}
";

      var tg = new TextGenerator();
      tg.SetStandardIndentString(2);

      tg.WriteLine("namespace Foo");
      tg.WriteLine("{");

      tg.PushIndent(); // Uses the standard indent string set above.
      tg.WriteLine("public class Bar");
      tg.WriteLine("{");

      tg.PushIndent(4); // Does not use the standard indent string.
      tg.WriteLine("// Hello, world!");
      tg.PopIndent();

      tg.WriteLine("}");
      tg.PopIndent();

      tg.WriteLine("}");

      Assert.AreEqual(expected, tg.Content);
    }
  }
}
