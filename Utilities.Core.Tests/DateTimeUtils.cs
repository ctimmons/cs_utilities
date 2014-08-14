/* See UNLICENSE.txt file for license details. */

using System;
using System.Linq;

using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  [TestFixture]
  public class DateTimeUtilsTests
  {
    private Tuple<DateTime, DateTime>[] _quarterDateRanges =
      new Tuple<DateTime, DateTime>[4]
      {
        new Tuple<DateTime, DateTime>(new DateTime(2000, 1, 1), new DateTime(2000, 3, 31)),
        new Tuple<DateTime, DateTime>(new DateTime(2000, 4, 1), new DateTime(2000, 6, 30)),
        new Tuple<DateTime, DateTime>(new DateTime(2000, 7, 1), new DateTime(2000, 9, 30)),
        new Tuple<DateTime, DateTime>(new DateTime(2000, 10, 1), new DateTime(2000, 12, 31))
      };

    private void RunActionOverQuarterDateRanges(Action<Int32, DateTime, DateTime, DateTime> action)
    {
      for (var quarter = 0; quarter < this._quarterDateRanges.Length; quarter++)
      {
        var quarterStartDate = this._quarterDateRanges[quarter].Item1;
        var quarterEndDate = this._quarterDateRanges[quarter].Item2;

        for (var date = quarterStartDate; date <= quarterEndDate; date = date.AddDays(1))
          action(quarter + 1, quarterStartDate, quarterEndDate, date);
      }
    }

    [Test]
    public void AddQuarterTest()
    {
      this.RunActionOverQuarterDateRanges(
        (quarter, quarterStartDate, quarterEndDate, date) =>
        {
          Assert.AreEqual((quarter % 4) + 1, DateTimeUtils.AddQuarters(date, 1).Quarter());
          Assert.AreEqual(((quarter + 1) % 4) + 1, DateTimeUtils.AddQuarters(date, 2).Quarter());
          Assert.AreEqual(((quarter + 2) % 4) + 1, DateTimeUtils.AddQuarters(date, 3).Quarter());
          Assert.AreEqual(((quarter + 3) % 4) + 1, DateTimeUtils.AddQuarters(date, 4).Quarter());
        });
    }

    [Test]
    public void AreDatesInSameYearAndQuarterTest()
    {
      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.IsTrue(DateTimeUtils.AreDatesInSameYearAndQuarter(date, quarterStartDate)));
      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.IsFalse(DateTimeUtils.AreDatesInSameYearAndQuarter(date, quarterStartDate.AddYears(-1))));
    }

    [Test]
    public void GetFirstDayOfQuarterTest()
    {
      Assert.Throws<ArgumentExceptionFmt>(() => DateTimeUtils.GetFirstDayOfQuarter(2000, 0));
      Assert.Throws<ArgumentExceptionFmt>(() => DateTimeUtils.GetFirstDayOfQuarter(2000, 5));

      Assert.AreEqual(new DateTime(2000, 1, 1), DateTimeUtils.GetFirstDayOfQuarter(2000, 1));
      Assert.AreEqual(new DateTime(2000, 4, 1), DateTimeUtils.GetFirstDayOfQuarter(2000, 2));
      Assert.AreEqual(new DateTime(2000, 7, 1), DateTimeUtils.GetFirstDayOfQuarter(2000, 3));
      Assert.AreEqual(new DateTime(2000, 10, 1), DateTimeUtils.GetFirstDayOfQuarter(2000, 4));

      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.AreEqual(quarterStartDate, date.FirstDayOfQuarter()));
    }

    [Test]
    public void GetLastDayOfQuarterTest()
    {
      Assert.Throws<ArgumentExceptionFmt>(() => DateTimeUtils.GetLastDayOfQuarter(2000, 0));
      Assert.Throws<ArgumentExceptionFmt>(() => DateTimeUtils.GetLastDayOfQuarter(2000, 5));

      Assert.AreEqual(new DateTime(2000, 3, 31), DateTimeUtils.GetLastDayOfQuarter(2000, 1));
      Assert.AreEqual(new DateTime(2000, 6, 30), DateTimeUtils.GetLastDayOfQuarter(2000, 2));
      Assert.AreEqual(new DateTime(2000, 9, 30), DateTimeUtils.GetLastDayOfQuarter(2000, 3));
      Assert.AreEqual(new DateTime(2000, 12, 31), DateTimeUtils.GetLastDayOfQuarter(2000, 4));

      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.AreEqual(quarterEndDate, date.LastDayOfQuarter()));
    }

    [Test]
    public void GetQuarterTest()
    {
      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.AreEqual(quarter, date.Quarter()));
    }

    [Test]
    public void IsDateInYearAndQuarterTest()
    {
      Assert.Throws<ArgumentExceptionFmt>(() => DateTimeUtils.IsDateInYearAndQuarter(new DateTime(2000, 1, 1), 2000, 0));
      Assert.Throws<ArgumentExceptionFmt>(() => DateTimeUtils.IsDateInYearAndQuarter(new DateTime(2000, 1, 1), 2000, 5));

      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.IsTrue(DateTimeUtils.IsDateInYearAndQuarter(date, quarterStartDate.Year, quarter)));
      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.IsFalse(DateTimeUtils.IsDateInYearAndQuarter(date, quarterStartDate.Year + 1, quarter)));
    }

    [Test]
    public void MaxTest()
    {
      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.AreEqual(date.AddYears(1), DateTimeUtils.Max(date.AddYears(1), quarterStartDate)));
    }

    [Test]
    public void MinTest()
    {
      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.AreEqual(date, DateTimeUtils.Min(date, quarterStartDate.AddYears(1))));
    }

    [Test]
    public void ToTest()
    {
      var startDateTime = new DateTime(2000, 1, 1);
      var endDateTime = new DateTime(2000, 1, 1);
      var days = startDateTime.To(endDateTime);

      /* A start date that is the same as the end date should result
         in a list of days with one DateTime value, and that value should be
         the same as the start and end dates. */
      Assert.AreEqual(days.Count(), 1);
      Assert.AreEqual(days.First(), startDateTime);

      //////////////////////////////////////////////////////////////////////////////////

      endDateTime = new DateTime(2000, 1, 10);
      days = startDateTime.To(endDateTime);

      /* A start date that is earlier than the end date should result
         in a list of DateTimes that fall between those two dates, inclusive. */
      Assert.AreEqual(days.Count(), 10);

      //////////////////////////////////////////////////////////////////////////////////

      startDateTime = new DateTime(2000, 1, 10);
      endDateTime = new DateTime(2000, 1, 1);
      days = startDateTime.To(endDateTime);

      /* A start date that is later than the end date should result
         in a list of DateTimes that fall between those two dates, inclusive,
         but in descending order. */
      Assert.AreEqual(days.Count(), 10);
    }
  }
}
