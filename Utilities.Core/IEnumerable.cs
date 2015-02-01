/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Utilities.Core
{
  public static class IEnumerableUtils
  {
    /* .Net 2.0 introduced a List<T>.ForEach method, but there is no comparable
        IEnumerable<T>.ForEach.  Lots of other types implement IEnumerable,
        so this method comes in handy in places besides List<T>. */
    public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
    {
      sequence.CheckForNull("sequence");
      action.CheckForNull("action");

      foreach (T item in sequence)
        action(item);
    }

    public static void ForEachI<T>(this IEnumerable<T> sequence, Action<T, Int32> action)
    {
      sequence.CheckForNull("sequence");
      action.CheckForNull("action");

      var i = 0;
      foreach (T item in sequence)
        action(item, i++);
    }

    public static String Join(this IEnumerable<String> values, String separator)
    {
      values.CheckForNull("values");
      separator.Check("separator", StringAssertion.NotNull);

      return String.Join(separator, values);
    }

    public static IEnumerable<String> Lines(this TextReader textReader)
    {
      textReader.CheckForNull("textreader");

      String line = null;
      while ((line = textReader.ReadLine()) != null)
        yield return line;
    }

    public static Boolean IsNullOrEmpty<T>(this IEnumerable<T> items)
    {
      return ((items == null) || !items.Any());
    }

    public static IEnumerable<T> Tail<T>(this IEnumerable<T> items)
    {
      return (items == null) ? null : items.Skip(1).Take(items.Count() - 1);
    }

    public static BigInteger Product(this IEnumerable<Int32> ints)
    {
      var result = new BigInteger(1);

      foreach (var i in ints)
        result *= i;

      return result;
    }
  }
}
