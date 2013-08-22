/* See UNLICENSE.txt file for license details. */

using System;
using System.Data;

using NUnit.Framework;

namespace Utilities.Sql.Tests
{
  [TestFixture]
  public class ExtensionsTests
  {
    [Test]
    public void GetValueOrDefaultTest()
    {
      var dt = new DataTable();
      dt.Columns.Add("int32", typeof(Int32));

      dt.Rows.Add(new Object[] { DBNull.Value });
      dt.Rows.Add(new Object[] { 42 });
      dt.Rows.Add(new Object[] { DBNull.Value });
      dt.Rows.Add(new Object[] { 42 });

      using (var dr = dt.CreateDataReader())
      {
        // Reading a DBNull.Value into a nullable Int32 will succeed.
        dr.Read();
        dr.GetValueOrDefault<Int32?>("int32");

        // Reading 42 into a nullable Int32 will succeed.
        dr.Read();
        dr.GetValueOrDefault<Int32?>("int32");

        // Reading a DBNull.Value into a non-nullable Int32 will throw an InvalidCastException.
        dr.Read();
        Assert.Catch<InvalidCastException>(() => dr.GetValueOrDefault<Int32>("int32"));

        // Reading 42 into a non-nullable Int32 will succeed.
        dr.Read();
        dr.GetValueOrDefault<Int32>("int32");
      }
    }
  }
}
