/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public void PushIndentTest()
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
  }
}
