using System;
using System.Data;
using System.Data.SqlClient;

namespace Utilities.Core
{
  public static partial class AssertUtils
  {
    public static AssertionContext<SqlConnection> IsOpen(this SqlConnection value)
    {
      return (new AssertionContext<SqlConnection>(value)).IsOpen();
    }

    public static AssertionContext<SqlConnection> IsOpen(this AssertionContext<SqlConnection> value)
    {
      if (value.Value.State == ConnectionState.Open)
        return value;
      else
        throw new ArgumentException(String.Format(Utilities.Sql.Properties.Resources.Assert_IsOpen, value.Name));
    }
  }
}
