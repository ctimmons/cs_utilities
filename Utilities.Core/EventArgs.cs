/* See UNLICENSE.txt file for license details. */

using System;

namespace Utilities.Core
{
  public class StringMessageEventArgs : EventArgs
  {
    public String Message { get; set; }

    public StringMessageEventArgs(String message)
      : base()
    {
      this.Message = message;
    }
  }
}
