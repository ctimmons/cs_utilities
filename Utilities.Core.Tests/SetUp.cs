using System;
using System.IO;

using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  [SetUpFixture]
  public class SetUp
  {
    private static String RootFolder;

    [SetUp]
    public void RunBeforeAnyTests()
    {
      RootFolder = Path.Combine(Win32Api.GetShellFolder(Win32Api.CSIDL_APPDATA), "Test Files") + Path.DirectorySeparatorChar;

      // Make sure the root test folder exists.
      Directory.CreateDirectory(RootFolder);
    }

    [TearDown]
    public void RunAfterAnyTests()
    {
      Directory.Delete(RootFolder);
    }
  }
}
