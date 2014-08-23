/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections;
using System.Web;

namespace Utilities.Core
{
  public class ItemNotFoundException : Exception
  {
    public ItemNotFoundException(String message)
      : base(message)
    {
    }
  }

  /*

     Descendants of some common exception classes
     that make creating a formatted message a little easier.
 
     Old way:

       new Exception(String.Format("foo {0} baz", "bar"));

     New way:

       new ExceptionFmt("foo {0} baz", "bar");
  
  */

  public class ExceptionFmt : Exception
  {
    public ExceptionFmt(String message, params Object[] args)
      : base(String.Format(message, args))
    {
    }
  }

  public class ArgumentExceptionFmt : ArgumentException
  {
    public ArgumentExceptionFmt(String message, params Object[] args)
      : base(String.Format(message, args))
    {
    }
  }

  public class ArgumentNullExceptionFmt : ArgumentNullException
  {
    public ArgumentNullExceptionFmt(String message, params Object[] args)
      : base(String.Format(message, args))
    {
    }
  }

  public class ArgumentOutOfRangeExceptionFmt : ArgumentOutOfRangeException
  {
    public ArgumentOutOfRangeExceptionFmt(String message, params Object[] args)
      : base(String.Format(message, args))
    {
    }
  }

  public static class ExceptionUtils
  {
    /* Recursively gathers up all data about a given exception, including inner exceptions and
       whatever's stored in the Data property, and returns it all as a string.
    
       Useful for logging exception data. */
    public static String GetAllExceptionMessages(Exception ex)
    {
      if (ex == null)
      {
        return "";
      }
      else
      {
        var nl = Environment.NewLine;
        var context = HttpContext.Current;
        var result = ex.Message + nl;

        /* When a file is missing in an ASP.Net app, the error message 'File does not exist' is less than helpful.
           Find the missing file name and add it to the error message. */
        if ((context != null) && (result.IndexOf(Properties.Resources.Exceptions_File_Does_Not_Exist, StringComparison.CurrentCultureIgnoreCase) > -1))
          result += String.Format(Properties.Resources.Exceptions_MissingFile, nl, context.Request.CurrentExecutionFilePath);

        foreach (DictionaryEntry de in ex.Data)
          result += String.Concat("  ", de.Key, ": ", de.Value, nl);

        /* StackTrace might be null when running this code in NUnit. */
        if (ex.StackTrace != null)
          result += String.Format(Properties.Resources.Exceptions_StackTrace, nl, ex.StackTrace.ToString());

        return (String.Concat(result, nl, GetAllExceptionMessages(ex.InnerException))).Trim();
      }
    }
  }
}
