/* See UNLICENSE.txt file for license details. */

using System;
using System.ComponentModel;

namespace Utilities.Core
{
  public static class InteropUtils
  {
    public static IntPtr CheckForBadHandle(IntPtr handle)
    {
      var BAD_HANDLE = IntPtr.Zero;

      if (handle == BAD_HANDLE)
        throw new Win32Exception();
      else
        return handle;
    }

    public static void CheckForFalseResult(Boolean resultOfInteropCall)
    {
      if (!resultOfInteropCall)
        throw new Win32Exception();
    }
  }
}
