using System;
using System.Collections.Generic;

namespace Utilities.Core
{
  /* The PartialComparer class is taken from Jon Skeet's "C# in Depth".
     Neither the book nor the downloadable source code has any kind of
     license file. */

  public static class PartialComparer
  {
    public static Int32? Compare<T>(T first, T second)
    {
      return Compare(Comparer<T>.Default, first, second);
    }

    public static Int32? Compare<T>(IComparer<T> comparer, T first, T second)
    {
      Int32 ret = comparer.Compare(first, second);
      if (ret == 0)
        return null;
      else
        return ret;
    }

    public static Int32? ReferenceCompare<T>(T first, T second) where T : class
    {
      if (first == second)
        return 0;
      else if (first == null)
        return -1;
      else if (second == null)
        return 1;
      else
        return null;
    }
  }
}
