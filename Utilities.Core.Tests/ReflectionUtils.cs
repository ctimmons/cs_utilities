/* See UNLICENSE.txt file for license details. */

using System;
using System.Data;
using System.Data.SqlClient;

using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  public class Test
  {
    public ConsoleModifiers ConsoleModifiers { get; set; }
  }

  [TestFixture]
  public class ReflectionUtilsTests
  {
    [Test]
    public void GetObjectInitializerTest()
    {
      var t = new Test() { ConsoleModifiers = ConsoleModifiers.Alt | ConsoleModifiers.Shift };
      Assert.AreEqual("new Test() { ConsoleModifiers = ConsoleModifiers.Alt | ConsoleModifiers.Shift }", ReflectionUtils.GetObjectInitializer(t));

      var p = new SqlParameter() { ParameterName = "@BusinessEntityID", SqlDbType = SqlDbType.Char, Value = 'a' };
      Assert.AreEqual(@"new SqlParameter() { DbType = DbType.AnsiStringFixedLength, ParameterName = ""@BusinessEntityID"", Size = 1, SqlDbType = SqlDbType.Char, SqlValue = a, Value = 'a' }", ReflectionUtils.GetObjectInitializer(p));
      Assert.AreEqual(@"new SqlParameter() { ParameterName = ""@BusinessEntityID"", Size = 1, SqlDbType = SqlDbType.Char, Value = 'a' }", ReflectionUtils.GetObjectInitializer(p, "DbType", "SqlValue"));
    }
  }
}
