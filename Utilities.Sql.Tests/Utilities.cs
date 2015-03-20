/* See UNLICENSE.txt file for license details. */

using System;

using NUnit.Framework;

namespace Utilities.Sql.Tests
{
  [TestFixture]
  public class SqlUtilitiesTests
  {
    [Test]
    public void GetNormalizedSqlIdentifierTest()
    {
      Assert.Throws<ArgumentNullException>(() => SqlUtilities.GetNormalizedSqlIdentifier(null));

      Action<String, String> areEqual = (expected, actual) => Assert.AreEqual(expected, SqlUtilities.GetNormalizedSqlIdentifier(actual));

      areEqual("", "");

      areEqual(".", ".");
      areEqual("..", "..");

      areEqual("[a]", "a");
      areEqual("[a]", "[a");
      areEqual("[a]", "a]");
      areEqual("[a]", "[a]");
      areEqual("[a]", "[[a]]");

      areEqual("[a].[b]", "[a].b");
      areEqual("[a].[b]", "a.[b]");
      areEqual("[a].[b]", "[a].[b]");

      areEqual("[a]..[b]", "[a]..b");
      areEqual("[a]..[b]", "a..[b]");
      areEqual("[a]..[b]", "[a]..[b]");
    }
  }
}
