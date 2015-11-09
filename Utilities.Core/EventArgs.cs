/* See UNLICENSE.txt file for license details. */

using System;
using System.Threading;

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

  public static class EventArgsExtensions
  {
    /* Thread-safe way to raise an event from "CLR via C#, 4th Edition"
       by Jeffrey Richter, pp. 254-255. */
    public static void Raise<TEventArgs>(this TEventArgs eventArgs, Object sender, ref EventHandler<TEventArgs> handler)
      where TEventArgs : EventArgs
    {
      EventHandler<TEventArgs> temp = Volatile.Read(ref handler);

      if (temp != null)
        temp(sender, eventArgs);
    }
  }
}
