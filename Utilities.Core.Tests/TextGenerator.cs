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
    public void PushIndentTest()
    {
      var tg = new TextGenerator();

      Assert.AreEqual(tg.Indent, "");
      Assert.AreEqual(tg.IndentBy, 0);
    }
  }
}
