/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  [TestFixture]
  public class LogTests
  {
    private static readonly String _logFileDirectory = FileUtils.GetTemporarySubfolder();

    [SetUp]
    public void Init()
    {
      Directory.CreateDirectory(_logFileDirectory);
    }

    [TearDown]
    public void Cleanup()
    {
      if (Directory.Exists(_logFileDirectory))
        Directory.Delete(_logFileDirectory, true /* Delete all files and subdirectories also. */);
    }

    [Test]
    public void WriteLineTest()
    {
      var logFilename = Path.Combine(_logFileDirectory, "logtest.txt");
      using (var sw = new StreamWriter(logFilename))
      {
        var log = new Log(sw);
        log.WriteLine(LogEntryType.Info, "info log entry");
        log.WriteLine(LogEntryType.Warning, "warning log entry");
        log.WriteLine(LogEntryType.Error, "error log entry");
      }

      var logFileContents = File.ReadAllLines(logFilename);

      /*
        Log entries look like this:

          2013-01-26T02:00:26.3637578Z - INF - Hello, world!
          2013-01-26T02:00:26.3793828Z - WRN - Hello, world!
          2013-01-26T02:00:26.3793828Z - ERR - Hello, world!
      */

      Assert.IsTrue(this.DoesMatchLogEntryFormat(logFileContents[0], "INF", "info log entry"), logFileContents[0]);
      Assert.IsTrue(this.DoesMatchLogEntryFormat(logFileContents[1], "WRN", "warning log entry"), logFileContents[1]);
      Assert.IsTrue(this.DoesMatchLogEntryFormat(logFileContents[2], "ERR", "error log entry"), logFileContents[2]);
    }

    private Boolean DoesMatchLogEntryFormat(String logEntry, String logEntryType, String logMessage)
    {
      var logEntryParts = logEntry.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);

      if (logEntryParts.Length != 3)
        throw new ArgumentException(String.Format("There are {0} parts in logEntry.  There should be 3.  logEntry = '{1}'.", logEntryParts.Length, logEntry));

      /* The timestamp portion should be in the "o" (round trip) format. */
      try
      {
        DateTime.ParseExact(logEntryParts[0], "o", CultureInfo.InvariantCulture);
      }
      catch
      {
        return false;
      }

      return
        logEntryType.Equals(logEntryParts[1], StringComparison.CurrentCulture) &&
        logMessage.Equals(logEntryParts[2], StringComparison.CurrentCulture);
    }
  }
}
