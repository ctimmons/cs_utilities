/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Utilities.Core
{
  public static class Win32Api
  {
    // Base message ID for all user-defined Windows messages.
    public const Int32 WmUser = 1024;
    public const Int32 WmNotify = 78;

    // Generic SendMessage declarations.  Normally, a class that needs
    // to use SendMessage will have its own declaration and use type-safe parameters.
    [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "SendMessage", SetLastError = false)]
    public static extern IntPtr SendMessage(IntPtr hWnd, Int32 msg, Int32 wParam, Int32 lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "SendMessage", SetLastError = false)]
    public static extern IntPtr SendMessage(IntPtr hWnd, Int32 msg, Int32 wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "GetClassName", SetLastError = true, CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern Boolean GetClassName(IntPtr hWnd, String lpClassName, Int32 nMaxCount);

    [DllImport("kernel32", EntryPoint = "GetPrivateProfileString", SetLastError = false, CharSet = CharSet.Auto)]
    private static extern Int64 GetPrivateProfileString(String lpApplicationName, String lpKeyName, String lpDefault, StringBuilder lpReturnedString, Int32 nSize, String lpFileName);

    /// <summary>
    /// Return a key's value in the specified INI file.  If the key does
    /// not exist, the default value is returned.
    /// </summary>
    /// <param name="iniFileName">The path and file name of the INI file.
    /// If no path is supplied (i.e. only a file name), then the Windows directory
    /// is searched for the file.</param>
    /// <param name="sectionName">The section the key/value entry is located in.</param>
    /// <param name="keyName">The name of the key to look for.</param>
    /// <param name="defaultValue">A default value to return if the key does not exist.</param>
    /// <returns>A <c>String</c> holding the keyName's value, or the defaultValue
    /// if keyName does not exist.</returns>
    /// <remarks>
    /// The iniFileName, sectionName, and keyName parameters are case-insensitive.
    /// <para>
    /// Also, this method relies on the ANSI version of the Windows API 
    /// function GetPrivateProfileStringA.  One strange (although documented)
    /// quirk of GetPrivateProfileStringA is this:  if a key's value is wrapped
    /// in single or Double quotes, those quotes are stripped off before the
    /// value is returned.  For example, if an INI file has two entries 
    /// that appear like this:
    /// </para>
    /// <para>
    /// <code>
    /// KeyNameQuoted = "A quoted value"
    /// KeyNameUnquoted = A quoted value
    /// </code>
    /// </para>
    /// <para>
    /// The value returned for either key will be the same, since the quotes
    /// will be stripped from KeyNameQuoted's value before being returned.
    /// </para>
    /// </remarks>
    public static String GetIniValue(String iniFileName, String sectionName, String keyName, String defaultValue)
    {
      const Int32 maxlen = 255;
      var buffer = new StringBuilder(maxlen);
      GetPrivateProfileString(sectionName, keyName, defaultValue, buffer, maxlen, iniFileName);
      return buffer.ToString();
    }

    // System Image List
    public const Int32 ShgfiIcon = 0x00000100;
    public const Int32 ShgfiLargeIcon = 0x00000000;
    public const Int32 ShgfiOpenIcon = 0x00000002;
    public const Int32 ShgfiSelected = 0x00010000;
    public const Int32 ShgfiShellIconSize = 0x00000004;
    public const Int32 ShgfiSmallIcon = 0x00000001;
    public const Int32 ShgfiSysIconIndex = 0x00004000;
    public const Int32 ShgfiTypeName = 0x00000400;
    public const Int32 ShgfiUseFileAttributes = 0x00000010;

    [StructLayout(LayoutKind.Sequential)]
    public struct ShFileInfo
    {
      public IntPtr hIcon;
      public IntPtr iIcon;
      public Int32 dwAttributes;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
      public String szDisplayName;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
      public String szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto, EntryPoint = "SHGetFileInfo", SetLastError = false)]
    public static extern IntPtr SHGetFileInfo(String pszPath, Int32 dwFileAttributes, ref ShFileInfo psfi, Int32 cbSizeFileInfo, Int32 uFlags);

    /// <summary>
    /// Passes the given command to the shell and executes it.
    /// </summary>
    /// <param name="command">
    /// String containing a valid shell command.
    /// </param>
    /// <exception cref="System.ArgumentException">
    /// Thrown if the command parameter is null or empty.
    /// </exception>
    public static void ShellExecute(String command)
    {
      command.Name("command").NotNullEmptyOrOnlyWhitespace();

      var browser = new Process();
      browser.StartInfo.FileName = command;
      browser.Start();
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "FindWindow", SetLastError = true)]
    public static extern IntPtr FindWindow(String lpClassName, String lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "ShowWindow", SetLastError = true)]
    public static extern Boolean ShowWindow(IntPtr hWnd, Int32 nCmdShow);

    [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "SetForegroundWindow", SetLastError = true)]
    public static extern Boolean SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "GetDesktopWindow", SetLastError = true)]
    public static extern Int32 GetDesktopWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "GetWindowDC", SetLastError = true)]
    public static extern Int32 GetWindowDC(Int32 hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "ReleaseDC", SetLastError = true)]
    public static extern Int32 ReleaseDC(Int32 hWnd, Int32 hDC);

    [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = true)]
    public static extern void RtlMoveMemory(IntPtr dest, IntPtr src, Int32 size);

    [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern Int32 memcmp(Byte[] b1, Byte[] b2, UIntPtr count);

    public const Int32 CSIDL_DESKTOP = 0x0000;                 // <desktop>
    public const Int32 CSIDL_INTERNET = 0x0001;                // Internet Explorer (icon on desktop)
    public const Int32 CSIDL_PROGRAMS = 0x0002;                // Start Menu\Programs
    public const Int32 CSIDL_CONTROLS = 0x0003;                // My Computer\Control Panel
    public const Int32 CSIDL_PRINTERS = 0x0004;                // My Computer\Printers
    public const Int32 CSIDL_PERSONAL = 0x0005;                // My Documents
    public const Int32 CSIDL_FAVORITES = 0x0006;               // <user name>\Favorites
    public const Int32 CSIDL_STARTUP = 0x0007;                 // Start Menu\Programs\Startup
    public const Int32 CSIDL_RECENT = 0x0008;                  // <user name>\Recent
    public const Int32 CSIDL_SENDTO = 0x0009;                  // <user name>\SendTo
    public const Int32 CSIDL_BITBUCKET = 0x000a;               // <desktop>\Recycle Bin
    public const Int32 CSIDL_STARTMENU = 0x000b;               // <user name>\Start Menu
    public const Int32 CSIDL_MYDOCUMENTS = CSIDL_PERSONAL;     // Personal was just a silly name for My Documents
    public const Int32 CSIDL_MYMUSIC = 0x000d;                 // "My Music" folder
    public const Int32 CSIDL_MYVIDEO = 0x000e;                 // "My Videos" folder
    public const Int32 CSIDL_DESKTOPDIRECTORY = 0x0010;        // <user name>\Desktop
    public const Int32 CSIDL_DRIVES = 0x0011;                  // My Computer
    public const Int32 CSIDL_NETWORK = 0x0012;                 // Network Neighborhood (My Network Places)
    public const Int32 CSIDL_NETHOOD = 0x0013;                 // <user name>\nethood
    public const Int32 CSIDL_FONTS = 0x0014;                   // windows\fonts
    public const Int32 CSIDL_TEMPLATES = 0x0015;
    public const Int32 CSIDL_COMMON_STARTMENU = 0x0016;        // All Users\Start Menu
    public const Int32 CSIDL_COMMON_PROGRAMS = 0X0017;         // All Users\Start Menu\Programs
    public const Int32 CSIDL_COMMON_STARTUP = 0x0018;          // All Users\Startup
    public const Int32 CSIDL_COMMON_DESKTOPDIRECTORY = 0x0019; // All Users\Desktop
    public const Int32 CSIDL_APPDATA = 0x001a;                 // <user name>\Application Data
    public const Int32 CSIDL_PRINTHOOD = 0x001b;               // <user name>\PrintHood
    public const Int32 CSIDL_LOCAL_APPDATA = 0x001c;           // <user name>\Local Settings\Applicaiton Data (non roaming)
    public const Int32 CSIDL_ALTSTARTUP = 0x001d;              // non localized startup
    public const Int32 CSIDL_COMMON_ALTSTARTUP = 0x001e;       // non localized common startup
    public const Int32 CSIDL_COMMON_FAVORITES = 0x001f;
    public const Int32 CSIDL_INTERNET_CACHE = 0x0020;
    public const Int32 CSIDL_COOKIES = 0x0021;
    public const Int32 CSIDL_HISTORY = 0x0022;
    public const Int32 CSIDL_COMMON_APPDATA = 0x0023;          // All Users\Application Data
    public const Int32 CSIDL_WINDOWS = 0x0024;                 // GetWindowsDirectory()
    public const Int32 CSIDL_SYSTEM = 0x0025;                  // GetSystemDirectory()
    public const Int32 CSIDL_PROGRAM_FILES = 0x0026;           // C:\Program Files
    public const Int32 CSIDL_MYPICTURES = 0x0027;              // C:\Program Files\My Pictures
    public const Int32 CSIDL_PROFILE = 0x0028;                 // USERPROFILE
    public const Int32 CSIDL_SYSTEMX86 = 0x0029;               // x86 system directory on RISC
    public const Int32 CSIDL_PROGRAM_FILESX86 = 0x002a;        // x86 C:\Program Files on RISC
    public const Int32 CSIDL_PROGRAM_FILES_COMMON = 0x002b;    // C:\Program Files\Common
    public const Int32 CSIDL_PROGRAM_FILES_COMMONX86 = 0x002c; // x86 Program Files\Common on RISC
    public const Int32 CSIDL_COMMON_TEMPLATES = 0x002d;        // All Users\Templates
    public const Int32 CSIDL_COMMON_DOCUMENTS = 0x002e;        // All Users\Documents
    public const Int32 CSIDL_COMMON_ADMINTOOLS = 0x002f;       // All Users\Start Menu\Programs\Administrative Tools
    public const Int32 CSIDL_ADMINTOOLS = 0x0030;              // <user name>\Start Menu\Programs\Administrative Tools
    public const Int32 CSIDL_CONNECTIONS = 0x0031;             // Network and Dial-up Connections
    public const Int32 CSIDL_COMMON_MUSIC = 0x0035;            // All Users\My Music
    public const Int32 CSIDL_COMMON_PICTURES = 0x0036;         // All Users\My Pictures
    public const Int32 CSIDL_COMMON_VIDEO = 0x0037;            // All Users\My Video
    public const Int32 CSIDL_RESOURCES = 0x0038;               // Resource Direcotry
    public const Int32 CSIDL_RESOURCES_LOCALIZED = 0x0039;     // Localized Resource Direcotry
    public const Int32 CSIDL_COMMON_OEM_LINKS = 0x003a;        // Links to All Users OEM specific apps
    public const Int32 CSIDL_CDBURN_AREA = 0x003b;             // USERPROFILE\Local Settings\Application Data\Microsoft\CD Burning
    // unused                               0x003c
    public const Int32 CSIDL_COMPUTERSNEARME = 0x003d;         // Computers Near Me (computered from Workgroup membership)

    public const Int32 CSIDL_NO_FLAG = 0;
    public const Int32 CSIDL_FLAG_CREATE = 0x8000;             // combine with CSIDL_ value to force folder creation in SHGetFolderPath()
    public const Int32 CSIDL_FLAG_DONT_VERIFY = 0x4000;        // combine with CSIDL_ value to return an unverified folder path
    public const Int32 CSIDL_FLAG_DONT_UNEXPAND = 0x2000;      // combine with CSIDL_ value to avoid unexpanding environment variables
    public const Int32 CSIDL_FLAG_NO_ALIAS = 0x1000;           // combine with CSIDL_ value to insure non-alias versions of the pidl
    public const Int32 CSIDL_FLAG_PER_USER_INIT = 0x0800;      // combine with CSIDL_ value to indicate per-user init (eg. upgrade)
    public const Int32 CSIDL_FLAG_MASK = 0xFF00;               // mask for all possible flag values

    [DllImport("shell32.dll")]
    private static extern Int32 SHGetFolderPath(IntPtr hwndOwner, Int32 csidl, IntPtr hToken, UInt32 dwFlags, [Out] StringBuilder pszPath);

    public static String GetShellFolder(Int32 csidl)
    {
      var sb = new StringBuilder();
      SHGetFolderPath(IntPtr.Zero, csidl, IntPtr.Zero, CSIDL_NO_FLAG, sb);
      return sb.ToString();
    }

    /* Example:
    
    public static String GetFontFolderPath()
    {
      StringBuilder sb = new StringBuilder();
      SHGetFolderPath(IntPtr.Zero, CSIDL_FONTS, IntPtr.Zero, CSIDL_NO_FLAG, sb);
      return sb.ToString();
    }
   
    */
  }
}