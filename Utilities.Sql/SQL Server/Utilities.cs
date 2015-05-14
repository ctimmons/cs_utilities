/* See UNLICENSE.txt file for license details. */

using System;
using System.Linq;

using Utilities.Core;

namespace Utilities.Sql.SqlServer
{
  public class SqlServerUtilities
  {
    private readonly static Char[] _brackets = "[]".ToCharArray();

    /// <summary>
    /// Given a T-SQL identifier, return the same identifier with all of its
    /// constituent parts wrapped in square brackets.
    /// </summary>
    public static String GetNormalizedSqlIdentifier(String identifier)
    {
      identifier.Name("identifier").NotNullEmptyOrOnlyWhitespace();

      Func<String, String> wrap = s => s.Any() ? String.Concat("[", s.Trim(_brackets), "]") : "";

      return
        identifier
        /* Keep empty array elements, because parts of a multi-part T-SQL identifier
           can be empty (e.g. "server.database..object", where the schema name is omitted). */
        .Split(".".ToCharArray(), StringSplitOptions.None)
        .Select(element => wrap(element))
        .Join(".");
    }

    public static String GetStrippedSqlIdentifier(String identifier)
    {
      return identifier.Replace("[", "").Replace("]", "");
    }
  }
}
