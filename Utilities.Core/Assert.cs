/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.Collections;
using System.IO;

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
    check strings for null-ness, whitespace only, and length.

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

  public static partial class AssertUtils
  {
    public static AssertionContext<T> Name<T>(this T source, String name)
    {
      return (new AssertionContext<T>(source)).Name(name);
    }

    public static AssertionContext<T> Name<T>(this AssertionContext<T> source, String name)
    {
      source.Name = name;
      return source;
    }

    public static AssertionContext<String> NotOnlyWhitespace(this String source)
    {
      return (new AssertionContext<String>(source)).NotOnlyWhitespace();
    }

    public static AssertionContext<String> NotOnlyWhitespace(this AssertionContext<String> source)
    {
      if (source.Value.Trim() != "")
        return source;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_StringIsAllWhitespace, source.Name));
    }

    public static AssertionContext<String> NotNullEmptyOrOnlyWhitespace(this String source)
    {
      return (new AssertionContext<String>(source)).NotNullEmptyOrOnlyWhitespace();
    }

    public static AssertionContext<String> NotNullEmptyOrOnlyWhitespace(this AssertionContext<String> source)
    {
      return source.NotNull().NotEmpty().NotOnlyWhitespace();
    }

    public static AssertionContext<T> NotNull<T>(this T source)
      where T : class
    {
      return (new AssertionContext<T>(source)).NotNull();
    }

    public static AssertionContext<T> NotNull<T>(this AssertionContext<T> source)
      where T : class
    {
      if (source.Value != null)
        return source;
      else
        throw new ArgumentNullException(source.Name);
    }

    public static AssertionContext<T> NotEmpty<T>(this T source)
      where T : IEnumerable
    {
      return (new AssertionContext<T>(source)).NotEmpty();
    }

    public static AssertionContext<T> NotEmpty<T>(this AssertionContext<T> source)
      where T : IEnumerable
    {
      /* Some non-generic IEnumerator enumerators returned by IEnumerable.GetEnumerator()
         implement IDisposable, while others do not.  Those enumerators
         that do implement IDisposable will need to have their Dispose() method called.
         
         A non-generic IEnumerator cannot be used in a "using" statement.
         So to make sure Dispose() is called (if it exists), "foreach" is used
         because it will generate code to dispose of the IEnumerator
         if the enumerator also implements IDisposable. */

      /* This loop will execute zero or more times,
         and the foreach will dispose the enumerator, if necessary. */
      foreach (var _ in source.Value)
        return source; /* Loop executed once.  There is at least one element in the IEnumerable, which means it's not empty. */

      /* Loop executed zero times, which means the IEnumerable is empty. */
      throw new ArgumentException(String.Format(Properties.Resources.Assert_ContainerIsEmpty, source.Name));
    }

    public static AssertionContext<T> GreaterThan<T>(this T source, T value)
      where T : IComparable<T>
    {
      return (new AssertionContext<T>(source)).GreaterThan(value);
    }

    public static AssertionContext<T> GreaterThan<T>(this AssertionContext<T> source, T value)
      where T : IComparable<T>
    {
      if (source.Value.CompareTo(value) > 0)
        return source;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_NotGreaterThan, source.Name, source.Value, value));
    }

    public static AssertionContext<T> GreaterThanOrEqualTo<T>(this T source, T value)
      where T : IComparable<T>
    {
      return (new AssertionContext<T>(source)).GreaterThanOrEqualTo(value);
    }

    public static AssertionContext<T> GreaterThanOrEqualTo<T>(this AssertionContext<T> source, T value)
      where T : IComparable<T>
    {
      if (source.Value.CompareTo(value) >= 0)
        return source;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_NotGreaterThanOrEqualTo, source.Name, source.Value, value));
    }

    public static AssertionContext<T> LessThan<T>(this T source, T value)
      where T : IComparable<T>
    {
      return (new AssertionContext<T>(source)).LessThan(value);
    }

    public static AssertionContext<T> LessThan<T>(this AssertionContext<T> source, T value)
      where T : IComparable<T>
    {
      if (source.Value.CompareTo(value) < 0)
        return source;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_NotLessThan, source.Name, source.Value, value));
    }

    public static AssertionContext<T> LessThanOrEqualTo<T>(this T source, T value)
      where T : IComparable<T>
    {
      return (new AssertionContext<T>(source)).LessThanOrEqualTo(value);
    }

    public static AssertionContext<T> LessThanOrEqualTo<T>(this AssertionContext<T> source, T value)
      where T : IComparable<T>
    {
      if (source.Value.CompareTo(value) <= 0)
        return source;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_NotLessThanOrEqualTo, source.Name, source.Value, value));
    }

    public static AssertionContext<T> EqualTo<T>(this T source, T value)
      where T : IComparable<T>
    {
      return (new AssertionContext<T>(source)).EqualTo(value);
    }

    public static AssertionContext<T> EqualTo<T>(this AssertionContext<T> source, T value)
      where T : IComparable<T>
    {
      if (source.Value.CompareTo(value) == 0)
        return source;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_NotEqualTo, source.Name, source.Value, value));
    }

    public static AssertionContext<T> NotEqualTo<T>(this T source, T value)
      where T : IComparable<T>
    {
      return (new AssertionContext<T>(source)).NotEqualTo(value);
    }

    public static AssertionContext<T> NotEqualTo<T>(this AssertionContext<T> source, T value)
      where T : IComparable<T>
    {
      if (source.Value.CompareTo(value) != 0)
        return source;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_EqualTo, source.Name, source.Value, value));
    }

    public static AssertionContext<T> BetweenInclusive<T>(this T source, T lowerBound, T upperBound)
      where T : IComparable<T>
    {
      return (new AssertionContext<T>(source)).BetweenInclusive(lowerBound, upperBound);
    }

    public static AssertionContext<T> BetweenInclusive<T>(this AssertionContext<T> source, T lowerBound, T upperBound)
      where T : IComparable<T>
    {
      if ((source.Value.CompareTo(lowerBound) >= 0) && (source.Value.CompareTo(upperBound) <= 0))
        return source;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_BetweenInclusive, source.Name, source.Value, lowerBound, upperBound));
    }

    public static AssertionContext<T> BetweenExclusive<T>(this T source, T lowerBound, T upperBound)
      where T : IComparable<T>
    {
      return (new AssertionContext<T>(source)).BetweenExclusive(lowerBound, upperBound);
    }

    public static AssertionContext<T> BetweenExclusive<T>(this AssertionContext<T> source, T lowerBound, T upperBound)
      where T : IComparable<T>
    {
      if ((source.Value.CompareTo(lowerBound) > 0) && (source.Value.CompareTo(upperBound) < 0))
        return source;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_BetweenExclusive, source.Name, source.Value, lowerBound, upperBound));
    }

    public static AssertionContext<String> DirectoryExists(this String source)
    {
      return (new AssertionContext<String>(source)).DirectoryExists();
    }

    public static AssertionContext<String> DirectoryExists(this AssertionContext<String> source)
    {
      if (Directory.Exists(source.Value))
        return source;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_DirectoryExists, source.Name, source.Value));
    }

    public static AssertionContext<String> FileExists(this String source)
    {
      return (new AssertionContext<String>(source)).FileExists();
    }

    public static AssertionContext<String> FileExists(this AssertionContext<String> source)
    {
      if (File.Exists(source.Value))
        return source;
      else
        throw new ArgumentException(String.Format(Properties.Resources.Assert_FileExists, source.Name, source.Value));
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

    public AssertionContext(T value)
      : this()
    {
      this.Name = "<Unknown variable name>";
      this.Value = value;
    }

    public AssertionContext(String name, T value)
      : this()
    {
      this.Name = name;
      this.Value = value;
    }
  }
}
