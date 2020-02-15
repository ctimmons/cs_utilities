/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  [TestFixture]
  public class StringUtilsTests
  {
    public StringUtilsTests() : base() { }

    [Test]
    public void ToMemoryStreamTest()
    {
      String s = null;
      using (var ms = s.ToMemoryStream())
        Assert.AreEqual(0, ms.Length);

      s = "";
      using (var ms = s.ToMemoryStream())
        Assert.AreEqual(0, ms.Length);

      s = "a";
      using (var ms = s.ToMemoryStream(Encoding.UTF8))
      {
        Assert.AreEqual(1, ms.Length);
        using (var sr = new StreamReader(ms))
          Assert.AreEqual(s, sr.ReadToEnd());
      }
    }

    [Test]
    public void CoalesceTest()
    {
      /* See comment in Coalesce() method as to why these two similar calls
         return different exceptions. */
      Assert.Throws<ArgumentNullException>(() => StringUtils.Coalesce(null));
      Assert.Throws<ArgumentException>(() => StringUtils.Coalesce((String) null));

      Assert.Throws<ArgumentException>(() => StringUtils.Coalesce());
      Assert.Throws<ArgumentException>(() => StringUtils.Coalesce(""));
      Assert.Throws<ArgumentException>(() => StringUtils.Coalesce("  "));
      Assert.Throws<ArgumentException>(() => StringUtils.Coalesce(null, null));
      Assert.Throws<ArgumentException>(() => StringUtils.Coalesce("", null));
      Assert.Throws<ArgumentException>(() => StringUtils.Coalesce("  ", null));
      Assert.Throws<ArgumentException>(() => StringUtils.Coalesce("  ", ""));
      Assert.Throws<ArgumentException>(() => StringUtils.Coalesce("", "  "));
      Assert.Throws<ArgumentException>(() => StringUtils.Coalesce("  ", "  "));
      Assert.Throws<ArgumentException>(() => StringUtils.Coalesce("", ""));
      Assert.Throws<ArgumentException>(() => StringUtils.Coalesce(null, ""));
      Assert.Throws<ArgumentException>(() => StringUtils.Coalesce(null, "  "));

      Assert.AreEqual("a", StringUtils.Coalesce("a"));
      Assert.AreEqual("a", StringUtils.Coalesce("a", null));
      Assert.AreEqual("a", StringUtils.Coalesce(null, "a"));
      Assert.AreEqual("a", StringUtils.Coalesce("a", ""));
      Assert.AreEqual("a", StringUtils.Coalesce("", "a"));
      Assert.AreEqual("a", StringUtils.Coalesce("a", "  "));
      Assert.AreEqual("a", StringUtils.Coalesce("  ", "a"));
    }

    [Test]
    public void RegexEscapeTest()
    {
      String s = null;
      Assert.Throws<ArgumentNullException>(() => s.RegexEscape());

      s = "";
      Assert.Throws<ArgumentException>(() => s.RegexEscape());

      s = " ";
      Assert.Throws<ArgumentException>(() => s.RegexEscape());

      s = "abc";
      Assert.AreEqual(Regex.Escape(s), s.RegexEscape());

      s = @"ab\c";
      Assert.AreEqual(Regex.Escape(s), s.RegexEscape());

      s = @"ab\c";
      Assert.AreEqual(s, s.RegexEscape('\\'));

      s = @"ab\.c";
      Assert.AreEqual(@"ab\\.c", s.RegexEscape('\\'));

      s = @"ab\.c";
      Assert.AreEqual(s, s.RegexEscape('\\', '.'));
    }

    [Test]
    public void GetRegexFromFilemaskTest()
    {
      String s = null;
      Assert.Throws<ArgumentNullException>(() => s.GetRegexFromFilemask());

      s = "";
      Assert.Throws<ArgumentException>(() => s.GetRegexFromFilemask());

      s = " ";
      Assert.Throws<ArgumentException>(() => s.GetRegexFromFilemask());

      s = "abc";
      Assert.Throws<ArgumentException>(() => s.GetRegexFromFilemask());

      s = "*";
      Assert.AreEqual("^.*?$", s.GetRegexFromFilemask().ToString());

      s = "?";
      Assert.AreEqual("^.$", s.GetRegexFromFilemask().ToString());

      s = "??";
      Assert.AreEqual("^..$", s.GetRegexFromFilemask().ToString());

      s = "a.b[cd]*";
      Assert.AreEqual(@"^a\.b\[cd].*?$", s.GetRegexFromFilemask().ToString());
    }

    [Test]
    public void UpToTest()
    {
      String s = null;
      Assert.Throws<ArgumentNullException>(() => s.UpTo('.'));

      s = "";
      Assert.AreEqual(s.UpTo('.'), "");

      s = ".";
      Assert.AreEqual(s.UpTo('.'), "");

      s = ".abc";
      Assert.AreEqual(s.UpTo('.'), "");

      s = "abc.def";
      Assert.AreEqual(s.UpTo('.'), "abc");

      s = "abcdef.";
      Assert.AreEqual(s.UpTo('.'), "abcdef");

      s = "abcdef.";
      Assert.AreEqual(s.UpTo('x'), "abcdef.");
    }

    [Test]
    public void As0Or1Test()
    {
      Assert.AreEqual(false.As0Or1(), '0');
      Assert.AreEqual(true.As0Or1(), '1');
    }

    [Test]
    public void AsYOrNTest()
    {
      Assert.AreEqual(false.AsYOrN(), 'N');
      Assert.AreEqual(true.AsYOrN(), 'Y');
    }

    [Test]
    public void AsBooleanTest()
    {
      /* The strings '1', 'true', 't', 'yes' and 'y' should all return true (case insensitive). 
         Anything else should return false. */

      foreach (var trueInput in new[] { "1", "true", "TRUE", "t", "T", "yes", "YES", "y", "Y" })
        Assert.AreEqual(trueInput.AsBoolean(), true);

      foreach (var falseInput in new[] { "", "0", "No", "False", "asfsadfdsf", null })
        Assert.AreEqual(falseInput.AsBoolean(), false);
    }

    [Test]
    public void IndexOfCITest()
    {
      var str = "abcdef";
      Assert.AreEqual(str.IndexOfCI("c"), 2);
      Assert.AreEqual(str.IndexOfCI("C"), 2);
      Assert.AreEqual(str.IndexOfCI("Z"), -1);
    }

    [Test]
    public void ContainsCITest()
    {
      var str = "abcdef";
      Assert.True(str.ContainsCI("c"));
      Assert.True(str.ContainsCI("C"));
      Assert.False(str.ContainsCI("Z"));
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
      Assert.AreEqual("", str.Repeat(count), "A non-empty string repeated zero times should be an empty string.");

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

    [Test]
    public void IndentTest()
    {
      String input = null;
      Assert.Throws<ArgumentNullException>(() => input.Indent(4));

      Assert.AreEqual("    ", "".Indent(4));

      input = @"one
two
three
";

      var output = @"    one
    two
    three
    ";

      Assert.Throws<ArgumentException>(() => input.Indent(0));

      Assert.AreEqual(input.Indent(4), output);
    }

    [Test]
    public void NLETest()
    {
      var actual = "one\rtwo\nthree\r\nfour".NLE();

      String expected;
      switch (Environment.OSVersion.Platform)
      {
        case PlatformID.MacOSX:
        case PlatformID.Unix:
          expected = "one\ntwo\nthree\nfour";
          break;
        case PlatformID.Win32NT:
        case PlatformID.Win32S:
        case PlatformID.Win32Windows:
        case PlatformID.WinCE:
        case PlatformID.Xbox:
          expected = "one\r\ntwo\r\nthree\r\nfour";
          break;
        default:
          throw new Exception("Unknown operating system.");
      }

      Assert.AreEqual(expected, actual);
    }

    [Test]
    public void ChompTest()
    {
      Assert.AreEqual("".Chomp(), "");
      Assert.AreEqual("x".Chomp(), "x");
      Assert.AreEqual("xx".Chomp(), "xx");

      Assert.AreEqual("\n".Chomp(), "");
      Assert.AreEqual("x\n".Chomp(), "x");
      Assert.AreEqual("xx\n".Chomp(), "xx");

      Assert.AreEqual("\r".Chomp(), "");
      Assert.AreEqual("x\r".Chomp(), "x");
      Assert.AreEqual("xx\r".Chomp(), "xx");

      Assert.AreEqual("\r\n".Chomp(), "");
      Assert.AreEqual("x\r\n".Chomp(), "x");
      Assert.AreEqual("xx\r\n".Chomp(), "xx");

      Assert.AreEqual("\n\n".Chomp(), "\n");
      Assert.AreEqual("x\n\n".Chomp(), "x\n");
      Assert.AreEqual("xx\n\n".Chomp(), "xx\n");

      Assert.AreEqual("\r\r".Chomp(), "\r");
      Assert.AreEqual("x\r\r".Chomp(), "x\r");
      Assert.AreEqual("xx\r\r".Chomp(), "xx\r");

      Assert.AreEqual("\r\n\r\n".Chomp(), "\r\n");
      Assert.AreEqual("x\r\n\r\n".Chomp(), "x\r\n");
      Assert.AreEqual("xx\r\n\r\n".Chomp(), "xx\r\n");
    }
  }
}
