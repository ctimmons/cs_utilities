/* See the LICENSE.txt file in the root folder for license details. */

using System;

using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  [TestFixture]
  public class ExceptionsTests
  {
    [Test]
    public void GetAllExceptionMessagesTest()
    {
      var result = String.Format("First Message{0}Second Message{0}Third Message{0}", Environment.NewLine);
      var exception = new Exception("First Message", new Exception("Second Message", new Exception("Third Message")));
      Assert.AreEqual(result, ExceptionUtils.GetAllExceptionMessages(exception));
    }
  }
}
