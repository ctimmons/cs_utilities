/* See UNLICENSE.txt file for license details. */

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.VisualBasic.FileIO;

namespace Utilities.Core
{
  public enum Overwrite { Yes, No }

  public static class FileUtils
  {
    public static readonly Char[] DirectorySeparators = new Char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

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

    public static Boolean IsDirectoryEmpty(DirectoryInfo directoryInfo)
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
        return String.Empty;
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

      Byte[] hash = _md5.ComputeHash(stream);

      /* Convert the byte array to a printable String. */
      var sb = new StringBuilder(32);
      foreach (Byte hex in hash)
        sb.Append(hex.ToString("X2"));

      return sb.ToString().ToUpper();
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
      return Path.ChangeExtension(Path.GetTempFileName(), null) + Path.DirectorySeparatorChar;
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
  }
}
