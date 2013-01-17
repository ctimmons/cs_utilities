/* See UNLICENSE.txt file for license details. */

using System;

namespace Utilities.Core
{
  public static class DateTimeUtils
  {
    #region Quarter Methods
    public static Int32 GetQuarter(DateTime dateTime)
    {
      return ((dateTime.Month + 2) / 3);
    }

    public static DateTime AddQuarters(DateTime dateTime, Int32 quarters)
    {
      return dateTime.AddMonths(quarters * 3);
    }

    private static void CheckQuarter(Int32 quarter)
    {
      if ((quarter < 1) || (quarter > 4))
        throw new ArgumentExceptionFmt(Properties.Resources.DateTimeUtils_QuarterNotInRange, quarter);
    }

    public static DateTime GetFirstDayOfQuarter(Int32 year, Int32 quarter)
    {
      CheckQuarter(quarter);
      return AddQuarters(new DateTime(year, 1, 1), quarter - 1);
    }

    public static DateTime GetFirstDayOfQuarter(DateTime dateTime)
    {
      return AddQuarters(new DateTime(dateTime.Year, 1, 1), GetQuarter(dateTime) - 1);
    }

    public static DateTime GetLastDayOfQuarter(Int32 year, Int32 quarter)
    {
      CheckQuarter(quarter);
      return AddQuarters(new DateTime(year, 1, 1), quarter).AddDays(-1);
    }

    public static DateTime GetLastDayOfQuarter(DateTime dateTime)
    {
      return AddQuarters(new DateTime(dateTime.Year, 1, 1), GetQuarter(dateTime)).AddDays(-1);
    }

    public static Boolean AreDatesInSameYearAndQuarter(DateTime datetime1, DateTime datetime2)
    {
      return (GetFirstDayOfQuarter(datetime1) == GetFirstDayOfQuarter(datetime2));
    }

    public static Boolean IsDateInYearAndQuarter(DateTime datetime, Int32 year, Int32 quarter)
    {
      CheckQuarter(quarter);
      return ((datetime.Year == year) && (GetQuarter(datetime) == quarter));
    }
    #endregion

    public static DateTime Min(DateTime dateTime1, DateTime dateTime2)
    {
      return (dateTime1 < dateTime2) ? dateTime1 : dateTime2;
    }

    public static DateTime Max(DateTime dateTime1, DateTime dateTime2)
    {
      return (dateTime1 > dateTime2) ? dateTime1 : dateTime2;
    }
  }
}