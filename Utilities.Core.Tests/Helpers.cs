/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  public class Helpers
  {
    public static void AssertionIsTrue<I, O>(I input, O actualOutput, O expectedOutput)
    {
      Assert.IsTrue(EqualityComparer<O>.Default.Equals(actualOutput, expectedOutput),
        String.Format("Failed on input '{0}'. Expected '{1}', but result was '{2}'.",
          GetStringRepresentation(input), GetStringRepresentation(expectedOutput), GetStringRepresentation(actualOutput)));
    }

    public static void AssertionIsTrue<I1, I2, O>(I1 input1, I2 input2, O actualOutput, O expectedOutput)
    {
      Assert.IsTrue(EqualityComparer<O>.Default.Equals(actualOutput, expectedOutput),
        String.Format("Failed on inputs '{0}' and '{1}'. Expected '{2}', but result was '{3}'.",
          GetStringRepresentation(input1), GetStringRepresentation(input2), GetStringRepresentation(expectedOutput), GetStringRepresentation(actualOutput)));
    }

    private static String GetStringRepresentation(Object s)
    {
      return (s == null) ? "NULL" : GetDataFromContainer(s);
    }

    private static String GetDataFromContainer(Object o)
    {
      if (o is List<String>)
        return ((List<String>) o).Join(", ");
      else
        return o.ToString();
    }
  }
}
