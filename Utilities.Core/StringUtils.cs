/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Utilities.Core
{
  public static class StringUtils
  {
    public static Char As0Or1(this Boolean value)
    {
      return (value ? '1' : '0');
    }

    public static Char AsYOrN(this Boolean value)
    {
      return (value ? 'Y' : 'N');
    }

    public static Boolean AsBoolean(this String value)
    {
      return (!String.IsNullOrEmpty(value)) && (Properties.Resources.StringUtils_BooleanTruthLiterals.IndexOf(value.Trim(), StringComparison.InvariantCultureIgnoreCase) > -1);
    }

    /* Return 'value' repeated 'count' times.
    
       Value cannot be null.
       A count of zero or a negative count returns an empty string.
       A count of one returns value.
       A count of more than one returns value repeated count times. */

    public static String Repeat(this String value, Int32 count)
    {
      value.Check("value", StringAssertion.NotNull);
      return (count < 1) ? "" : (new StringBuilder(value.Length * count)).Insert(0, value, count).ToString();
    }

    public static Char LastChar(this String value)
    {
      value.Check("value", StringAssertion.NotNull | StringAssertion.NotZeroLength);
      return value[value.Length - 1];
    }

    public static String SurroundWith(this String value, String c)
    {
      value.Check("value", StringAssertion.NotNull);
      c.Check("c", StringAssertion.NotNull);
      return c + value + c;
    }

    public static String MD5Checksum(this String value)
    {
      value.Check("value", StringAssertion.NotNull);
      return MD5Checksum(value, Encoding.ASCII);
    }

    public static String MD5Checksum(this String value, Encoding encoding)
    {
      value.Check("value", StringAssertion.NotNull);
      encoding.CheckForNull("encoding");

      Byte[] bytes = encoding.GetBytes(value);
      using (var ms = new MemoryStream(bytes))
        return FileUtils.GetMD5Checksum(ms);
    }

    public static String RemovePrefix(this String value, String prefix)
    {
      value.Check("value", StringAssertion.NotNull);
      prefix.Check("prefix", StringAssertion.NotNull);

      if (value.StartsWith(prefix, StringComparison.CurrentCulture))
        return value.Substring(prefix.Length);
      else
        return value;
    }

    public static String RemoveSuffix(this String value, String suffix)
    {
      value.Check("value", StringAssertion.NotNull);
      suffix.Check("suffix", StringAssertion.NotNull);

      if ((suffix.Length <= value.Length) && (value.EndsWith(suffix, StringComparison.CurrentCulture)))
        return value.Substring(0, value.Length - suffix.Length);
      else
        return value;
    }

    public static String RemovePrefixAndSuffix(this String value, String stringToTrim)
    {
      return value.RemovePrefix(stringToTrim).RemoveSuffix(stringToTrim);
    }

    public static String AddTrailingForwardSlash(this String value)
    {
      value.Check("value", StringAssertion.NotNull);
      return value.EndsWith("/") ? value : value + "/";
    }

    private static readonly Regex _stripHtmlRegex = new Regex("<[^>]+?>", RegexOptions.Singleline);

    public static String RemoveHtml(this String value)
    {
      value.Check("value", StringAssertion.NotNull);
      return _stripHtmlRegex.Replace(value, String.Empty);
    }

    private static readonly Regex _whitespaceRegex = new Regex(@"[\p{Z}\p{C}]" /* All Unicode whitespace (Z) and control characters (C). */, RegexOptions.Singleline);

    public static String RemoveWhitespace(this String value)
    {
      value.Check("value", StringAssertion.NotNull);
      return _whitespaceRegex.Replace(value, String.Empty);
    }

    public static Boolean AreAllEmpty(this List<String> strings)
    {
      strings.CheckForNull("strings");
      return strings.All(s => String.IsNullOrWhiteSpace(s));
    }

    public static Boolean AreAnyEmpty(this List<String> strings)
    {
      strings.CheckForNull("strings");
      return strings.Any(s => String.IsNullOrWhiteSpace(s));
    }

    public static String Indent(this String value, Int32 indent)
    {
      value.Check("value", StringAssertion.NotNull);
      
      /* A string may consist of more than one line (i.e. lines separated by carriage returns).
          Return a string in which all lines are indented by the specified number of spaces. */

      if ((indent <= 0) || (value == ""))
      {
        return value;
      }
      else
      {
        var indentString = " ".Repeat(indent);
        return indentString + value.Replace(Environment.NewLine, Environment.NewLine + indentString);
      }
    }
  }
}