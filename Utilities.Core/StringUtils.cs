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
    /// <summary>
    /// Returns Char '1' for true, '0' for false.
    /// </summary>
    public static Char As0Or1(this Boolean value)
    {
      return (value ? '1' : '0');
    }

    /// <summary>
    /// Returns Char 'Y' for true, 'N' for false.
    /// </summary>
    public static Char AsYOrN(this Boolean value)
    {
      return (value ? 'Y' : 'N');
    }

    /// <summary>
    /// Returns true if value is in the set "1,Y,T,TRUE,YES" (case insensitive).
    /// </summary>
    public static Boolean AsBoolean(this String value)
    {
      return (!String.IsNullOrEmpty(value)) && Properties.Resources.StringUtils_BooleanTruthLiterals.ContainsCI(value.Trim());
    }

    /// <summary>
    /// Case insensitive version of System.String.IndexOf().
    /// </summary>
    public static Int32 IndexOfCI(this String source, String searchValue)
    {
      source.Name("source").NotNull();
      searchValue.Name("searchValue").NotNull();
      return source.IndexOf(searchValue, StringComparison.CurrentCultureIgnoreCase);
    }

    /// <summary>
    /// Case insensitive version of System.String.Contains().
    /// </summary>
    public static Boolean ContainsCI(this String source, String searchValue)
    {
      source.Name("source").NotNull();
      searchValue.Name("searchValue").NotNull();
      return (source.IndexOfCI(searchValue) > -1);
    }

    /// <summary>
    /// Return 'value' repeated 'count' times.
    /// <para>
    /// <ul>
    ///   <li>Value cannot be null</li>
    ///   <li>A count of zero or a negative count returns an empty string</li>
    ///   <li>A count of one returns value.</li>
    ///   <li>A count of more than one returns value repeated count times.</li>
    /// </ul>
    /// </para>
    /// </summary>
    public static String Repeat(this String value, Int32 count)
    {
      value.Name("value").NotNull();
      return (count < 1) ? "" : (new StringBuilder(value.Length * count)).Insert(0, value, count).ToString();
    }

    /// <summary>
    /// Return the last Char in 'value'.
    /// <para>
    /// 'value' must non-null and contain one or more characters.
    /// </para>
    /// </summary>
    public static Char LastChar(this String value)
    {
      value.Name("value").NotNull().NotEmpty();
      return value[value.Length - 1];
    }

    /// <summary>
    /// Return the last word - separated by a space character - in 'value'.
    /// <para>
    /// 'value' must non-null and contain one or more characters.
    /// </para>
    /// </summary>
    public static String LastWord(this String value)
    {
      value.Name("value").NotNull().NotEmpty();
      var lastIndexOfSpace = value.Trim().LastIndexOf(' ');
      return (lastIndexOfSpace == -1) ? value : value.Substring(lastIndexOfSpace + 1);
    }

    /// <summary>
    /// Returns 'value' with 'c' pre-pended and appended.
    /// <para>
    /// Both 'value' and 'c' must non-null.
    /// </para>
    /// </summary>
    public static String SurroundWith(this String value, String c)
    {
      value.Name("value").NotNull();
      c.Name("c").NotNull();
      return String.Concat(c, value, c);
    }

    /// <summary>
    /// Return the MD5 checksum for 'value', using an ASCII encoding for 'value'.
    /// <para>
    /// 'value' must non-null.
    /// </para>
    /// </summary>
    public static String MD5Checksum(this String value)
    {
      value.Name("value").NotNull();
      return MD5Checksum(value, Encoding.ASCII);
    }

    /// <summary>
    /// Return the MD5 checksum for 'value', using 'encoding' as an encoding for 'value'.
    /// <para>
    /// Both 'value' and 'encoding' must non-null.
    /// </para>
    /// </summary>
    public static String MD5Checksum(this String value, Encoding encoding)
    {
      value.Name("value").NotNull();
      encoding.Name("encoding").NotNull();

      Byte[] bytes = encoding.GetBytes(value);
      using (var ms = new MemoryStream(bytes))
        return FileUtils.GetMD5Checksum(ms);
    }

    /// <summary>
    /// Remove 'prefix' from the beginning of 'value' and return the result.
    /// <para>
    /// Both 'value' and 'prefix' must non-null.
    /// </para>
    /// </summary>
    public static String RemovePrefix(this String value, String prefix)
    {
      value.Name("value").NotNull();
      prefix.Name("prefix").NotNull();

      if (value.StartsWith(prefix, StringComparison.CurrentCulture))
        return value.Substring(prefix.Length);
      else
        return value;
    }

    /// <summary>
    /// Remove 'suffix' from the end of 'value' and return the result.
    /// <para>
    /// Both 'value' and 'suffix' must non-null.
    /// </para>
    /// </summary>
    public static String RemoveSuffix(this String value, String suffix)
    {
      value.Name("value").NotNull();
      suffix.Name("suffix").NotNull();

      if ((suffix.Length <= value.Length) && (value.EndsWith(suffix, StringComparison.CurrentCulture)))
        return value.Substring(0, value.Length - suffix.Length);
      else
        return value;
    }

    /// <summary>
    /// Remove 'stringToTrim' from the beginning and end of 'value' and return the result.
    /// <para>
    /// Both 'value' and 'stringToTrim' must non-null.
    /// </para>
    /// </summary>
    public static String RemovePrefixAndSuffix(this String value, String stringToTrim)
    {
      return value.RemovePrefix(stringToTrim).RemoveSuffix(stringToTrim);
    }

    /// <summary>
    /// If 'value' doesn't already end with a trailing slash, one is appended to 'value' and the combined string is returned.
    /// <para>
    /// 'value' must non-null.
    /// </para>
    /// </summary>
    public static String AddTrailingForwardSlash(this String value)
    {
      value.Name("value").NotNull();
      return value.EndsWith("/") ? value : value + "/";
    }

    private static readonly Regex _stripHtmlRegex = new Regex("<[^>]+?>", RegexOptions.Singleline);

    /// <summary>
    /// Remove anything that resembles an HTML or XML tag from 'value' and return the modified string.
    /// <para>
    /// 'value' must non-null.
    /// </para>
    /// </summary>
    public static String RemoveHtml(this String value)
    {
      value.Name("value").NotNull();
      return _stripHtmlRegex.Replace(value, "");
    }

    private static readonly Regex _whitespaceRegex = new Regex(@"[\p{Z}\p{C}]" /* All Unicode whitespace (Z) and control characters (C). */, RegexOptions.Singleline);

    /// <summary>
    /// Remove all Unicode whitespace from 'value' and return the modified string.
    /// <para>
    /// 'value' must non-null.
    /// </para>
    /// </summary>
    public static String RemoveWhitespace(this String value)
    {
      value.Name("value").NotNull();
      return _whitespaceRegex.Replace(value, "");
    }

    /// <summary>
    /// Returns true if 'value' is null, has a length of zero, or contains only whitespace.
    /// <para>
    /// 'value' must non-null.
    /// </para>
    /// </summary>
    public static Boolean IsEmpty(this String value)
    {
      value.Name("value").NotNull();
      return String.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Returns true if 'value' is not null, has a length greater than zero, and does not consist of only whitespace.
    /// <para>
    /// 'value' must non-null.
    /// </para>
    /// </summary>
    public static Boolean IsNotEmpty(this String value)
    {
      return !value.IsEmpty();
    }

    /// <summary>
    /// Returns true if all strings in 'values' are null, have a length of zero, or contain only whitespace.
    /// <para>
    /// 'values' must non-null.
    /// </para>
    /// </summary>
    public static Boolean AreAllEmpty(this List<String> values)
    {
      values.Name("values").NotNull();
      return values.All(s => s.IsEmpty());
    }

    /// <summary>
    /// Returns true if any of the strings in 'values' are null, have a length of zero, or contain only whitespace.
    /// <para>
    /// 'values' must non-null.
    /// </para>
    /// </summary>
    public static Boolean AreAnyEmpty(this List<String> values)
    {
      values.Name("values").NotNull();
      return values.Any(s => s.IsEmpty());
    }

    /// <summary>
    /// Treat 'value' as a multiline string, where each string is separated by System.Environment.NewLine.
    /// Indent each line in 'value' by 'indent' spaces and return the modified string.
    /// <para>
    /// 'value' must non-null.
    /// </para>
    /// </summary>
    public static String Indent(this String value, Int32 indent)
    {
      value.Name("value").NotNull();
      
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

    /// <summary>
    /// Case-insensitive version of System.String.Equals().
    /// </summary>
    public static Boolean EqualsCI(this String value, String other)
    {
      value.Name("value").NotNull();
      other.Name("other").NotNull();
      return value.Equals(other, StringComparison.CurrentCultureIgnoreCase);
    }

    /// <summary>
    /// Case-insensitive version of !System.String.Equals().
    /// </summary>
    public static Boolean NotEqualsCI(this String value, String other)
    {
      return !value.EqualsCI(other);
    }

    /// <summary>
    /// Case-insensitive version of System.String.StartsWith().
    /// </summary>
    public static Boolean StartsWithCI(this String value, String other)
    {
      value.Name("value").NotNull();
      other.Name("other").NotNull();
      return value.StartsWith(other, StringComparison.CurrentCultureIgnoreCase);
    }
  }

  public class CaseInsensitiveStringComparer : IEqualityComparer<String>
  {
    public static CaseInsensitiveStringComparer Instance = new CaseInsensitiveStringComparer();

    public Int32 GetHashCode(String s)
    {
      return s.GetHashCode();
    }

    public Boolean Equals(String s1, String s2)
    {
      return s1.EqualsCI(s2);
    }
  }
}