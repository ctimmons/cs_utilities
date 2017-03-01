/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Utilities.Core
{
  public enum RunProcessType { IgnoreResult, ReturnResult }

  public static class GeneralUtils
  {
    public static String RunProcess(String command, String arguments, RunProcessType runProcessType)
    {
      var psi =
        new ProcessStartInfo()
        {
          FileName = command,
          Arguments = arguments,
          RedirectStandardOutput = (runProcessType == RunProcessType.ReturnResult),
          UseShellExecute = false
        };
      var process = Process.Start(psi);

      switch (runProcessType)
      {
        case RunProcessType.IgnoreResult:
          return null;
        case RunProcessType.ReturnResult:
          /* Avoid deadlocks by reading the entire standard output stream and
             then waiting for the process to exit.  See the "Remarks" section
             in the MSDN documentation:
               https://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k(System.Diagnostics.ProcessStartInfo.RedirectStandardOutput);k(TargetFrameworkMoniker-.NETFramework   
          */
          var output = process.StandardOutput.ReadToEnd();
          process.WaitForExit();
          return output;
        default:
          throw new ArgumentExceptionFmt(Properties.Resources.Utils_UnknownRunProcessType, runProcessType);
      }
    }

    /* See the Remarks section for Assembly.GetCallingAssembly() as to why
       MethodImplAttribute is needed.
       (https://msdn.microsoft.com/en-us/library/system.reflection.assembly.getcallingassembly.aspx)
       
       Tip: To get the correct name of the embedded resource, use ILSpy (http://www.ilspy.net/)
       to open the resource's assembly.  Look in the assembly's "Resources" folder. */
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static String GetEmbeddedTextResource(this String resourceName)
    {
      resourceName.Name("resourceName").NotNullEmptyOrOnlyWhitespace();

      using (var sr = new StreamReader(Assembly.GetCallingAssembly().GetManifestResourceStream(resourceName)))
        return sr.ReadToEnd();
    }

    public static Boolean IsJITOptimized(this Assembly assembly)
    {
      assembly.Name("assembly").NotNull();

      foreach (var attribute in assembly.GetCustomAttributes(typeof(DebuggableAttribute), false))
        if (attribute is DebuggableAttribute)
          return !(attribute as DebuggableAttribute).IsJITOptimizerDisabled;

      return true;
    }

    /* Recursively enumerate the objects params array, building a new XOR-ed hashcode of
       all of the object instances it contains. */
    public static Int32 GetHashCode(params Object[] objects)
    {
      Func<Int32, Object, Int32> getHashCodeRecursive = null; /* Initially set to null so the lambda can be recursive. */
      getHashCodeRecursive =
        (hashcode, obj) =>
        {
          if (obj is IEnumerable)
          {
            /* Recursive case. */
            foreach (var o in (obj as IEnumerable))
              hashcode = getHashCodeRecursive(hashcode, o);

            return hashcode;
          }
          else
          {
            /* Base case. */
            return hashcode ^ obj.GetHashCode();
          }
        };

      return getHashCodeRecursive(0, objects);
    }

    /* Given an error code returned by System.Runtime.InteropServices.Marshal.GetLastWin32Error,
       get the Windows API String representation of that error code.
       If no error message can be found for the given error code, the original error code and,
       if the call to FormatMessage fails, Marshal.GetLastWin32Error() are returned in a string. */
    public static String GetSystemErrorMessage(Int32 win32ErrorCode)
    {
      const Int32 formatMessageFromSystem = 0x00001000;
      const Int32 defaultLanguageID = 0;
      
      var buffer = new StringBuilder(256);
      var numberOfCharactersInBuffer = FormatMessage(formatMessageFromSystem, IntPtr.Zero, win32ErrorCode, defaultLanguageID, buffer, buffer.Capacity, IntPtr.Zero);

      if (numberOfCharactersInBuffer > 0)
      {
        return buffer.ToString().Trim();
      }
      else
      {
        var lastErrorCode = Marshal.GetLastWin32Error();
        if (lastErrorCode == 0)
          return String.Format(Properties.Resources.Utils_NoSystemErrorMessageFound, win32ErrorCode);
        else
          return String.Format(Properties.Resources.Utils_FormatMessageError, win32ErrorCode, lastErrorCode);
      }
    }

    [DllImport("Kernel32.dll", CharSet = CharSet.Auto, EntryPoint = "FormatMessage", SetLastError = true)]
    private static extern Int32 FormatMessage(Int32 flags, IntPtr source, Int32 messageId, Int32 languageId, StringBuilder buffer, Int32 size, IntPtr arguments);

    public static String GetMethodName(Int32 stackFrameLevel = 1)
    {
      var sf = new StackFrame(stackFrameLevel);
      var mb = sf.GetMethod();
      return String.Concat(mb.DeclaringType.FullName, ".", mb.Name);
    }

    public static String GetStackInfo()
    {
      /* The "2" parameter indicates the caller's stack frame and this method's stack frame are
         not included in the result. */
      return GetStackInfo(2);
    }

    /// <summary>
    /// Get a String containing stack information from zero or more levels up the call stack.
    /// </summary>
    /// <param name="levels">
    /// A <see cref="System.Int32"/> which indicates how many levels up the stack 
    /// the information should be retrieved from.  This value must be zero or greater.
    /// </param>
    /// <returns>
    /// A String in this format:
    /// <para>
    /// file name::namespace.[one or more type names].method name
    /// </para>
    /// </returns>
    public static String GetStackInfo(Int32 levels)
    {
      if (levels < 0)
        throw new ArgumentException("The levels parameter cannot be less than zero.", "levels");

      var sf = new StackFrame(levels, true /* Get the file name, line number, and column number of the stack frame. */);
      var mb = sf.GetMethod();

      return String.Format("{0}::{1}.{2} - Line {3}",
        Path.GetFileName(sf.GetFileName()),
        mb.DeclaringType.FullName,
        mb.Name,
        sf.GetFileLineNumber());
    }

    public static String GetErrorMessageWithStackInfo(String errorMessage)
    {
      return GetStackInfo() + Environment.NewLine.Repeat(2) + errorMessage;
    }

    private static String GetWmiPropertyValueAsString(String queryString, String propertyName)
    {
      var query = new SelectQuery(queryString);
      var searcher = new ManagementObjectSearcher(query);
      foreach (ManagementObject mo in searcher.Get())
        return mo.Properties[propertyName].Value.ToString();

      return null;
    }

    private static DateTime GetWmiPropertyValueAsDateTime(String queryString, String propertyName)
    {
      var value = GetWmiPropertyValueAsString(queryString, propertyName);
      return (value == null) ? DateTime.MinValue : ManagementDateTimeConverter.ToDateTime(value);
    }

    /// <summary>
    /// Use WMI to get the DateTime the current user logged on.
    /// <para>NOTE: Depending on Windows permissions settings, this may only work when the app is run as an administrator (i.e. the app has elevated privileges).</para>
    /// <para>Otherwise a ManagementException will be thrown.</para>
    /// </summary>
    /// <exception cref="System.Management.ManagementException">Thrown when the current user does not have sufficient privileges to read the WMI Win32_Session class.</exception>
    public static DateTime GetLastLoginDateTime()
    {
      var badLoginDateTime = DateTime.MinValue; // Not sure where I got this from -> new DateTime(1600, 12, 31, 18, 0, 0);
      var loginDateTime = GetWmiPropertyValueAsDateTime("SELECT * FROM Win32_Session", "StartTime");

      if (loginDateTime == badLoginDateTime)
        throw new ManagementException(Properties.Resources.Utils_BadLoginDateTime);
      else
        return loginDateTime;
    }

    public static DateTime GetBootDateTime()
    {
      return GetWmiPropertyValueAsDateTime("SELECT * FROM Win32_OperatingSystem WHERE Primary='true'", "LastBootUpTime");
    }
  }
}