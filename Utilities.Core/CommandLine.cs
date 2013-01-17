/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Utilities.Core
{
  public class CommandLineParameterException : Exception
  {
    public CommandLineParameterException(String message)
      : base(message)
    {
    }

    public CommandLineParameterException(String message, Exception innerException)
      : base(message, innerException)
    {
    }
  }

  /*
  
    Command line items in Windows can be divided into four categories:

    == Switch ==

      Switch characters may be -, / or any combination of those two characters.

      A switch is a command line item that is preceded by a switch character,
      and does not have an associated value.

        Examples: --version, /?, -P

      Relevant method:

        DoesSwitchExist

    == Parameter ==

      A parameter is a switch that has a value associated with it.

      The switch and value may be adjacent (i.e. with no separating character between them),
      or they may be separated by a : or = character, or they may be separated by one or more
      whitespace characters.
  
      If the value has embedded whitespace, wrap it in double-quotes so it is treated as one entity by
      the Windows command line.  Otherwise, Windows will treat the whitespace characters as separators
      and split the value up into multiple command line items.

        Examples:

          -output"d:\data\My File With Spaces.txt"
          -output:"d:\data\My File With Spaces.txt"
          -output=d:\data\MyFileWithOutSpaces.txt
          -output d:\data\MyFileWithOutSpaces.txt

      Relevant methods:

        GetValueWithNullCheck
        GetValue
        GetDateTime
        GetDouble
        GetEnumValue<T>
        GetInt32

    == Value ==

      A value is any command line item that is not preceded by a switch character.
      These types of parameters are usually accessed by their position in the string
      array that holds the command line items.

      Examples:

        "d:\data\My File With Spaces.txt"
        d:\data\MyFileWithNoSpaces.txt

      Relevant properties:

        this (the default property indexer)
        Values

    == Response File ==

      Response files are plain text files that contain zero or more command line
      items, along with optional comment lines and blank lines.

      A response file's filename is specified on the command line by preceding it with
      the @ character.  When the CommandLine object is instantiated, its constructor will
      expand the contents of the response file inline into the string array holding the
      command line items.

      Within a response file, blank lines are ignored.  A comment line starts
      with the # character (any leading whitespace is ignored).  Comments may only
      appear on lines by themselves; a comment may not follow any command line
      items on the same line.  If a comment does appear on the same line
      as one or more command line items, no error will be thrown.  But the
      comment will be broken up into individual words and those words treated
      as command line items.

        Example using two response files:

          d:\data\ResponseFile1.rsp
          -------------------------

            # This is a response file.

            /foo
            -bar

          e:\my files\ResponseFile2.rsp
          -----------------------------

            # This is a different response file.

            --baz /quux

          Original command line
          ---------------------

            /switch1 @d:\data\ResponseFile1.rsp /switch2 @"e:\my files\ResponseFile2.rsp" /switch3

          Expanded command line
          ---------------------

            /switch1 /foo -bar /switch2 --baz /quux /switch3

  */

  public class CommandLine
  {
    public String this[Int32 index]
    {
      get { return this._args[index]; }
    }

    /* A command line may be a mix of switches, parameters, values and response file names.
       The Values property is a string array that references only the values.
       This makes it easy to access only the desired value at its position in the
       list of command line items, ignoring the other types of items.

       For example, a program's command line might be structured like this:

         program.exe [zero or more switches] inputFile outputFile
      
       The Values property allows access to just the inputFile or outputFile items,
       skipping any switches.  Values[0] holds inputFile, and Values[1] holds outputFile. */

    private String[] _values = null;
    public String[] Values
    {
      get
      {
        if (this._values == null)
          this._values = this._args.Where(arg => this.IsValue(arg)).ToArray();

        return this._values;
      }
    }

    private readonly Char[] _parameterValueSeparators = ":=".ToCharArray();
    private readonly Char[] _switchCharacters = "@-/".ToCharArray();
    private readonly String[] _args = null;

    public CommandLine()
      : base()
    {
      /* Environment.GetCommandLineArgs() includes the app's name as the first argument in the
         array it returns.  Use Skip(1) to avoid treating the app name as a parameter. */
      this._args = this.GetArgsWithExpandedResponseFileContents(Environment.GetCommandLineArgs().Skip(1).ToArray());
    }

    public CommandLine(String[] args)
      : base()
    {
      this._args = this.GetArgsWithExpandedResponseFileContents(args);
    }

    private Boolean IsValue(String arg)
    {
      return (arg.Length == arg.TrimStart(this._switchCharacters).Length);
    }

    private String[] GetArgsWithExpandedResponseFileContents(String[] args)
    {
      var result = new List<String>();

      foreach (var arg in args)
      {
        if (arg.StartsWith("@"))
        {
          var responseFilename = arg.TrimStart(this._switchCharacters);

          if (!File.Exists(responseFilename))
            throw new CommandLineParameterException(String.Format(Properties.Resources.CommandLine_ResponseFileDoesNotExist, responseFilename));

          result.AddRange(this.GetParametersFromResponseFile(responseFilename));
        }
        else
        {
          result.Add(arg);
        }
      }

      return result.ToArray();
    }

    private List<String> GetParametersFromResponseFile(String responseFilename)
    {
      try
      {
        using (var sr = new StreamReader(responseFilename))
        {
          var result = new List<String>();
          var lineNumber = 1;
          var line = sr.ReadLine();
          while (line != null)
          {
            line = line.Trim();
            
            if ((line.Length > 0) && !line.StartsWith("#"))
              result.AddRange(this.GetParametersFromLine(responseFilename, lineNumber, line));

            line = sr.ReadLine();
            lineNumber++;
          }

          return result;
        }
      }
      catch (Exception ex)
      {
        throw new CommandLineParameterException(String.Format(Properties.Resources.CommandLine_ResponseFileCouldNotBeProcessed, responseFilename), ex);
      }
    }

    private List<String> GetParametersFromLine(String responseFilename, Int32 lineNumber, String line)
    {
      Int32 numberOfParameters;
      var parametersIntPtr = CommandLineToArgvW(line, out numberOfParameters);
      if (parametersIntPtr == IntPtr.Zero)
      {
        var win32Error = Marshal.GetLastWin32Error();
        throw new ArgumentExceptionFmt(Properties.Resources.CommandLine_ResponseFileLineError,
          responseFilename, lineNumber, line, win32Error, GeneralUtils.GetSystemErrorMessage(win32Error));
      }

      try
      {
        var parameters = new List<String>(numberOfParameters);

        for (var i = 0; i < numberOfParameters; i++)
          parameters.Add(Marshal.PtrToStringUni(Marshal.ReadIntPtr(parametersIntPtr, i * IntPtr.Size)));

        return parameters;
      }
      finally
      {
        LocalFree(parametersIntPtr);
      }
    }

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] String lpCmdLine, out Int32 pNumArgs);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);

    public Boolean DoesSwitchExist(String parameterName)
    {
      return this.DoesSwitchExist(parameterName, StringComparison.OrdinalIgnoreCase);
    }

    public Boolean DoesSwitchExist(String parameterName, StringComparison stringComparison)
    {
      parameterName = parameterName.TrimStart(this._switchCharacters);

      return this._args.Any(arg => arg.TrimStart(this._switchCharacters).Equals(parameterName, stringComparison));
    }

    public String GetValue(String parameterName)
    {
      return this.GetValue(parameterName, StringComparison.OrdinalIgnoreCase);
    }

    public String GetValue(String parameterName, String defaultValue)
    {
      return this.GetValue(parameterName, defaultValue, StringComparison.OrdinalIgnoreCase);
    }

    public String GetValue(String parameterName, String defaultValue, StringComparison stringComparison)
    {
      try
      {
        return this.GetValue(parameterName, stringComparison) ?? defaultValue;
      }
      catch (CommandLineParameterException)
      {
        return defaultValue;
      }
    }

    public String GetValue(String parameterName, StringComparison stringComparison)
    {
      parameterName = parameterName.TrimStart(this._switchCharacters);

      for (var i = 0; i < this._args.Length; i++)
      {
        var arg = this._args[i].TrimStart(this._switchCharacters);

        if (arg.StartsWith(parameterName, stringComparison))
        {
          if (arg.Length > parameterName.Length)
          {
            /* Returns the value for the "-paramValue", "-param=Value" or "-param:Value" formats. */
            return arg.Substring(parameterName.Length).TrimStart(this._parameterValueSeparators);
          }
          else
          {
            /* "-param Value" format.  This requires a little more logic since it involves
               getting the next parameter in this._args, which may not exist.
               Or the next parameter might not be a value (i.e. it's preceded by a - or / character). */
            if ((i == (this._args.Length - 1)) || "-/".Contains(this._args[i + 1][0]))
              return default(String);
            else
              return this._args[i + 1];
          }
        }
      }

      return default(String);
    }

    public String GetValueWithNullCheck(String parameterName)
    {
      var value = this.GetValue(parameterName);
      if (value == default(String))
        throw new CommandLineParameterException(String.Format(Properties.Resources.CommandLine_ParameterNotFound, parameterName));
      else
        return value;
    }

    public DateTime GetDateTime(String parameterName)
    {
      return this.GetDateTime(parameterName, DateTimeFormatInfo.CurrentInfo);
    }

    public DateTime GetDateTime(String parameterName, DateTime defaultValue)
    {
      try
      {
        return this.GetDateTime(parameterName);
      }
      catch (CommandLineParameterException)
      {
        return defaultValue;
      }
    }

    public DateTime GetDateTime(String parameterName, DateTime defaultValue, DateTimeFormatInfo dateTimeFormatInfo)
    {
      try
      {
        return this.GetDateTime(parameterName, dateTimeFormatInfo);
      }
      catch (CommandLineParameterException)
      {
        return defaultValue;
      }
    }

    public DateTime GetDateTime(String parameterName, DateTimeFormatInfo dateTimeFormatInfo)
    {
      var parameterValue = this.GetValueWithNullCheck(parameterName);

      DateTime result;
      if (DateTime.TryParse(parameterValue, dateTimeFormatInfo, DateTimeStyles.None, out result))
        return result;
      else
        throw new CommandLineParameterException(String.Format(
          Properties.Resources.CommandLine_BadDateTime,
          parameterName, parameterValue, Thread.CurrentThread.CurrentCulture.Name));
    }

    public Double GetDouble(String parameterName)
    {
      return this.GetDouble(parameterName, NumberFormatInfo.CurrentInfo);
    }

    public Double GetDouble(String parameterName, Double defaultValue)
    {
      try
      {
        return this.GetDouble(parameterName);
      }
      catch (CommandLineParameterException)
      {
        return defaultValue;
      }
    }

    public Double GetDouble(String parameterName, Double defaultValue, NumberFormatInfo numberFormatInfo)
    {
      try
      {
        return this.GetDouble(parameterName, numberFormatInfo);
      }
      catch (CommandLineParameterException)
      {
        return defaultValue;
      }
    }

    public Double GetDouble(String parameterName, NumberFormatInfo numberFormatInfo)
    {
      var parameterValue = this.GetValueWithNullCheck(parameterName);

      Double result;
      if (Double.TryParse(parameterValue, NumberStyles.Float, numberFormatInfo, out result))
        return result;
      else
        throw new CommandLineParameterException(String.Format(
          Properties.Resources.CommandLine_BadDouble,
          parameterName, parameterValue, Thread.CurrentThread.CurrentCulture.Name));
    }

    public T GetEnumValue<T>(String parameterName, T defaultValue)
      where T : struct, IComparable, IFormattable, IConvertible
    {
      try
      {
        return this.GetEnumValue<T>(parameterName);
      }
      catch (CommandLineParameterException)
      {
        return defaultValue;
      }
    }

    public T GetEnumValue<T>(String parameterName)
      where T : struct, IComparable, IFormattable, IConvertible
    {
      var type = typeof(T);

      if (!type.IsEnum)
        throw new ArgumentException(String.Format(Properties.Resources.CommandLine_BadEnumType, type.ToString()));

      var parameterValue = this.GetValueWithNullCheck(parameterName);

      T result;
      if (Enum.TryParse(parameterValue, true /* Ignore case. */, out result))
        return result;
      else
        throw new CommandLineParameterException(String.Format(Properties.Resources.CommandLine_BadEnumValue, parameterName, type.FullName));
    }

    public Int32 GetInt32(String parameterName, Int32 defaultValue)
    {
      try
      {
        return this.GetInt32(parameterName);
      }
      catch (CommandLineParameterException)
      {
        return defaultValue;
      }
    }

    public Int32 GetInt32(String parameterName)
    {
      var parameterValue = this.GetValueWithNullCheck(parameterName);

      Int32 result;
      if (Int32.TryParse(parameterValue, out result))
        return result;
      else
        throw new CommandLineParameterException(String.Format(Properties.Resources.CommandLine_BadInt32, parameterName, parameterValue));
    }
  }
}
