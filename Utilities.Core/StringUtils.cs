/* See the LICENSE.txt file in the root folder for license details. */

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
    /// Convert value to a MemoryStream, using a default Unicode encoding.
    /// </summary>
    public static MemoryStream ToMemoryStream(this String value)
    {
      return value.ToMemoryStream(Encoding.Unicode);
    }

    /// <summary>
    /// Convert value to a MemoryStream, using the given encoding.
    /// </summary>
    public static MemoryStream ToMemoryStream(this String value, Encoding encoding)
    {
      return new MemoryStream(encoding.GetBytes(value ?? ""));
    }

    /// <summary>
    /// Extension of the System.Text.RegularExpressions.Regex.Escape() method.
    /// This method allows selected regex-related characters to remain unescaped.
    /// <para>If charsToUnescape is empty, this method has the same behavior as
    /// Regex.Escape().</para>
    /// </summary>
    public static String RegexEscape(this String s, params Char[] charsToUnescape)
    {
      s.Name("s").NotNullEmptyOrOnlyWhitespace();

      /* Start with a fuly escaped string. */
      var escapedRegex = Regex.Escape(s);

      if (charsToUnescape.Length == 0)
      {
        return escapedRegex;
      }
      else
      {
        var result = new StringBuilder(escapedRegex.Length);
        var isRemovingEscapeCharacter = false;

        /* "Unescape" the desired characters. */
        for (var i = escapedRegex.Length - 1; i >= 0; i--)
        {
          var c = escapedRegex[i];

          if (isRemovingEscapeCharacter)
          {
            if (c != '\\')
              result.Insert(0, c);

            isRemovingEscapeCharacter = false;
          }
          else if (charsToUnescape.Contains(c))
          {
            result.Insert(0, c);
            isRemovingEscapeCharacter = true;
          }
          else
          {
            result.Insert(0, c);
          }
        }

        return result.ToString();
      }
    }

    /// <summary>
    /// If filemask contains * or ? characters, convert it
    /// to an equivalent Regex.  If neither of those characters are in filemask,
    /// throw an error.
    /// </summary>
    public static Regex GetRegexFromFilemask(this String filemask)
    {
      filemask.Name("filemask").NotNullEmptyOrOnlyWhitespace();
      if (!filemask.ContainsAny("*?".ToCharArray()))
        throw new ArgumentException("'filemask' does not contain either the * or ? characters.");

      var filemaskRegexPattern =
        filemask
        /* Escape all regex-related characters except '*' and '?'. */
        .RegexEscape('*', '?')
        /* Convert '*' and '?' to their regex equivalents. */
        .Replace('?', '.')
        .Replace("*", ".*?");

      return new Regex("^" + filemaskRegexPattern + "$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }

    /// <summary>
    /// Returns the string s with all of the characters in cs removed.
    /// </summary>
    public static String Strip(this String s, Char[] cs)
    {
      s.Name("s").NotNull();

      return
        s
        .Where(c => !cs.Any(a => a == c))
        .Join();
    }

    /// <summary>
    /// Returns the first string parameter that is not null, has a length greater
    /// than zero, and does not consist only of whitespace.
    /// </summary>
    public static String Coalesce(params String[] strings)
    {
      /* Odd C# behavior.
      
         Calling this method with no parameters (i.e. Coalesce())
         causes the strings parameter to be an empty String[].
         
         Calling this method with a strongly typed null (i.e. Coalesce((String) null)
         causes the strings parameter to be a one element array with that element set to null.
         
         One assumes calling this method with an untyped null (i.e. Coalesce(null))
         would behave the same way, due to type inference. Instead, the strings
         parameter itself is set to null. */

      strings.Name("strings").NotNull(); // Throw ArgumentNullException for untyped null parameter.

      foreach (var s in strings)
        if (!s.IsEmpty())
          return s;

      throw new ArgumentException(Properties.Resources.StringUtils_Coalesce);
    }

    /// <summary>
    /// Returns the beginning portion of s up to, but not including,
    /// the first occurrence of the character c.  If c is not present in
    /// s, then s is returned.
    /// </summary>
    public static String UpTo(this String s, Char c)
    {
      var indexOfC = s.IndexOf(c);
      return
        (indexOfC == -1)
        ? s
        : s.Substring(0, indexOfC);
    }

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
      return value.SurroundWith(c, c);
    }

    /// <summary>
    /// Returns 'value' with 'c' pre-pended and appended.
    /// <para>
    /// Both 'value' and 'c' must non-null.
    /// </para>
    /// </summary>
    public static String SurroundWith(this String value, String c1, String c2)
    {
      value.Name("value").NotNull();
      c1.Name("c1").NotNull();
      c2.Name("c2").NotNull();
      return String.Concat(c1, value, c2);
    }

    public static String SingleQuote(this String value)
    {
      return value.SurroundWith("'");
    }

    public static String DoubleQuote(this String value)
    {
      return value.SurroundWith("\"");
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
      indent.Name("indent").GreaterThan(0);

      /* A string may consist of more than one line (i.e. lines separated by carriage returns).
          Return a string in which all lines are indented by the specified number of spaces. */

      var indentString = " ".Repeat(indent);
      return indentString + value.Replace(Environment.NewLine, Environment.NewLine + indentString);
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