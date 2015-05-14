/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  [TestFixture]
  public class IEnumerableTests
  {
    [Test]
    public void ForEachTest()
    {
      const Int32 RANGE = 100;
      var expected = (RANGE + 1) * (RANGE / 2);
      var actual = 0;
      Enumerable.Range(1, RANGE).ForEach(i => actual += i);
      Assert.AreEqual(expected, actual);
    }

    [Test]
    public void ForEachITest()
    {
      const Int32 RANGE = 100;
      var expected = (RANGE + 1) * (RANGE / 2);
      var actual = 0;
      Enumerable.Range(1, RANGE + 1).ForEachI((_, i) => actual += i);
      Assert.AreEqual(expected, actual);
    }

    [Test]
    public void JoinTest()
    {
      var data = new List<String>() { "A", "B", "C", "D", "E" };
      var expected = "A, B, C, D, E";
      var actual = data.Join(", ");
      Assert.AreEqual(expected, actual);
    }

    [Test]
    public void JoinAndIndentTest()
    {
      var data = new List<String>() { "A", "B", "C", "D", "E" };
      var expected = @"   A
   B
   C
   D
   E";
      var actual = data.JoinAndIndent(3);
      Assert.AreEqual(expected, actual);
    }

    /* IEnumerableExtensions.Lines is tested in FileIO.cs. */

    [Test]
    public void IsNullOrEmptyTest()
    {
      var data = new List<String>() { "A", "B", "C", "D", "E" };
      var emptyData = new List<String>();

      Assert.IsFalse(data.IsNullOrEmpty());
      Assert.IsTrue(emptyData.IsNullOrEmpty());
      Assert.IsTrue(((IEnumerable<String>) null).IsNullOrEmpty());
    }

    [Test]
    public void TailTest()
    {
      var data = new List<Int32>() { 1, 2, 3 };
      var actual = new List<Int32> { 2, 3 };
      var expected = data.Tail();
      Assert.IsTrue(Enumerable.SequenceEqual(actual, expected));

      data = new List<Int32>() { 1 };
      actual = new List<Int32>();
      expected = data.Tail();
      Assert.IsTrue(Enumerable.SequenceEqual(actual, expected));

      data = new List<Int32>();
      actual = new List<Int32>();
      expected = data.Tail();
      Assert.IsTrue(Enumerable.SequenceEqual(actual, expected));

      data = (List<Int32>) null;
      actual = (List<Int32>) null;
      expected = data.Tail();
      Assert.AreEqual(expected, actual);
    }

    [Test]
    public void ProductTest()
    {
      var data = new List<Int32>() { 2, 3, 4, 5 };
      BigInteger expected = 120;

      Assert.AreEqual(expected, data.Product());
    }
  }
}
