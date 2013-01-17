/* See UNLICENSE.txt file for license details. */

using System;
using System.Globalization;
using System.IO;

using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  [TestFixture]
  public class CommandLineTests
  {
    private static String _testFilesPath = FileUtils.GetTemporarySubfolder();
    private static String _emptyResponseFilePath = Path.Combine(_testFilesPath, "emptyresponsefile.rsp");
    private static String _goodResponseFile1Path = Path.Combine(_testFilesPath, "goodresponsefile1.rsp");
    private static String _goodResponseFile2Path = Path.Combine(_testFilesPath, "goodresponsefile2.rsp");

    [SetUp]
    public void Init()
    {
      /* Setup an environment of folders and files that most of the unit tests use when they run. */
      Directory.CreateDirectory(_testFilesPath);

      File.WriteAllText(_emptyResponseFilePath, @"
# only comments and blank lines

# another comment line
# ....

");

      File.WriteAllText(_goodResponseFile1Path, @"
# comment

/foo
-bar

");

      File.WriteAllText(_goodResponseFile2Path, @"
# comment

/baz:value --quux

");
    }

    [TearDown]
    public void Cleanup()
    {
      if (Directory.Exists(_testFilesPath))
        Directory.Delete(_testFilesPath, true /* Delete all files and subdirectories also. */);
    }

    [Test]
    public void EmptyResponseFileTest()
    {
      var cl = new CommandLine(new[] { "/param1:first", "@" + _emptyResponseFilePath, "--param3third" });
      Assert.IsTrue(cl[0] == "/param1:first");
      Assert.IsTrue(cl[1] == "--param3third");
    }

    [Test]
    public void GoodResponseFileTest()
    {
      var cl = new CommandLine(new[] { "/param1:first", "@" + _goodResponseFile1Path, "--param3third", "@" + _goodResponseFile2Path, "fourth" });
      Assert.IsTrue(cl[0] == "/param1:first");
      Assert.IsTrue(cl[1] == "/foo");
      Assert.IsTrue(cl[2] == "-bar");
      Assert.IsTrue(cl[3] == "--param3third");
      Assert.IsTrue(cl[4] == "/baz:value");
      Assert.IsTrue(cl[5] == "--quux");
      Assert.IsTrue(cl[6] == "fourth");
    }
    
    [Test]
    public void DoesSwitchExistTest()
    {
      var cl = new CommandLine(new[] { "/foo" });
      
      Assert.IsTrue(cl.DoesSwitchExist("foo"));
      Assert.IsFalse(cl.DoesSwitchExist("FOO", StringComparison.Ordinal));
      Assert.IsFalse(cl.DoesSwitchExist("bar"));
    }

    [Test]
    public void GetValueTest()
    {
      var cl = new CommandLine(new[] { "/foo", "/param1:value1", "-param2=value2", "--param3value3", "/param4", "value4", "-param5" });

      Assert.AreEqual(null, cl.GetValue("foo"));
      Assert.AreEqual("value1", cl.GetValue("param1"));
      Assert.AreEqual("value2", cl.GetValue("param2"));
      Assert.AreEqual("value3", cl.GetValue("param3"));
      Assert.AreEqual("value4", cl.GetValue("param4"));
      Assert.AreEqual(null, cl.GetValue("param5"));

      Assert.AreEqual(null, cl.GetValue("PARAM1", StringComparison.Ordinal));
      Assert.AreEqual(null, cl.GetValue("PARAM2", StringComparison.Ordinal));
      Assert.AreEqual(null, cl.GetValue("PARAM3", StringComparison.Ordinal));
      Assert.AreEqual(null, cl.GetValue("PARAM4", StringComparison.Ordinal));

      Assert.AreEqual("bar", cl.GetValue("foo", "bar"));
      Assert.AreEqual("defaultvalue1", cl.GetValue("PARAM1", "defaultvalue1", StringComparison.Ordinal));
      Assert.AreEqual("defaultvalue2", cl.GetValue("PARAM2", "defaultvalue2", StringComparison.Ordinal));
      Assert.AreEqual("defaultvalue3", cl.GetValue("PARAM3", "defaultvalue3", StringComparison.Ordinal));
      Assert.AreEqual("defaultvalue4", cl.GetValue("PARAM4", "defaultvalue4", StringComparison.Ordinal));
      Assert.AreEqual("value5", cl.GetValue("param5", "value5"));
    }

    [Test]
    public void GetValueWithNullCheckTest()
    {
      var cl = new CommandLine(new[] { "/foo", "/param1:value1", "-param2=value2", "--param3value3", "/param4", "value4", "-param5" });

      Assert.AreEqual("value1", cl.GetValueWithNullCheck("param1"));
      Assert.Throws<CommandLineParameterException>(() => cl.GetValueWithNullCheck("notpresent"));
    }

    [Test]
    public void GetDateTimeTest()
    {
      var cl = new CommandLine(new[] { "/good:2012-12-5", "-bad=2012-13-13", "/fr-FR:5/12/2012" });
      var dt = new DateTime(2012, 12, 5);

      Assert.AreEqual(dt, cl.GetDateTime("good"));
      Assert.Throws<CommandLineParameterException>(() => cl.GetDateTime("bad"));
      Assert.AreEqual(dt, cl.GetDateTime("bad", dt));
      Assert.AreEqual(dt, cl.GetDateTime("fr-FR", CultureInfo.CreateSpecificCulture("fr-FR").DateTimeFormat));
    }

    [Test]
    public void GetDoubleTest()
    {
      var cl = new CommandLine(new[] { "/good1:123.45", "/good2:123456.78", "-bad=123,456.78", "/fr-FR:123,45" });
      var d1 = 123.45d;
      var d2 = 123456.78d;

      Assert.AreEqual(d1, cl.GetDouble("good1"));
      Assert.AreEqual(d2, cl.GetDouble("good2"));
      Assert.Throws<CommandLineParameterException>(() => cl.GetDouble("bad"));
      Assert.AreEqual(d1, cl.GetDouble("bad", d1));
      Assert.AreEqual(d1, cl.GetDouble("fr-FR", CultureInfo.CreateSpecificCulture("fr-FR").NumberFormat));
    }

    public enum EnumValue { First, Second, Third }

    [Test]
    public void GetEnumValueTest()
    {
      var cl = new CommandLine(new[] { "/param1:first", "-param2=second", "--param3third", "/param4", "fourth" });

      Assert.AreEqual(EnumValue.First, cl.GetEnumValue<EnumValue>("param1"));
      Assert.AreEqual(EnumValue.Second, cl.GetEnumValue<EnumValue>("PARAM2"));
      Assert.AreEqual(EnumValue.Third, cl.GetEnumValue<EnumValue>("PaRaM3"));
      Assert.Throws<CommandLineParameterException>(() => cl.GetEnumValue<EnumValue>("param4"));

      Assert.AreEqual(EnumValue.First, cl.GetEnumValue<EnumValue>("param4", EnumValue.First));
    }

    [Test]
    public void GetInt32Test()
    {
      var cl = new CommandLine(new[] { "/good:123", "-bad=12xyz" });
      var n = 123;

      Assert.AreEqual(n, cl.GetDouble("good"));
      Assert.Throws<CommandLineParameterException>(() => cl.GetDouble("bad"));
      Assert.AreEqual(n, cl.GetDouble("bad", n));
    }
  }
}
