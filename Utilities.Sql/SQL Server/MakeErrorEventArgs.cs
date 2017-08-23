using System;

namespace Utilities.Sql.SqlServer
{
  public class MakeErrorEventArgs : EventArgs
  {
    private Exception _exception;

    public MakeErrorEventArgs(Exception exception)
    {
      this._exception = exception;
    }

    public virtual Exception GetException()
    {
      return this._exception;
    }
  }
}
