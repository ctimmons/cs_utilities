/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  [TestFixture]
  public class StringUtilsTests
  {
    public StringUtilsTests() : base() { }

    [Test]
    public void As0Or1Test()
    {
      Helpers.AssertionIsTrue(false, false.As0Or1(), '0');
      Helpers.AssertionIsTrue(true, true.As0Or1(), '1');
    }

    [Test]
    public void AsYOrNTest()
    {
      Helpers.AssertionIsTrue(false, false.AsYOrN(), 'N');
      Helpers.AssertionIsTrue(true, true.AsYOrN(), 'Y');
    }

    [Test]
    public void AsBooleanTest()
    {
      /* The strings '1', 'true', 't', 'yes' and 'y' should all return true (case insensitive). 
         Anything else should return false. */

      foreach (var trueInput in new[] { "1", "true", "TRUE", "t", "T", "yes", "YES", "y", "Y" })
        Helpers.AssertionIsTrue(trueInput, trueInput.AsBoolean(), true);

      foreach (var falseInput in new[] { "", "0", "No", "False", "asfsadfdsf", null })
        Helpers.AssertionIsTrue(falseInput, falseInput.AsBoolean(), false);
    }

    [Test]
    public void RepeatTest()
    {
      String str = null;
      var count = 0;
      Assert.Throws<ArgumentNullException>(() => str.Repeat(count));

      str = "";
      count = 0;
      Assert.AreEqual("", str.Repeat(count), "An empty string that's not repeated should be an empty string.");

      str = "";
      count = 3;
      Assert.AreEqual("", str.Repeat(count), "A repeated empty string should be an empty string.");

      str = "a";
      count = 0;
      Assert.AreEqual("a", str.Repeat(count));

      str = "a";
      count = 2;
      Assert.AreEqual("aa", str.Repeat(count));

      str = "a";
      count = 10;
      Assert.AreEqual("aaaaaaaaaa", str.Repeat(count));
    }

    [Test]
    public void LastCharTest()
    {
      String s = null;
      Assert.Throws<ArgumentNullException>(() => s.LastChar());

      s = "a";
      Assert.AreEqual('a', s.LastChar());

      s = "ab";
      Assert.AreEqual('b', s.LastChar());
    }

    [Test]
    public void SurroundWithTest()
    {
      String s = null;
      String delimiter = null;
      Assert.Throws<ArgumentNullException>(() => s.SurroundWith(delimiter));

      s = "";
      Assert.Throws<ArgumentNullException>(() => s.SurroundWith(delimiter));

      s = null;
      delimiter = "";
      Assert.Throws<ArgumentNullException>(() => s.SurroundWith(delimiter));

      s = "";
      delimiter = "";
      Assert.AreEqual("", s.SurroundWith(delimiter));

      s = "";
      delimiter = "'";
      Assert.AreEqual("''", s.SurroundWith(delimiter));

      s = "a";
      delimiter = "'";
      Assert.AreEqual("'a'", s.SurroundWith(delimiter));
    }

    [Test]
    public void MD5ChecksumTest()
    {
      /* Correct test values from RFC 1321 (http://www.faqs.org/rfcs/rfc1321.html)
    
         MD5 ("") = d41d8cd98f00b204e9800998ecf8427e
         MD5 ("a") = 0cc175b9c0f1b6a831c399e269772661
         MD5 ("abc") = 900150983cd24fb0d6963f7d28e17f72
         MD5 ("message digest") = f96b697d7cb7938d525a2f31aaf161d0
         MD5 ("abcdefghijklmnopqrstuvwxyz") = c3fcd3d76192e4007dfb496cca67e13b
         MD5 ("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789") = d174ab98d277d9f5a5611c2c9f419d9f
         MD5 ("12345678901234567890123456789012345678901234567890123456789012345678901234567890") = 57edf4a22be3c955ac49da2e2107b67a */

      String input = null;
      Encoding encoding = null;
      Assert.Throws<ArgumentNullException>(() => input.MD5Checksum(encoding));

      input = "";
      Assert.Throws<ArgumentNullException>(() => input.MD5Checksum(encoding));

      input = null;
      encoding = Encoding.ASCII;
      Assert.Throws<ArgumentNullException>(() => input.MD5Checksum(encoding));

      input = "";
      Assert.AreEqual("D41D8CD98F00B204E9800998ECF8427E", input.MD5Checksum(encoding));
      
      input = "a";
      Assert.AreEqual("0CC175B9C0F1B6A831C399E269772661", input.MD5Checksum(encoding));

      input = "abc";
      Assert.AreEqual("900150983CD24FB0D6963F7D28E17F72", input.MD5Checksum(encoding));
      
      input = "message digest";
      Assert.AreEqual("F96B697D7CB7938D525A2F31AAF161D0", input.MD5Checksum(encoding));

      input = "abcdefghijklmnopqrstuvwxyz";
      Assert.AreEqual("C3FCD3D76192E4007DFB496CCA67E13B", input.MD5Checksum(encoding));

      input = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
      Assert.AreEqual("D174AB98D277D9F5A5611C2C9F419D9F", input.MD5Checksum(encoding));

      input = "12345678901234567890123456789012345678901234567890123456789012345678901234567890";
      Assert.AreEqual("57EDF4A22BE3C955AC49DA2E2107B67A", input.MD5Checksum(encoding));
    }

    [Test]
    public void RemovePrefixTest()
    {
      String source = null;
      String stringToTrim = null;
      Assert.Throws<ArgumentNullException>(() => source.RemovePrefix(stringToTrim));

      source = "";
      Assert.Throws<ArgumentNullException>(() => source.RemovePrefix(stringToTrim));

      source = null;
      stringToTrim = "";
      Assert.Throws<ArgumentNullException>(() => source.RemovePrefix(stringToTrim));

      source = "";
      stringToTrim = "";
      Assert.AreEqual("", source.RemovePrefix(stringToTrim));

      source = "aa";
      stringToTrim = "a";
      Assert.AreEqual("a", source.RemovePrefix(stringToTrim));

      source = "aa";
      stringToTrim = "aa";
      Assert.AreEqual("", source.RemovePrefix(stringToTrim));

      source = "aa";
      stringToTrim = "aaa";
      Assert.AreEqual("aa", source.RemovePrefix(stringToTrim));
    }

    [Test]
    public void RemoveSuffixTest()
    {
      String source = null;
      String stringToTrim = null;
      Assert.Throws<ArgumentNullException>(() => source.RemoveSuffix(stringToTrim));

      source = "";
      Assert.Throws<ArgumentNullException>(() => source.RemoveSuffix(stringToTrim));

      source = null;
      stringToTrim = "";
      Assert.Throws<ArgumentNullException>(() => source.RemoveSuffix(stringToTrim));

      source = "";
      stringToTrim = "";
      Assert.AreEqual("", source.RemoveSuffix(stringToTrim));

      source = "aa";
      stringToTrim = "a";
      Assert.AreEqual("a", source.RemoveSuffix(stringToTrim));

      source = "aa";
      stringToTrim = "aa";
      Assert.AreEqual("", source.RemoveSuffix(stringToTrim));

      source = "aa";
      stringToTrim = "aaa";
      Assert.AreEqual("aa", source.RemoveSuffix(stringToTrim));
    }

    [Test]
    public void AddTrailingForwardSlashTest()
    {
      String s = null;
      Assert.Throws<ArgumentNullException>(() => s.AddTrailingForwardSlash());

      s = "";
      Assert.AreEqual("/", s.AddTrailingForwardSlash());

      s = "a";
      Assert.AreEqual("a/", s.AddTrailingForwardSlash());

      s = "a/";
      Assert.AreEqual("a/", s.AddTrailingForwardSlash());

      s = "a//";
      Assert.AreEqual("a//", s.AddTrailingForwardSlash());

      s = "a/ ";
      Assert.AreEqual("a/ /", s.AddTrailingForwardSlash());
    }

    [Test]
    public void RemoveHtmlTest()
    {
      String input = null;
      Assert.Throws<ArgumentNullException>(() => input.RemoveHtml());

      input = "";
      Assert.AreEqual("", input.RemoveHtml());

      input = @"
        < html >
          <head>
            <  title >Hello, world!</title>
          </head>
          <body>
          </ body >
        </html>
        ";
      Assert.AreEqual("Hello, world!", input.RemoveHtml().Trim());
    }

    [Test]
    public void RemoveWhitespaceTest()
    {
      String input = null;
      Assert.Throws<ArgumentNullException>(() => input.RemoveWhitespace());

      input = "";
      Assert.AreEqual("", input.RemoveWhitespace());

      /* A string of all possible whitespace characters in .Net. */
      input =
        String.Join("", 
          Enumerable
            .Range(0, Convert.ToInt32(Char.MaxValue))
            .Select(c => Convert.ToChar(c))
            .Where(c => Char.IsControl(c) || Char.IsSeparator(c) || Char.IsWhiteSpace(c)));

      Assert.AreEqual("", input.RemoveWhitespace());
    }

    [Test]
    public void AreAllEmptyTest()
    {
      List<String> strings = null;
      Assert.Throws<ArgumentNullException>(() => strings.AreAllEmpty());

      strings = new List<String>();
      Assert.IsTrue(strings.AreAllEmpty());

      strings.Add("a");
      Assert.IsFalse(strings.AreAllEmpty());

      strings.Clear();
      strings.Add(" ");
      Assert.IsTrue(strings.AreAllEmpty());

      strings.Add("a");
      Assert.IsFalse(strings.AreAllEmpty());

      strings.Add("b");
      Assert.IsFalse(strings.AreAllEmpty());
    }

    [Test]
    public void AreAnyEmptyTest()
    {
      List<String> strings = null;
      Assert.Throws<ArgumentNullException>(() => strings.AreAnyEmpty());

      strings = new List<String>();
      Assert.IsFalse(strings.AreAnyEmpty());

      strings.Add("a");
      Assert.IsFalse(strings.AreAnyEmpty());

      strings.Clear();
      strings.Add(" ");
      Assert.IsTrue(strings.AreAnyEmpty());

      strings.Add("a");
      Assert.IsTrue(strings.AreAnyEmpty());
    }
  }
}
