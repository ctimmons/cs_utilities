/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Utilities.Core
{
  [Flags]
  public enum StringAssertion
  {
    None = 0x1,
    NotNull = 0x2,
    NotOnlyWhitespace = 0x4,
    NotZeroLength = 0x8,
    All = NotNull | NotOnlyWhitespace | NotZeroLength
  }

  /*

    A small collection of extension methods that reduces the code needed to
    check strings for null-ness, whitespace only and length.

    A common idiom in C# methods is using one or more if/then statements to check parameter(s) for validity.

      public String GetFileContents(String filename)
      {
        if (filename == null)
          throw new ArgumentNullException("filename cannot be null.");
              
        if (filename.Trim().Length == 0)
          throw new ArgumentException("filename cannot be empty.");
              
        // more code here...
      }

    Those if/then statements are ripe for abstraction.
    A more pleasant way to express the above logic might be something like this:

      public String GetFileContents(String filename)
      {
        filename.Check("filename", StringAssertion.NotNull | StringAssertion.NotZeroLength);
              
        // more code here...
      }

    Or even shorter, since checking a string for null-ness and length is so common:

      public String GetFileContents(String filename)
      {
        filename.Check("filename");
              
        // more code here...
      }

    That's what this class provides - extension methods on .Net's string type
    that reduce many of those if/then statements to simple function calls.
  
    One thing to note is these string extension methods will work if the string is null and the null has a string type.
  
      // The following three lines of code are equivalent.
          
      AssertUtils.Check(null, "s");
          
      ((String) null).Check("s");
          
      String s = null; s.Check("s");
          
      // The compiler will emit an error if an untyped null is used.
      null.Check("s");

    
  // Examples ///////////////////////////////////////////////////////////////

    String s = null;
        
    // The default is to apply StringAssertion.All.
    // An ArgumentNullException will be raised because s is null.
    s.Check("s");

    s = "";

    // An ArgumentException will be raised because s is empty.
    s.Check("s");

    s = "   ";

    // An ArgumentException will be raised because s consists only of whitespace.
    s.Check("s");

    // No exception will be raised because, even though
    // s consists only of whitespace, it has a length
    // greater than zero.
    s.Check("s", StringAssertion.NotNull | StringAssertion.NotZeroLength);

    s = "123";

    // An ArgumentException will be raised.
    // Because StringAssertion.None is specified,
    // s will not be checked for null-ness, zero length or if it consists only of whitespace.
    // Instead, s will be checked to see if its length is 5, which fails because
    // s is only 3 characters long.
    s.Check("s", StringAssertion.None, 5);

    // The last two Int32 parameters are the minimum and
    // maximum allowed length (inclusive) of s.
    // Because s is 3 characters long, no exception will be raised.
    s.Check("s", StringAssertion.None, 3, 5);

    // Other types can be checked for null-ness with the CheckForNull<T> method.
    StreamReader sr = null;
    sr.CheckForNull("sr");

  */

  public static class AssertUtils
  {
    private static void InternalCheckString(Int32 stackFrameLevel, String value, String name, StringAssertion stringAssertion, Int32 minimumLength, Int32 maximumLength)
    {
      name.CheckForNull("name");

      if (!stringAssertion.HasFlag(StringAssertion.None))
      {
        if (stringAssertion.HasFlag(StringAssertion.NotNull))
          value.CheckForNull("value");

        if (stringAssertion.HasFlag(StringAssertion.NotOnlyWhitespace))
          if (String.IsNullOrWhiteSpace(value))
            throw new ArgumentException(String.Format(Properties.Resources.Assert_StringNotWhitespace, name), name);

        /* value has to be non-null before its length can be checked. */
        value.CheckForNull("value");
        if (stringAssertion.HasFlag(StringAssertion.NotZeroLength))
          if (value.Length == 0)
            throw new ArgumentException(String.Format(Properties.Resources.Assert_StringNotZeroLength, name), name);
      }

      if (minimumLength > maximumLength)
        throw new ArgumentException(String.Format(Properties.Resources.Assert_StringInconsistentLengthParameters, minimumLength, maximumLength));

      /* All of the following checks require value to be non-null. */
      value.CheckForNull("value");

      if ((minimumLength == maximumLength) && (value.Length != minimumLength))
        throw new ArgumentException(String.Format(Properties.Resources.Assert_StringLengthsNotEqual, name, value.Length, minimumLength), name);

      if (value.Length < minimumLength)
        throw new ArgumentException(String.Format(Properties.Resources.Assert_StringLengthLessThanMinimum, name, value.Length, minimumLength), name);

      if (value.Length > maximumLength)
        throw new ArgumentException(String.Format(Properties.Resources.Assert_StringLengthGreaterThanMaximum, name, value.Length, maximumLength), name);
    }

    public static void Check(this String value, String name)
    {
      InternalCheckString(3, value, name, StringAssertion.All, 0, Int32.MaxValue);
    }

    public static void Check(this String value, String name, StringAssertion stringAssertion)
    {
      InternalCheckString(3, value, name, stringAssertion, 0, Int32.MaxValue);
    }

    public static void Check(this String value, String name, StringAssertion stringAssertion, Int32 length)
    {
      InternalCheckString(3, value, name, stringAssertion, length, length);
    }

    public static void Check(this String value, String name, StringAssertion stringAssertion, Int32 minimumLength, Int32 maximumLength)
    {
      InternalCheckString(3, value, name, stringAssertion, minimumLength, maximumLength);
    }

    public static void CheckForNull<T>(this T value, String name)
    {
      if (name == null)
        throw new ArgumentNullException("name");

      if (value == null)
        throw new ArgumentNullException(name);
    }

    //////////////////////////////////////////////////////////////
    // In the process of converting these methods to a "fluent" API.
    // Consider the above code obsolete.
    //////////////////////////////////////////////////////////////

    public static AssertionContext<T> Name<T>(this T value, String name)
    {
      return (new AssertionContext<T>(value)).Name(name);
    }

    public static AssertionContext<T> Name<T>(this AssertionContext<T> value, String name)
    {
      value.Name = name;
      return value;
    }

    public static AssertionContext<String> NotOnlyWhitespace(this String value)
    {
      return (new AssertionContext<String>(value)).NotOnlyWhitespace();
    }

    public static AssertionContext<String> NotOnlyWhitespace(this AssertionContext<String> value)
    {
      if (value.Value.Trim() != "")
        return value;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_StringIsAllWhitespace, value.Name));
    }

    public static AssertionContext<T> NotNull<T>(this T value)
      where T : class
    {
      return (new AssertionContext<T>(value)).NotNull();
    }

    public static AssertionContext<T> NotNull<T>(this AssertionContext<T> value)
      where T : class
    {
      if (value.Value != null)
        return value;
      else
        throw new ArgumentNullException(value.Name);
    }

    public static AssertionContext<T> NotEmpty<T>(this T values)
      where T : IEnumerable
    {
      return (new AssertionContext<T>(values)).NotEmpty();
    }

    public static AssertionContext<T> NotEmpty<T>(this AssertionContext<T> value)
      where T : IEnumerable
    {
      /* Some non-generic IEnumerator enumerators returned by IEnumerable.GetEnumerator()
         also implement IDisposable, while others do not.  Those enumerators
         that do implement IDisposable will need to have their Dispose() method called.
         
         A non-generic IEnumerator cannot be used in a "using" statement.
         So to make sure Dispose() is called (if it exists), "foreach" is used
         because it will generate code to dispose of the IEnumerator
         if the enumerator also implements IDisposable. */

      /* This loop will execute zero or more times. */
      foreach (var _ in value.Value)
        return value; /* Loop executed once.  There is at least one element in the IEnumerable, which means it's not empty. */

      /* Loop executed zero times, which means the IEnumerable is empty. */
      throw new ArgumentException(String.Format(Properties.Resources.Assert_ContainerIsNotEmpty, value.Name));
    }

    public static AssertionContext<T> GreaterThan<T>(this T value, T other)
      where T : IComparable<T>
    {
      return (new AssertionContext<T>(value)).GreaterThan(other);
    }

    public static AssertionContext<T> GreaterThan<T>(this AssertionContext<T> value, T other)
      where T : IComparable<T>
    {
      if (value.Value.CompareTo(other) > 0)
        return value;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_NotGreaterThan, value.Name, value.Value, other));
    }

    public static AssertionContext<T> GreaterThanOrEqualTo<T>(this T value, T other)
      where T : IComparable<T>
    {
      return (new AssertionContext<T>(value)).GreaterThanOrEqualTo(other);
    }

    public static AssertionContext<T> GreaterThanOrEqualTo<T>(this AssertionContext<T> value, T other)
      where T : IComparable<T>
    {
      if (value.Value.CompareTo(other) >= 0)
        return value;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_NotGreaterThanOrEqualTo, value.Name, value.Value, other));
    }

    public static AssertionContext<T> LessThan<T>(this T value, T other)
      where T : IComparable<T>
    {
      return (new AssertionContext<T>(value)).LessThan(other);
    }

    public static AssertionContext<T> LessThan<T>(this AssertionContext<T> value, T other)
      where T : IComparable<T>
    {
      if (value.Value.CompareTo(other) < 0)
        return value;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_NotLessThan, value.Name, value.Value, other));
    }

    public static AssertionContext<T> LessThanOrEqualTo<T>(this T value, T other)
      where T : IComparable<T>
    {
      return (new AssertionContext<T>(value)).LessThanOrEqualTo(other);
    }

    public static AssertionContext<T> LessThanOrEqualTo<T>(this AssertionContext<T> value, T other)
      where T : IComparable<T>
    {
      if (value.Value.CompareTo(other) <= 0)
        return value;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_NotLessThanOrEqualTo, value.Name, value.Value, other));
    }

    public static AssertionContext<T> EqualTo<T>(this T value, T other)
      where T : IComparable<T>
    {
      return (new AssertionContext<T>(value)).EqualTo(other);
    }

    public static AssertionContext<T> EqualTo<T>(this AssertionContext<T> value, T other)
      where T : IComparable<T>
    {
      if (value.Value.CompareTo(other) == 0)
        return value;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_NotEqualTo, value.Name, value.Value, other));
    }

    public static AssertionContext<T> NotEqualTo<T>(this T value, T other)
      where T : IComparable<T>
    {
      return (new AssertionContext<T>(value)).NotEqualTo(other);
    }

    public static AssertionContext<T> NotEqualTo<T>(this AssertionContext<T> value, T other)
      where T : IComparable<T>
    {
      if (value.Value.CompareTo(other) != 0)
        return value;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_EqualTo, value.Name, value.Value, other));
    }

    public static AssertionContext<T> BetweenInclusive<T>(this T value, T lowerBound, T upperBound)
      where T : IComparable<T>
    {
      return (new AssertionContext<T>(value)).BetweenInclusive(lowerBound, upperBound);
    }

    public static AssertionContext<T> BetweenInclusive<T>(this AssertionContext<T> value, T lowerBound, T upperBound)
      where T : IComparable<T>
    {
      if ((value.Value.CompareTo(lowerBound) >= 0) && (value.Value.CompareTo(upperBound) <= 0))
        return value;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_BetweenInclusive, value.Name, value.Value, lowerBound, upperBound));
    }

    public static AssertionContext<T> BetweenExclusive<T>(this T value, T lowerBound, T upperBound)
      where T : IComparable<T>
    {
      return (new AssertionContext<T>(value)).BetweenExclusive(lowerBound, upperBound);
    }

    public static AssertionContext<T> BetweenExclusive<T>(this AssertionContext<T> value, T lowerBound, T upperBound)
      where T : IComparable<T>
    {
      if ((value.Value.CompareTo(lowerBound) > 0) && (value.Value.CompareTo(upperBound) < 0))
        return value;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_BetweenExclusive, value.Name, value.Value, lowerBound, upperBound));
    }
  }

  public class AssertionContext<T>
  {
    public String Name { get; set; }
    public T Value { get; set; }

    private AssertionContext()
      : base()
    {
    }

    internal AssertionContext(T value)
      : this()
    {
      this.Name = "<Unknown variable name>";
      this.Value = value;
    }

    internal AssertionContext(String name, T value)
      : this()
    {
      this.Name = name;
      this.Value = value;
    }
  }
}
