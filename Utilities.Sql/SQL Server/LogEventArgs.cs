using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Utilities.Core;

namespace Utilities.Sql.SqlServer
{
  public class LogEventArgs : EventArgs
  {
    private EventLogEntryType _eventLogEntryType;
    private Exception _exception;
    private String _message;

    private LogEventArgs()
      : base()
    {
    }

    public LogEventArgs(EventLogEntryType eventLogEntryType, String message)
      : this()
    {
      var stackFrame =
        (new StackTrace(true))
        .GetFrames()
        /* Skips the ".ctor" frames that occur before the "RaiseLogEvent" frames. */
        .SkipWhile(frame => !frame.GetMethod().Name.ContainsCI("RaiseLogEvent"))
        /* Skip the "RaiseLogEvent" frames. */
        .SkipWhile(frame => frame.GetMethod().Name.ContainsCI("RaiseLogEvent"))
        /* The stack frame after all of the skipped frames will be the call site
           that raised this event. */
        .First();
      var name = stackFrame.GetMethod().Name;
      var lineNumber =
        /* In JIT optimized code, line numbers are either
           not available or are wildly inaccurate. */
        Assembly.GetExecutingAssembly().IsJITOptimized()
        ? ""
        : $" - Line {stackFrame.GetFileLineNumber()}";

      this._message = $"{name}{lineNumber} - {message}";
      this._eventLogEntryType = eventLogEntryType;
    }

    public LogEventArgs(String message)
      : this(EventLogEntryType.Information, message)
    {
    }

    public LogEventArgs(Exception exception)
      : this()
    {
      this._exception = exception;
      this._eventLogEntryType = EventLogEntryType.Error;
    }

    public virtual EventLogEntryType GetEventLogEntryType()
    {
      return this._eventLogEntryType;
    }

    public virtual Exception GetException()
    {
      return this._exception;
    }

    public virtual String GetMessage()
    {
      return this._message;
    }
  }
}
