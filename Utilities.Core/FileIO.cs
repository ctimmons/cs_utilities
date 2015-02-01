/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.VisualBasic.FileIO;

namespace Utilities.Core
{
  public enum DirectoryWalkerErrorHandling { Accumulate, StopOnFirst }
  [Flags]
  public enum FileSystemTypes { Files = 1, Directories = 2, All = Files | Directories }
  public enum Overwrite { Yes, No }

  public static class FileUtils
  {
    public static readonly Char[] DirectorySeparators = new Char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

    public static List<Exception> DirectoryWalker(String rootDirectory, Action<FileSystemInfo> action)
    {
      return DirectoryWalker(rootDirectory, action, FileSystemTypes.All, DirectoryWalkerErrorHandling.Accumulate);
    }

    public static List<Exception> DirectoryWalker(String rootDirectory, Action<FileSystemInfo> action, FileSystemTypes fileSystemTypes)
    {
      return DirectoryWalker(rootDirectory, action, fileSystemTypes, DirectoryWalkerErrorHandling.Accumulate);
    }

    public static List<Exception> DirectoryWalker(String rootDirectory, Action<FileSystemInfo> action, DirectoryWalkerErrorHandling directoryWalkerErrorHandling)
    {
      return DirectoryWalker(rootDirectory, action, FileSystemTypes.All, directoryWalkerErrorHandling);
    }

    /* System.IO.DirectoryInfo.EnumerateFileSystemInfos is a great method for
       getting file and directory info.  Unfortunately, that
       method can't be used to easily modify those files and directories, much less do multiple
       modifying operations on one traversal of the tree.

       That's what DirectoryWalker is for.  It recurses down the tree
       doing a depth-first traversal until it reaches the "leaves" (i.e. directories
       that don't contain any subdirectories).  Then, as the recursion unwinds,
       the provided action lambda is given a FileSystemInfo representing either the
       current file or directory that's being enumerated.  The lambda can do anything
       it wants with the FileSystemInfo, including deleting the file or directory it represents.
       This is a safe operation since it happens on the way back "up" the tree, so deleting
       a directory won't have any affect on this method's algorithm.
    
       One neat trick this allows, and the reason I wrote this method in the first place,
       is that the lambda can delete files, and then delete any directories if they're empty.
       Both of those operations occur safely in one traversal of the directory hierarchy.
       (See the unit tests in Utilities.Core.Tests::FileIO.cs for an example of this and
       several other uses of DirectoryWalker).

       Since this method allows file operations, there's always the chance of an
       exception occurring.  Should the method stop on the first exception, or store it
       for later perusal and continue?  The answer I decided on is "both".

       The DirectoryWalkerErrorHandling parameter allows the caller to select what
       exception handling behavior DirectoryWalker should exhibit.  The exceptions are
       not thrown, so this method doesn't need to be wrapped in a try/catch handler.
       Any exceptions that do occur are stored in the return value of List<Exception>.
       
       When called with a setting of DirectoryWalkerErrorHandling.Accumulate, DirectoryWalker
       will process all files and directories, storing all exception objects in the return value.
    */

    public static List<Exception> DirectoryWalker(String rootDirectory, Action<FileSystemInfo> action, FileSystemTypes fileSystemTypes, DirectoryWalkerErrorHandling directoryWalkerErrorHandling)
    {
      var exceptions = new List<Exception>();

      Action<String> rec = null; /* C# oddity: lambda definition must initially be set to null to allow for recursion. */
      rec =
        (directory) =>
        {
          if (directoryWalkerErrorHandling.HasFlag(DirectoryWalkerErrorHandling.StopOnFirst) && exceptions.Any())
            return;

          try
          {
            var di = new DirectoryInfo(directory);
            foreach (var fsi in di.EnumerateFileSystemInfos())
            {
              if (fsi is DirectoryInfo)
                rec(fsi.FullName);

              if (directoryWalkerErrorHandling.HasFlag(DirectoryWalkerErrorHandling.StopOnFirst) && exceptions.Any())
                return;

              try
              {
                if ((fileSystemTypes.HasFlag(FileSystemTypes.Files) && (fsi is FileInfo)) ||
                   ((fileSystemTypes.HasFlag(FileSystemTypes.Directories) && (fsi is DirectoryInfo))))
                  action(fsi);
              }
              catch (Exception ex)
              {
                exceptions.Add(ex);
              }
            }
          }
          catch (Exception ex)
          {
            exceptions.Add(ex);
          }
        };

      rec(rootDirectory);

      return exceptions;
    }

    /* System.IO.DirectoryInfo.EnumerateFileSystemInfos has, in my opinion,
       a serious design flaw.  When enumerating directories, if one of those
       directories cannot be accessed, that method throws an exception and all
       enumeration stops.
    
       This method is meant to act as a replacement for System.IO.DirectoryInfo.EnumerateFileSystemInfos.
       When an exception occurs in this method, the error handler is called, and processing continues
       with the next file or directory.
    
       Like the DirectoryWalker method, this method does a depth-first tree
       traversal.  Unlike DirectoryWalker, this method is LINQ-friendly,
       and is meant to be used in read-only situations (like most LINQ expressions).
       DirectoryWalker is best used if modification is necessary while traversing
       the directory tree. */
    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(String path, String filemask, System.IO.SearchOption searchOption, Action<String, Exception> errorHandler)
    {
      /* Yield statements cannot appear inside of a try/catch statement.
         That's why the di and diEnumerator variables,
         and the associated if/then logic, are necessary. */

      DirectoryInfo di = null;
      try
      {
        di = new DirectoryInfo(path);
      }
      catch (Exception ex)
      {
        if (errorHandler != null)
          errorHandler(path, ex);
      }

      if (di == null)
      {
        yield break;
      }
      else
      {
        yield return di;

        IEnumerable<FileSystemInfo> diEnumerator = null;
        try
        {
          diEnumerator = di.EnumerateFileSystemInfos(filemask);
        }
        catch (Exception ex)
        {
          if (errorHandler != null)
            errorHandler(path, ex);
        }

        if (diEnumerator == null)
        {
          yield break;
        }
        else
        {
          foreach (var fsi in diEnumerator)
          {
            if ((fsi is DirectoryInfo) && (searchOption == System.IO.SearchOption.AllDirectories))
            {
              foreach (var fsi2 in EnumerateFileSystemInfos(fsi.FullName, filemask, searchOption, errorHandler))
                yield return fsi2;
            }
            else if (fsi is FileInfo)
            {
              yield return fsi;
            }
          }
        }
      }
    }

    public static void DeleteIfEmpty(this DirectoryInfo di)
    {
      if (di.IsDirectoryEmpty())
        di.Delete();
    }

    public static void SafelyCreateEmptyFile(String filename)
    {
      filename.Check("filename");
      CreateEmptyFile(filename, Overwrite.No);
    }

    public static void CreateEmptyFile(String filename, Overwrite overwrite)
    {
      filename.Check("filename");

      if ((overwrite == Overwrite.Yes) || !File.Exists(filename))
      {
        Directory.CreateDirectory(Path.GetDirectoryName(filename));

        using (var fs = File.Create(filename))
        {
          /* Create an empty file. */
        }
      }
    }

    public static void Touch(String filename, DateTime timestamp)
    {
      filename.Check("filename");

      File.SetCreationTime(filename, timestamp);
      File.SetLastAccessTime(filename, timestamp);
      File.SetLastWriteTime(filename, timestamp);
    }

    public static void WriteMemoryStreamToFile(String filename, MemoryStream ms)
    {
      filename.Check("filename");
      ms.CheckForNull("ms");

      using (var fs = File.Create(filename))
        ms.WriteTo(fs);
    }

    public static void DeleteEmptyDirectories(String path)
    {
      path.Check("path");
      DeleteEmptyDirectories(new DirectoryInfo(path));
    }
    
    public static void DeleteEmptyDirectories(DirectoryInfo directoryInfo)
    {
      directoryInfo.CheckForNull("directoryInfo");

      foreach (var subDirectory in directoryInfo.EnumerateDirectories())
      {
        DeleteEmptyDirectories(subDirectory);
        if (IsDirectoryEmpty(subDirectory))
          subDirectory.Delete(false /* Don't recurse. */);
      }
    }

    public static Boolean IsDirectoryEmpty(String path)
    {
      path.Check("path");
      return IsDirectoryEmpty(new DirectoryInfo(path));
    }

    public static Boolean IsDirectoryEmpty(this DirectoryInfo directoryInfo)
    {
      directoryInfo.CheckForNull("directoryInfo");
      return !directoryInfo.EnumerateFileSystemInfos().Any();
    }

    [DllImport("Shlwapi.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr StrFormatByteSize(Int64 fileSize, StringBuilder buffer, Int32 bufferSize);

    /* Converts a numeric value into a String that represents the number expressed 
       as a size value in bytes, kilobytes, megabytes, or gigabytes, depending on the size.
    
       This method does not necessarily have to be used only with file sizes.  It will 
       take any Int64 value and convert it into a string representation.
    
       The range of valid file size values is (2^32) - 1 to (2^64) - 1.
       Negative numbers are always given the unit designation of "bytes", and are
       not represented as megabytes (MB), etc.
    
       The underlying Win32 API routine this method calls calculates KB, MB, etc. using
       a divisor of 1024, so using a fileSize parameter of 1024 will generate a 
       return value of "1.00 KB". */
    public static String GetFormattedFileSize(Int64 fileSize)
    {
      /* Arbitrary max length of returned file size description. */
      const Int32 maxBufferSize = 30;

      var buffer = new StringBuilder(maxBufferSize);
      if (StrFormatByteSize(fileSize, buffer, maxBufferSize) != IntPtr.Zero)
        return buffer.ToString();
      else
        return "";
    }

    public static void MoveFileToRecycleBin(String filename)
    {
      FileSystem.DeleteFile(filename, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
    }

    private static MD5CryptoServiceProvider _md5 = new MD5CryptoServiceProvider();

    public static String GetMD5Checksum(String filename)
    {
      filename.Check("filename");
      using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
        return GetMD5Checksum(fs);
    }

    public static String GetMD5Checksum(Stream stream)
    {
      stream.CheckForNull("stream");

      return _md5.ComputeHash(stream).Select(c => c.ToString("X2")).Join("");
    }

    public static String GetExecutablePath()
    {
      return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location).AddTrailingSeparator();
    }

    private static readonly Regex _multipleBackslashes = new Regex(@"\\+", RegexOptions.Singleline);

    public static String DuplicateSeparators(this String directory)
    {
      directory.Check("directory", StringAssertion.NotNull);
      return _multipleBackslashes.Replace(directory, @"\\");
    }

    public static String GetTemporarySubfolder()
    {
      return Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), null)) + Path.DirectorySeparatorChar;
    }

    public static String AddTrailingSeparator(this String directory)
    {
      directory.Check("directory", StringAssertion.NotNull);
      return directory.RemoveTrailingSeparator() + Path.DirectorySeparatorChar.ToString();
    }

    public static String RemoveTrailingSeparator(this String directory)
    {
      directory.Check("directory", StringAssertion.NotNull);
      return directory.TrimEnd().TrimEnd(DirectorySeparators);
    }

    public static Boolean AreFilenamesEqual(String filename1, String filename2)
    {
      return Path.GetFullPath(filename1).EqualsCI(Path.GetFullPath(filename2));
    }

    public static Boolean CompareFiles(String filename1, String filename2)
    {
      if (!File.Exists(filename1) || !File.Exists(filename2))
        return false;

      if (AreFilenamesEqual(filename1, filename2))
        return true;

      using (FileStream fs1 = new FileStream(filename1, FileMode.Open),
                        fs2 = new FileStream(filename2, FileMode.Open))
      {
        if (fs1.Length != fs2.Length)
          return false;

        Int32 fb1, fb2;
        do
        {
          fb1 = fs1.ReadByte();
          fb2 = fs2.ReadByte();
        } while ((fb1 == fb2) && (fb1 != -1));

        return ((fb1 - fb2) == 0);
      }
    }
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct SYSTEMTIME
  {
    [MarshalAs(UnmanagedType.U2)]
    public UInt16 Year;
    [MarshalAs(UnmanagedType.U2)]
    public UInt16 Month;
    [MarshalAs(UnmanagedType.U2)]
    public UInt16 DayOfWeek;
    [MarshalAs(UnmanagedType.U2)]
    public UInt16 Day;
    [MarshalAs(UnmanagedType.U2)]
    public UInt16 Hour;
    [MarshalAs(UnmanagedType.U2)]
    public UInt16 Minute;
    [MarshalAs(UnmanagedType.U2)]
    public UInt16 Second;
    [MarshalAs(UnmanagedType.U2)]
    public UInt16 Milliseconds;

    public SYSTEMTIME(DateTime dt)
    {
      dt = dt.ToUniversalTime();  // SetSystemTime expects the SYSTEMTIME in UTC
      Year = (UInt16) dt.Year;
      Month = (UInt16) dt.Month;
      DayOfWeek = (UInt16) dt.DayOfWeek;
      Day = (UInt16) dt.Day;
      Hour = (UInt16) dt.Hour;
      Minute = (UInt16) dt.Minute;
      Second = (UInt16) dt.Second;
      Milliseconds = (UInt16) dt.Millisecond;
    }
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public struct WIN32_FIND_DATA
  {
    public FileAttributes dwFileAttributes;
    public ComTypes.FILETIME ftCreationTime;
    public ComTypes.FILETIME ftLastAccessTime;
    public ComTypes.FILETIME ftLastWriteTime;
    public Int32 nFileSizeHigh;
    public Int32 nFileSizeLow;
    public Int32 dwReserved0;
    public Int32 dwReserved1;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public String cFileName;
    // not using this
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
    public String cAlternate;
  }
}
