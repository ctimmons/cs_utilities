/* See UNLICENSE.txt file for license details. */

using System;
using System.Linq;

using Utilities.Core;

namespace Utilities.Sql
{
  public class SqlUtilities
  {
    /// <summary>
    /// Given a T-SQL identifier, return the same identifier with all of its
    /// constituent parts wrapped in square brackets.
    /// </summary>
    public static String GetNormalizedSqlIdentifier(String identifier)
    {
      identifier.Name("identifier").NotNull();

      Func<String, String> wrap = s => s.Any() ? String.Concat("[", s.Trim("[]".ToCharArray()), "]") : "";

      return
        identifier
        /* Keep empty array elements, because parts of a multi-part T-SQL identifier
           can be empty (e.g. "server.database..object", where the schema name is omitted). */
        .Split(".".ToCharArray(), StringSplitOptions.None)
        .Select(element => wrap(element))
        .Join(".");
    }
  }
}
