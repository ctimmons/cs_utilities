/* See UNLICENSE.txt file for license details. */

using System;

using NUnit.Framework;

namespace Utilities.Sql.SqlServer.Tests
{
  [TestFixture]
  public class IdentifierHelperTests
  {
    [Test]
    public void GetTargetLanguageIdentifierTest()
    {
      /* GetTargetLanguageIdentifier should throw an Exception if called prior to initializing the IdentifierHelper class. */
      Assert.Throws<Exception>(() => IdentifierHelper.GetTargetLanguageIdentifier("valid_identifier"));

      var configuration =
        new Utilities.Sql.SqlServer.Configuration()
        {
          Connection = null, /* Not needed for these unit tests. */
          XmlSystem = XmlSystem.NonLinq_XmlDocument,
          TargetLanguage = TargetLanguage.CSharp_4_0,
          XmlValidationLocation = XmlValidationLocation.PropertySetter
        };

      IdentifierHelper.Init(configuration);

      /* After initialization, bad inputs should throw these exceptions... */

      Assert.Throws<ArgumentNullException>(() => IdentifierHelper.GetTargetLanguageIdentifier(null));
      Assert.Throws<ArgumentException>(() => IdentifierHelper.GetTargetLanguageIdentifier(""));
      Assert.Throws<ArgumentException>(() => IdentifierHelper.GetTargetLanguageIdentifier(" "));

      Action<String, String> areEqual = (expected, actual) => Assert.AreEqual(expected, IdentifierHelper.GetTargetLanguageIdentifier(actual));

      /* ...and good inputs should pass these tests. */

      areEqual("valid_identifier", "valid_identifier");      // Already a valid identifier.
      areEqual("valid_identifier", "valid identifier");      // Spaces converted to underscores.
      areEqual("valid_identifier", "valid.identifier");      // Dots converted to underscores.
      areEqual("_42valid_identifier", "42valid identifier"); // Spaces converted to underscores, and starts with a number.
      areEqual("_delegate", "delegate");                     // Keyword.
      areEqual("_delegate", "_delegate");                    // Already starts with an underscore.
      areEqual("___delegate", "__delegate");                 // C# specific: Starts with two underscores, so prepend a third one.
      areEqual("_42delegate", "42delegate");                 // Starts with a number.
    }

    [Test]
    public void GetNormalizedSqlIdentifierTest()
    {
      Assert.Throws<ArgumentNullException>(() => IdentifierHelper.GetBracketedSqlIdentifier(null));
      Assert.Throws<ArgumentException>(() => IdentifierHelper.GetBracketedSqlIdentifier(""));
      Assert.Throws<ArgumentException>(() => IdentifierHelper.GetBracketedSqlIdentifier(" "));

      Action<String, String> areEqual = (expected, actual) => Assert.AreEqual(expected, IdentifierHelper.GetBracketedSqlIdentifier(actual));

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
