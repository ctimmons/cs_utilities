/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  [TestFixture]
  public class FileUtilsTests
  {
    private static readonly String _testFilesPath = FileUtils.GetTemporarySubfolder();
    private static readonly String _testStringsFile = _testFilesPath + "test_strings.txt";
    private static readonly String _testStringFoxAndDog = "The quick brown fox jumped over the lazy dog.";
    private static readonly String _testStringHelloWorld = "Hello, world!";
    private static readonly String _testStrings = (_testStringFoxAndDog + Environment.NewLine).Repeat(10).Trim();

    private static readonly String _level_1_1 = Path.Combine(_testFilesPath, @"level_1.1");
    private static readonly String _level_1_2 = Path.Combine(_testFilesPath, @"level_1.2");
    private static readonly String _level_2_2 = Path.Combine(_testFilesPath, @"level_1.1\level_2.2");
    private static readonly String _level_3_1 = Path.Combine(_testFilesPath, @"level_1.1\level_2.1\level_3.1");
    private static readonly String _level_3_2 = Path.Combine(_testFilesPath, @"level_1.1\level_2.1\level_3.2");

    public FileUtilsTests()
      : base()
    {
    }

    private String GetTestFilename()
    {
      return Path.Combine(_testFilesPath, Path.GetRandomFileName());
    }

    [SetUp]
    public void Init()
    {
      /* Setup an environment of folders and files that most of the unit tests use when they run. */

      Directory.CreateDirectory(_level_1_2);
      Directory.CreateDirectory(_level_2_2);
      Directory.CreateDirectory(_level_3_1);
      Directory.CreateDirectory(_level_3_2);

      File.WriteAllText(Path.Combine(_level_1_1, "fox_and_dog.txt"), _testStringFoxAndDog);
      File.WriteAllText(Path.Combine(_level_1_2, "hello_world.txt"), _testStringHelloWorld);
      File.WriteAllText(Path.Combine(_level_2_2, "hello_world.txt"), _testStringHelloWorld);
      File.WriteAllText(Path.Combine(_level_3_1, "fox_and_dog.txt"), _testStringFoxAndDog);
      File.WriteAllText(Path.Combine(_level_3_2, "hello_world.txt"), _testStringHelloWorld);

      File.WriteAllText(_testStringsFile, _testStrings);
    }

    [TearDown]
    public void Cleanup()
    {
      if (Directory.Exists(_testFilesPath))
        Directory.Delete(_testFilesPath, true /* Delete all files and subdirectories also. */);
    }

    [Test]
    public void DeleteDirectoryTest()
    {
      var rootDir = Path.Combine(_testFilesPath, "root/folder1/folder2");

      Directory.CreateDirectory(rootDir);

      var readwriteFilename = Path.Combine(rootDir, "dummy1.txt");
      File.WriteAllText(readwriteFilename, _testStringFoxAndDog);

      var readonlyFilename = Path.Combine(rootDir, "dummy2.txt");
      File.WriteAllText(readonlyFilename, _testStringFoxAndDog);
      File.SetAttributes(readonlyFilename, FileAttributes.ReadOnly);

      FileUtils.DeleteDirectory(rootDir);

      Assert.IsTrue(!Directory.Exists(rootDir));
    }

    private void DirectoryWalkerHarness(Action<FileSystemInfo> action, List<String> expected, List<String> actual)
    {
      var exceptions = FileUtils.DirectoryWalker(_testFilesPath, action, FileSystemTypes.All, DirectoryWalkerErrorHandling.Accumulate);

      if (exceptions.Any())
      {
        var messages = String.Join(Environment.NewLine, exceptions.Select(ex => ex.Message));
        throw new Exception(messages);
      }
      else
      {
        Assert.IsTrue(!expected.Except(actual).Any());
      }
    }

    [Test]
    public void DirectoryWalkerTest_DeleteFilesAndEmptyDirectories()
    {
      Action<FileSystemInfo> action =
        fsi =>
        {
          if (fsi is DirectoryInfo)
          {
            (fsi as DirectoryInfo).DeleteIfEmpty();
          }
          else if (fsi is FileInfo)
          {
            var fi = (fsi as FileInfo);
            if (fi.Name == "hello_world.txt")
              fi.Delete();
          }
        };

      var exceptions = FileUtils.DirectoryWalker(_testFilesPath, action, FileSystemTypes.All, DirectoryWalkerErrorHandling.Accumulate);

      if (exceptions.Any())
      {
        var messages = String.Join(Environment.NewLine, exceptions.Select(ex => ex.Message));
        throw new Exception(messages);
      }
      else
      {
        /* Contains fox_and_dog.txt, so this directory should still exist. */
        Assert.IsTrue(Directory.Exists(_level_3_1));

        /* All of these directories contained hello_world.txt, so they should have been deleted. */
        Assert.IsTrue(!Directory.Exists(_level_1_2));
        Assert.IsTrue(!Directory.Exists(_level_2_2));
        Assert.IsTrue(!Directory.Exists(_level_3_2));
      }
    }

    [Test]
    public void DirectoryWalkerTest_GetDirectoryNamesBasedOnFilenamesInDirectory()
    {
      var actual = new List<String>();

      Action<FileSystemInfo> action =
        fsi =>
        {
          if (fsi is DirectoryInfo)
          {
            var di = (fsi as DirectoryInfo);
            if (di.EnumerateFiles().Where(fi => fi.Name.Contains("dog")).Any())
              actual.Add(di.FullName);
          }
        };

      var expected = new List<String>()
      {
        _level_1_1,
        _level_3_1
      };

      this.DirectoryWalkerHarness(action, expected, actual);
    }

    [Test]
    public void DirectoryWalkerTest_GetDirectoryNamesBasedOnRegex()
    {
      var actual = new List<String>();

      Action<FileSystemInfo> action =
        fsi =>
        {
          if (fsi is DirectoryInfo)
          {
            var di = (fsi as DirectoryInfo);
            if (Regex.Match(di.FullName, "le..l_1", RegexOptions.Singleline).Success)
              actual.Add(di.FullName);
          }
        };

      var expected = new List<String>()
      {
        _level_1_1,
        _level_1_2
      };

      this.DirectoryWalkerHarness(action, expected, actual);
    }

    [Test]
    public void DirectoryWalkerTest_GetFilenamesBasedOnFileContents()
    {
      var actual = new List<String>();

      Action<FileSystemInfo> action =
        fsi =>
        {
          if (fsi is FileInfo)
          {
            var fi = (fsi as FileInfo);
            if (File.ReadAllText(fi.FullName).Contains("fox"))
              actual.Add(fi.FullName);
          }
        };

      var expected = new List<String>()
      {
        Path.Combine(_level_1_1, "fox_and_dog.txt"),
        Path.Combine(_level_3_1, "fox_and_dog.txt")
      };

      this.DirectoryWalkerHarness(action, expected, actual);
    }

    [Test]
    public void DirectoryWalkerTest_GetFilenamesBasedOnRegex()
    {
      var actual = new List<String>();

      Action<FileSystemInfo> action =
        fsi =>
        {
          if (fsi is FileInfo)
          {
            var fi = (fsi as FileInfo);
            if (Regex.Match(fi.FullName, "he..o", RegexOptions.Singleline).Success)
              actual.Add(fi.FullName);
          }
        };

      var expected = new List<String>()
      {
        Path.Combine(_level_3_2, "hello_world.txt"),
        Path.Combine(_level_2_2, "hello_world.txt"),
        Path.Combine(_level_1_2, "hello_world.txt")
      };

      this.DirectoryWalkerHarness(action, expected, actual);
    }

    [Test]
    public void SafelyCreateEmptyFileTest()
    {
      String filename = null;
      Assert.Throws<ArgumentNullException>(() => FileUtils.SafelyCreateEmptyFile(filename));

      filename = "";
      Assert.Throws<ArgumentException>(() => FileUtils.SafelyCreateEmptyFile(filename));

      filename = " ";
      Assert.Throws<ArgumentException>(() => FileUtils.SafelyCreateEmptyFile(filename));

      filename = Path.Combine(_testFilesPath, Path.GetRandomFileName());
      Assert.IsFalse(File.Exists(filename));
      FileUtils.SafelyCreateEmptyFile(filename);
      Assert.IsTrue(File.Exists(filename));
    }

    [Test]
    public void CreateEmptyFileTest()
    {
      String filename = null;
      Assert.Throws<ArgumentNullException>(() => FileUtils.CreateEmptyFile(filename, Overwrite.No));

      filename = "";
      Assert.Throws<ArgumentException>(() => FileUtils.CreateEmptyFile(filename, Overwrite.No));

      filename = " ";
      Assert.Throws<ArgumentException>(() => FileUtils.CreateEmptyFile(filename, Overwrite.No));

      filename = Path.Combine(_testFilesPath, Path.GetRandomFileName());
      Assert.IsFalse(File.Exists(filename));
      FileUtils.CreateEmptyFile(filename, Overwrite.No);
      Assert.IsTrue(File.Exists(filename));

      File.WriteAllText(filename, _testStringFoxAndDog);
      Assert.IsTrue((new FileInfo(filename)).Length > 0);
      FileUtils.CreateEmptyFile(filename, Overwrite.No);
      Assert.IsTrue((new FileInfo(filename)).Length > 0);
    }

    [Test]
    public void TouchTest()
    {
      var touchDate = new DateTime(1984, 1, 1);

      FileUtils.Touch(_testStringsFile, touchDate);

      Assert.AreEqual(touchDate, File.GetCreationTime(_testStringsFile));
      Assert.AreEqual(touchDate, File.GetLastAccessTime(_testStringsFile));
      Assert.AreEqual(touchDate, File.GetLastWriteTime(_testStringsFile));
    }

    /* The Lines extension method lives in IEnumberable.cs.
       The method is tested here because it's convenient to do so. */
    [Test]
    public void LinesTest()
    {
      using (var sr = new StreamReader(_testStringsFile, true))
        Assert.AreEqual(sr.Lines().Count(), 10);
    }

    [Test]
    public void WriteMemoryStreamToFileTest()
    {
      var filename = this.GetTestFilename();

      using (var ms = new MemoryStream())
      {
        ms.Write(Encoding.UTF8.GetBytes(_testStringFoxAndDog), 0, _testStringFoxAndDog.Length);
        FileUtils.WriteMemoryStreamToFile(filename, ms);

        var contents = File.ReadAllText(filename);
        Assert.AreEqual(_testStringFoxAndDog, contents);
      }
    }

    [Test]
    public void DeleteEmptyDirectoriesTest()
    {
      String rootDir = null;
      Assert.Throws<ArgumentNullException>(() => FileUtils.DeleteEmptyDirectories(rootDir));

      rootDir = "";
      Assert.Throws<ArgumentException>(() => FileUtils.DeleteEmptyDirectories(rootDir));

      rootDir = " ";
      Assert.Throws<ArgumentException>(() => FileUtils.DeleteEmptyDirectories(rootDir));

      rootDir = Path.Combine(_testFilesPath, "root");

      Directory.CreateDirectory(Path.Combine(rootDir, "empty/empty"));
      Directory.CreateDirectory(Path.Combine(rootDir, "empty/non empty"));
      Directory.CreateDirectory(Path.Combine(rootDir, "non empty/empty"));
      Directory.CreateDirectory(Path.Combine(rootDir, "non empty/non empty"));
      Directory.CreateDirectory(Path.Combine(rootDir, "really empty/empty"));
      Directory.CreateDirectory(Path.Combine(rootDir, "really empty/empty/empty"));

      File.WriteAllText(Path.Combine(rootDir, "empty/non empty/dummy.txt"), _testStringFoxAndDog);
      File.WriteAllText(Path.Combine(rootDir, "non empty/dummy.txt"), _testStringFoxAndDog);
      File.WriteAllText(Path.Combine(rootDir, "non empty/non empty/dummy.txt"), _testStringFoxAndDog);

      FileUtils.DeleteEmptyDirectories(rootDir);

      var areEmptyFoldersGone =
         Directory.Exists(Path.Combine(rootDir, "empty")) &&
        !Directory.Exists(Path.Combine(rootDir, "empty/empty")) &&
         Directory.Exists(Path.Combine(rootDir, "empty/non empty")) &&
        !Directory.Exists(Path.Combine(rootDir, "non empty/empty")) &&
         Directory.Exists(Path.Combine(rootDir, "non empty")) &&
         Directory.Exists(Path.Combine(rootDir, "non empty/non empty")) &&
        !Directory.Exists(Path.Combine(rootDir, "really empty/empty")) &&
        !Directory.Exists(Path.Combine(rootDir, "really empty/empty/empty"));

      Assert.IsTrue(areEmptyFoldersGone);
    }

    [Test]
    public void IsDirectoryEmptyTest()
    {
      String rootDir = null;
      Assert.Throws<ArgumentNullException>(() => FileUtils.IsDirectoryEmpty(rootDir));

      rootDir = "";
      Assert.Throws<ArgumentException>(() => FileUtils.IsDirectoryEmpty(rootDir));

      rootDir = " ";
      Assert.Throws<ArgumentException>(() => FileUtils.IsDirectoryEmpty(rootDir));

      rootDir = Path.Combine(_testFilesPath, "root");

      Directory.CreateDirectory(Path.Combine(rootDir, "empty/empty"));
      Directory.CreateDirectory(Path.Combine(rootDir, "empty/non empty"));

      File.WriteAllText(Path.Combine(rootDir, "empty/non empty/dummy.txt"), _testStringFoxAndDog);

      Assert.IsTrue(FileUtils.IsDirectoryEmpty(Path.Combine(rootDir, "empty/empty")));
      Assert.IsFalse(FileUtils.IsDirectoryEmpty(Path.Combine(rootDir, "empty/non empty")));
    }

    [Test]
    public void GetFormattedFileSizeTest()
    {
      Assert.AreEqual("532 bytes", FileUtils.GetFormattedFileSize(532));
      Assert.AreEqual("1.30 KB", FileUtils.GetFormattedFileSize(1340));
      Assert.AreEqual("22.9 KB", FileUtils.GetFormattedFileSize(23506));
      Assert.AreEqual("2.28 MB", FileUtils.GetFormattedFileSize(2400016));
      Assert.AreEqual("2.23 GB", FileUtils.GetFormattedFileSize(2400000000));
      Assert.AreEqual("8.19 GB", FileUtils.GetFormattedFileSize(8800000000));
      Assert.AreEqual("8.00 TB", FileUtils.GetFormattedFileSize(8800000000000));
    }

    /* GetMD5Checksum is tested indirectly via the MD5Checksum test in StringUtils.cs. */

    [Test]
    public void DuplicateSeparatorsTest()
    {
      String directory = null;
      Assert.Throws<ArgumentNullException>(() => directory.DuplicateSeparators());

      directory = "";
      Assert.AreEqual("", directory.DuplicateSeparators());

      directory = " ";
      Assert.AreEqual(" ", directory.DuplicateSeparators());

      directory = @"c:\\a\\\\b\\\\\\c\\\d";
      Assert.AreEqual(@"c:\\a\\b\\c\\d", directory.DuplicateSeparators());
    }

    [Test]
    public void AddTrailingSeparatorTest()
    {
      String directory = null;
      Assert.Throws<ArgumentNullException>(() => directory.AddTrailingSeparator());

      directory = @"c:\temp";
      Assert.AreEqual(directory + Path.DirectorySeparatorChar, directory.AddTrailingSeparator());

      directory = @"c:\temp\";
      Assert.AreEqual(directory, directory.AddTrailingSeparator());
    }

    [Test]
    public void RemoveTrailingSeparatorTest()
    {
      String directory = null;
      Assert.Throws<ArgumentNullException>(() => directory.RemoveTrailingSeparator());

      directory = @"c:\temp";
      Assert.AreEqual(directory, (directory + Path.DirectorySeparatorChar).RemoveTrailingSeparator());

      Assert.AreEqual(directory, directory.RemoveTrailingSeparator());
    }

    [Test]
    public void AreFilenamesEqualTest()
    {
      Assert.IsFalse(FileUtils.AreFilenamesEqual(@"c:\dir1\dir2\file1.txt", @"c:\dir1\dir2\dir3\file1.txt"));
      Assert.IsTrue(FileUtils.AreFilenamesEqual(@"c:\dir1\dir2\file1.txt", @"c:\dir1\dir2\dir3\..\file1.txt"));
    }

    [Test]
    public void CompareFilesTest()
    {
      var filename1 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
      var filename2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

      try
      {
        /* Neither file exists yet. */
        Assert.IsFalse(FileUtils.CompareFiles(filename1, filename2));

        File.WriteAllText(filename1, "");

        /* Only one of the files exists. */
        Assert.IsFalse(FileUtils.CompareFiles(filename1, filename2));

        File.WriteAllText(filename2, "Hello, world!");

        /* Both files exist, but have different lengths and different contents. */
        Assert.IsFalse(FileUtils.CompareFiles(filename1, filename2));

        File.WriteAllText(filename1, "abcdefghijklm");

        /* Both files exist, and have the same length, but have different contents. */
        Assert.IsFalse(FileUtils.CompareFiles(filename1, filename2));

        File.WriteAllText(filename1, "");
        File.WriteAllText(filename2, "");

        /* Both files exist, have the same length, and are both empty. */
        Assert.IsTrue(FileUtils.CompareFiles(filename1, filename2));

        File.WriteAllText(filename1, "Hello, world!");
        File.WriteAllText(filename2, "Hello, world!");

        /* Both files exist, have the same length, and have the same content. */
        Assert.IsTrue(FileUtils.CompareFiles(filename1, filename2));
      }
      finally
      {
        File.Delete(filename1);
        File.Delete(filename2);
      }
    }
  }
}
