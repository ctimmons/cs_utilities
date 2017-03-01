/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.Collections.Generic;

namespace Utilities.Core
{
  public static class DateTimeUtils
  {
    #region Quarter Methods
    public static Int32 Quarter(this DateTime dateTime)
    {
      return ((dateTime.Month + 2) / 3);
    }

    public static DateTime AddQuarters(this DateTime dateTime, Int32 quarters)
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
      return (new DateTime(year, 1, 1)).AddQuarters(quarter - 1);
    }

    public static DateTime FirstDayOfQuarter(this DateTime dateTime)
    {
      return (new DateTime(dateTime.Year, 1, 1)).AddQuarters(dateTime.Quarter() - 1);
    }

    public static DateTime GetLastDayOfQuarter(Int32 year, Int32 quarter)
    {
      CheckQuarter(quarter);
      return (new DateTime(year, 1, 1)).AddQuarters(quarter).AddDays(-1);
    }

    public static DateTime LastDayOfQuarter(this DateTime dateTime)
    {
      return (new DateTime(dateTime.Year, 1, 1)).AddQuarters(dateTime.Quarter()).AddDays(-1);
    }

    public static Boolean AreDatesInSameYearAndQuarter(DateTime dateTime1, DateTime dateTime2)
    {
      return (dateTime1.FirstDayOfQuarter() == dateTime2.FirstDayOfQuarter());
    }

    public static Boolean IsDateInYearAndQuarter(DateTime dateTime, Int32 year, Int32 quarter)
    {
      CheckQuarter(quarter);
      return ((dateTime.Year == year) && (dateTime.Quarter() == quarter));
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

    /* endDateTime can be earlier, the same, or later than startDateTime.
    
       If both parameters are the same date, then a list with one DateTime element
       is returned, and the element's value is the same as startDateTime (and endDateTime).

       If endDateTime is later than startDateTime, then an ascending list of DateTimes
       is returned.  Likewise, if endDateTime is earlier than startDateTime, a descending
       list of DateTimes is returned.

       For example:

         var time1 = new DateTime(2000, 1, 1);
         var time2 = new DateTime(2000, 1, 3);

         Calling time1.To(time2) will return a list
         of DateTimes in ascending order:

           1/1/2000
           1/2/2000
           1/3/2000

         The reverse call to time2.To(time1) will
         return a descending list of DateTimes:

           1/3/2000
           1/2/2000
           1/1/2000
    */

    public static IEnumerable<DateTime> To(this DateTime startDateTime, DateTime endDateTime)
    {
      var signedNumberOfDays = (endDateTime - startDateTime).Days;
      var sign = (signedNumberOfDays < 0) ? -1 : 1;
      var absoluteNumberOfDays = Math.Abs(signedNumberOfDays) + 1; /* "+ 1" to ensure both the start and end dates are included in the result set. */
      var result = new List<DateTime>(absoluteNumberOfDays);

      for (var day = 0; day < absoluteNumberOfDays; day++)
        result.Add(startDateTime.AddDays(day * sign));

      return result;
    }
  }
}