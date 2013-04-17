/* See UNLICENSE.txt file for license details. */

using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace Utilities.Core
{
  public static class GeneralUtils
  {
    /* Given an error code returned by System.Runtime.InteropServices.Marshal.GetLastWin32Error,
       get the Windows API String representation of that error code.
       If no error message can be found for the given error code, Marshal.GetLastWin32Error() is returned as a string. */
    public static String GetSystemErrorMessage(Int32 win32ErrorCode)
    {
      const Int32 formatMessageFromSystem = 0x00001000;
      const Int32 defaultLanguageID = 0;
      
      var buffer = new StringBuilder(256);
      var numberOfCharactersInBuffer = FormatMessage(formatMessageFromSystem, IntPtr.Zero, win32ErrorCode, defaultLanguageID, buffer, buffer.Capacity, IntPtr.Zero);

      if (numberOfCharactersInBuffer > 0)
        return buffer.ToString().Trim();
      else
        return "Unable to retrieve system error message for error " + Marshal.GetLastWin32Error().ToString();
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
      var badLoginDateTime = new DateTime(1600, 12, 31, 18, 0, 0);
      var loginDateTime = GetWmiPropertyValueAsDateTime("SELECT * FROM Win32_Session", "StartTime");

      if (loginDateTime == badLoginDateTime)
        throw new ManagementException("WMI Error.  The Win32_Session class does not return data unless the caller is running with sufficient permissions.  Alter the user's permissions or run the application as an adminstrator to avoid this error.");
      else
        return loginDateTime;
    }

    public static DateTime GetBootDateTime()
    {
      return GetWmiPropertyValueAsDateTime("SELECT * FROM Win32_OperatingSystem WHERE Primary='true'", "LastBootUpTime");
    }
  }
}