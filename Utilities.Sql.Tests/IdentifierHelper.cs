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

      /* After initialization, bad data should throw these exceptions... */

      Assert.Throws<ArgumentNullException>(() => IdentifierHelper.GetTargetLanguageIdentifier(null));
      Assert.Throws<ArgumentException>(() => IdentifierHelper.GetTargetLanguageIdentifier(""));
      Assert.Throws<ArgumentException>(() => IdentifierHelper.GetTargetLanguageIdentifier(" "));

      /* ...and good data should pass these tests. */

      Assert.AreEqual(IdentifierHelper.GetTargetLanguageIdentifier("valid_identifier"), "valid_identifier");      // Already a valid identifier.
      Assert.AreEqual(IdentifierHelper.GetTargetLanguageIdentifier("valid identifier"), "valid_identifier");      // Spaces converted to underscores.
      Assert.AreEqual(IdentifierHelper.GetTargetLanguageIdentifier("valid.identifier"), "valid_identifier");      // Dots converted to underscores.
      Assert.AreEqual(IdentifierHelper.GetTargetLanguageIdentifier("42valid identifier"), "_42valid_identifier"); // Spaces converted to underscores, and starts with a number.
      Assert.AreEqual(IdentifierHelper.GetTargetLanguageIdentifier("delegate"), "_delegate");                     // Keyword.
      Assert.AreEqual(IdentifierHelper.GetTargetLanguageIdentifier("_delegate"), "_delegate");                    // Already starts with an underscore.
      Assert.AreEqual(IdentifierHelper.GetTargetLanguageIdentifier("__delegate"), "___delegate");                 // C# specific: Starts with two underscores, so prepend a third one.
      Assert.AreEqual(IdentifierHelper.GetTargetLanguageIdentifier("42delegate"), "_42delegate");                 // Starts with a number.
    }

    [Test]
    public void GetNormalizedSqlIdentifierTest()
    {
      Assert.Throws<ArgumentNullException>(() => IdentifierHelper.GetNormalizedSqlIdentifier(null));
      Assert.Throws<ArgumentException>(() => IdentifierHelper.GetNormalizedSqlIdentifier(""));
      Assert.Throws<ArgumentException>(() => IdentifierHelper.GetNormalizedSqlIdentifier(" "));

      Action<String, String> areEqual = (expected, actual) => Assert.AreEqual(expected, IdentifierHelper.GetNormalizedSqlIdentifier(actual));

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
