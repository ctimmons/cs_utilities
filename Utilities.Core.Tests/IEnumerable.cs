/* See the LICENSE.txt file in the root folder for license details. */

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
    public void ContainsTest()
    {
      var values = new String[] { "one", "two", "three" };
      Assert.IsTrue(values.ContainsCI("one"));
      Assert.IsFalse(values.ContainsCI("tw"));
    }

    [Test]
    public void ContainsAnyTest()
    {
      String s = null;
      var cs = "".ToCharArray();
      Assert.Throws<ArgumentNullException>(() => s.ContainsAny(cs));

      s = "";
      Assert.Throws<ArgumentNullException>(() => s.ContainsAny(null));
      Assert.Throws<ArgumentException>(() => s.ContainsAny(cs));

      cs = "cd".ToCharArray();
      Assert.IsFalse("abe".ContainsAny(cs));
      Assert.IsTrue("abce".ContainsAny(cs));
      Assert.IsTrue("abcde".ContainsAny(cs));
    }

    [Test]
    public void ContainsAllTest()
    {
      String s = null;
      var cs = "".ToCharArray();
      Assert.Throws<ArgumentNullException>(() => s.ContainsAll(cs));

      s = "";
      Assert.Throws<ArgumentNullException>(() => s.ContainsAll(null));
      Assert.Throws<ArgumentException>(() => s.ContainsAll(cs));

      cs = "cd".ToCharArray();
      Assert.IsFalse("abe".ContainsAll(cs));
      Assert.IsFalse("abce".ContainsAll(cs));
      Assert.IsTrue("abcde".ContainsAll(cs));
      Assert.IsTrue("abcdecd".ContainsAll(cs));
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

    [Test]
    public void JoinAndTest()
    {
      var expected = "";
      var actual = (new[] { "" }).JoinAnd();
      Assert.AreEqual(expected, actual, "Empty, comma.");

      actual = (new[] { "" }).JoinAnd(false);
      Assert.AreEqual(expected, actual, "Empty, no comma.");

      expected = "a";
      actual = (new[] { "a" }).JoinAnd();
      Assert.AreEqual(expected, actual, "One element, comma.");

      expected = "a";
      actual = (new[] { "a" }).JoinAnd(false);
      Assert.AreEqual(expected, actual, "One element, no comma.");

      expected = "a and b";
      actual = (new[] { "a", "b" }).JoinAnd();
      Assert.AreEqual(expected, actual, "Two elements, comma.");

      expected = "a and b";
      actual = (new[] { "a", "b" }).JoinAnd(false);
      Assert.AreEqual(expected, actual, "Two elements, no comma.");

      expected = "a, b, and c";
      actual = (new[] { "a", "b", "c" }).JoinAnd();
      Assert.AreEqual(expected, actual, "Three elements, comma.");

      expected = "a, b and c";
      actual = (new[] { "a", "b", "c" }).JoinAnd(false);
      Assert.AreEqual(expected, actual, "Three elements, no comma.");
    }

    /* No point in writing a test for JoinOr().  It's the same code
       as JoinAnd(). */

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
