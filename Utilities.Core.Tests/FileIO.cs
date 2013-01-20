/* See UNLICENSE.txt file for license details. */

using System;
using System.IO;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  [TestFixture]
  public class FileUtilsTests
  {
    private static readonly String _testFilesPath = FileUtils.GetTemporarySubfolder();
    private static readonly String _testStringsFile = _testFilesPath + "test_strings.txt";
    private static readonly String _testString = "The quick brown fox jumped over the lazy dog.";
    private static readonly String _testStrings = (_testString + Environment.NewLine).Repeat(10).Trim();
    private static readonly String _directories = @"level_1.{0}\level_2.{0}\level_3.{0}";

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
      Directory.CreateDirectory(_testFilesPath);
      foreach (var i in Enumerable.Range(1, 3))
        Directory.CreateDirectory(Path.Combine(_testFilesPath, String.Format(_directories, i)));

      File.WriteAllText(_testStringsFile, _testStrings);
    }

    [TearDown]
    public void Cleanup()
    {
      if (Directory.Exists(_testFilesPath))
        Directory.Delete(_testFilesPath, true /* Delete all files and subdirectories also. */);
    }

    [Test]
    public void DirectoryWalkerTest_GetFilenamesBasedOnRegex()
    {
      /* create directory tree under _testfilespath several layers deep.  populate with files.
      
      read files from root based on filename
      read files from child folder one level below root based on filename
      read files from child folder more than one level below root based on filename
      ditto for directories

      saa, except examine file contents

      saa, except delete certain files and remove empty directories
      
      */
    }

    [Test]
    public void SafelyCreateEmptyFileTest()
    {
      String filename = null;
      Assert.Throws<ArgumentNullException>(() => FileUtils.SafelyCreateEmptyFile(filename));

      filename = String.Empty;
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

      filename = String.Empty;
      Assert.Throws<ArgumentException>(() => FileUtils.CreateEmptyFile(filename, Overwrite.No));

      filename = " ";
      Assert.Throws<ArgumentException>(() => FileUtils.CreateEmptyFile(filename, Overwrite.No));

      filename = Path.Combine(_testFilesPath, Path.GetRandomFileName());
      Assert.IsFalse(File.Exists(filename));
      FileUtils.CreateEmptyFile(filename, Overwrite.No);
      Assert.IsTrue(File.Exists(filename));

      File.WriteAllText(filename, _testString);
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
        ms.Write(Encoding.UTF8.GetBytes(_testString), 0, _testString.Length);
        FileUtils.WriteMemoryStreamToFile(filename, ms);

        var contents = File.ReadAllText(filename);
        Assert.AreEqual(_testString, contents);
      }
    }

    [Test]
    public void DeleteEmptyDirectoriesTest()
    {
      String rootDir = null;
      Assert.Throws<ArgumentNullException>(() => FileUtils.DeleteEmptyDirectories(rootDir));

      rootDir = String.Empty;
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

      File.WriteAllText(Path.Combine(rootDir, "empty/non empty/dummy.txt"), _testString);
      File.WriteAllText(Path.Combine(rootDir, "non empty/dummy.txt"), _testString);
      File.WriteAllText(Path.Combine(rootDir, "non empty/non empty/dummy.txt"), _testString);

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

      rootDir = String.Empty;
      Assert.Throws<ArgumentException>(() => FileUtils.IsDirectoryEmpty(rootDir));

      rootDir = " ";
      Assert.Throws<ArgumentException>(() => FileUtils.IsDirectoryEmpty(rootDir));

      rootDir = Path.Combine(_testFilesPath, "root");

      Directory.CreateDirectory(Path.Combine(rootDir, "empty/empty"));
      Directory.CreateDirectory(Path.Combine(rootDir, "empty/non empty"));

      File.WriteAllText(Path.Combine(rootDir, "empty/non empty/dummy.txt"), _testString);

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
  }
}
