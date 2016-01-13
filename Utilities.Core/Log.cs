/* See UNLICENSE.txt file for license details. */

using System;
using System.IO;

namespace Utilities.Core
{
  public enum LogEntryType { Info, Warning, Error }

  /* The Log class provides a very simple logging facility.  It's only intended
     to be used when larger and more complete logging libraries,
     like Microsoft's Enterprise Library (http://msdn.microsoft.com/en-us/library/ff648951.aspx),
     are overkill.

     NOTE: Any TextWriter descendent passed to the Log constructor will
           NOT be closed or disposed of when the instance of Log is disposed.
  
     Examples:

       // Writing log data to a file.
       using (var sw = new StreamWriter("my_log_file.txt"))
       {
         var log = new Log(sw);
         log.WriteLine(LogEntryType.Info, "Hello, world!");
       }

       // Writing log data to the console.
       var log = new Log(Console.Out);
       log.WriteLine(LogEntryType.Info, "Hello, world!");
  */

  public class Log
  {
    private TextWriter _writer;

    private Log()
      : base()
    {
    }

    public Log(TextWriter writer)
      : this()
    {
      writer.Name("writer").NotNull();
      this._writer = writer;
    }

    public void WriteLine(LogEntryType logEntryType, String message)
    {
      /* Timestamps are represented in the Round Trip Format Specifier
         (http://msdn.microsoft.com/en-us/library/az4se3k1.aspx#Roundtrip). */
      var timestamp = DateTime.Now.ToUniversalTime().ToString("o");

      String type;
      switch (logEntryType)
      {
        case LogEntryType.Info:
          type = "INF";
          break;
        case LogEntryType.Warning:
          type = "WRN";
          break;
        case LogEntryType.Error:
          type = "ERR";
          break;
        default:
          type = "UNK";
          break;
      }

      this._writer.WriteLine(String.Format("{0} - {1} - {2}", timestamp, type, message));
    }

    public void WriteLine(LogEntryType logEntryType, String message, params Object[] args)
    {
      this.WriteLine(logEntryType, String.Format(message, args));
    }
  }
}
