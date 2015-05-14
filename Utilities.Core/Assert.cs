/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections;

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
        filename.Name("filename").NotNull().NotEmpty();
              
        // more code here...
      }

    That's what this class provides - extension methods on .Net's IEnumerable and IComparable
    interfaces that reduce many of those if/then statements to a simple chain of function calls.
  
    One thing to note is these string extension methods will work if the type is null.
  
      // The following lines of code are equivalent.
          
      String s = null;  s.Name("s").NotNull();
          
      ((String) null).Name("s").NotNull();


    See the unit tests in Utilities.Core.Tests/Assert.cs for usage examples.

  */

  public static class AssertUtils
  {
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

    public static AssertionContext<String> NotNullEmptyOrOnlyWhitespace(this String value)
    {
      return (new AssertionContext<String>(value)).NotNullEmptyOrOnlyWhitespace();
    }

    public static AssertionContext<String> NotNullEmptyOrOnlyWhitespace(this AssertionContext<String> value)
    {
      return value.NotNull().NotEmpty().NotOnlyWhitespace();
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
      throw new ArgumentException(String.Format(Properties.Resources.Assert_ContainerIsEmpty, value.Name));
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

    /* Old, obsolete code. */
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

    [Obsolete("Use new fluent assertion API instead.")]
    public static void Check(this String value, String name)
    {
      InternalCheckString(3, value, name, StringAssertion.All, 0, Int32.MaxValue);
    }

    [Obsolete("Use new fluent assertion API instead.")]
    public static void Check(this String value, String name, StringAssertion stringAssertion)
    {
      InternalCheckString(3, value, name, stringAssertion, 0, Int32.MaxValue);
    }

    [Obsolete("Use new fluent assertion API instead.")]
    public static void Check(this String value, String name, StringAssertion stringAssertion, Int32 length)
    {
      InternalCheckString(3, value, name, stringAssertion, length, length);
    }

    [Obsolete("Use new fluent assertion API instead.")]
    public static void Check(this String value, String name, StringAssertion stringAssertion, Int32 minimumLength, Int32 maximumLength)
    {
      InternalCheckString(3, value, name, stringAssertion, minimumLength, maximumLength);
    }

    [Obsolete("Use new fluent assertion API instead.")]
    public static void CheckForNull<T>(this T value, String name)
    {
      if (name == null)
        throw new ArgumentNullException("name");

      if (value == null)
        throw new ArgumentNullException(name);
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
