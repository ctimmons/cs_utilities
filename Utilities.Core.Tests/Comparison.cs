/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  internal class Person
  {
    public String Name { get; set; }
    public Int32 Age { get; set; }
  }

  internal class PersonComparer : IComparer<Person>
  {
    #region IComparer<Person> Members
    public Int32 Compare(Person x, Person y)
    {
      return
        PartialComparer.ReferenceCompare(x, y) ??
        PartialComparer.Compare(x.Name, y.Name) ?? // Primary sort by ascending name.
        PartialComparer.Compare(y.Age, x.Age) ??   // Secondary sort by descending age.
        0;
    }
    #endregion
  }

  [TestFixture]
  public class Comparison
  {
    [Test]
    public void PartialComparerTest()
    {
      var people = new List<Person>()
      {
        new Person() { Name = "Foo", Age = 42 },
        new Person() { Name = "Bar", Age = 33 },
        new Person() { Name = "Bar", Age = 25 },
        new Person() { Name = "Quux", Age = 50 }
      };

      people.Sort(new PersonComparer());

      Assert.IsTrue(
        ((people[0].Name == "Bar") && (people[0].Age == 33)) &&
        ((people[1].Name == "Bar") && (people[1].Age == 25)) &&
        (people[2].Name == "Foo") &&
        (people[3].Name == "Quux"));
    }
  }
}
