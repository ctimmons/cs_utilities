/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
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

    public static String GetAllExceptionMessages(this Exception ex)
    {
      var result = new StringBuilder();
      var context = HttpContext.Current;
      var nl = Environment.NewLine;

      Action<Exception> rec = null; /* Initially set to null so the lambda can be called recursively. */
      rec =
        (currentException) =>
        {
          if (currentException == null)
            return;

          result.AppendLine(currentException.Message);

          /* When a file is missing in an ASP.Net app, the error message 'File does not exist' is less than helpful.
             Find the missing file name and add it to the error message. */
          if ((context != null) && currentException.Message.ContainsCI(Properties.Resources.Exceptions_File_Does_Not_Exist))
            result.AppendLineFormat(Properties.Resources.Exceptions_MissingFile, nl, context.Request.CurrentExecutionFilePath);

          foreach (DictionaryEntry de in currentException.Data)
            result.AppendLine(String.Concat("  ", de.Key, ": ", de.Value));

          if (currentException is SqlException)
          {
            foreach (var error in ((SqlException) currentException).Errors.Cast<SqlError>())
              result.AppendLineFormat("{0}.{1}.{2}({3}): {4} - {5} (state: {6})",
                error.Source, error.Server, error.Procedure, error.LineNumber, error.Number, error.Message, error.State);
          }

          /* StackTrace might be null when running this code in NUnit. */
          if (currentException.StackTrace != null)
            result.AppendLineFormat(Properties.Resources.Exceptions_StackTrace, nl, currentException.StackTrace.ToString());

          rec(currentException.InnerException);
        };

      rec(ex);
      return result.ToString();
    }
  }
}
