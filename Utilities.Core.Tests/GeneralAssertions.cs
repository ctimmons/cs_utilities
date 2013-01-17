using System;

using NUnit.Framework;

using Utilities.Core;

namespace Utilities.Core.UnitTests
{
  [TestFixture]
  public class StringAssertionsTests
  {
    public StringAssertionsTests() : base() { }
  
    /* Check all of the ways an exception can be thrown by the Check() method. */

    [Test, ExpectedException(typeof(ArgumentNullException))]
    public void CheckNotNullTest()
    {
      Assertion.Check("testParam", null, StringAssertionOptions.NotNull);
      Assert.Fail();  // Fail if the previous line of code did NOT throw an exception.
    }

    [Test, ExpectedException(typeof(ArgumentException))]
    public void ChecLengthNotZeroTest()
    {
      Assertion.Check("testParam", string.Empty, StringAssertionOptions.LengthNotZero);
      Assert.Fail();
    }
    
    [Test, ExpectedException(typeof(ArgumentException))]
    public void CheckStrictLengthNotZeroTest()
    {
      Assertion.Check("testParam", "\t \r\n", StringAssertionOptions.TrimmedLengthNotZero);
      Assert.Fail();
    }
    
    [Test, ExpectedException(typeof(ArgumentNullException))]
    public void CheckNotNullAndStrictLengthNotZeroTest1()
    {
      Assertion.Check("testParam", null, 
        StringAssertionOptions.NotNull | StringAssertionOptions.TrimmedLengthNotZero);
      Assert.Fail();
    }
    
    [Test, ExpectedException(typeof(ArgumentException))]
    public void CheckNotNullAndStrictLengthNotZeroTest2()
    {
      Assertion.Check("testParam", "\t \r\n",
        StringAssertionOptions.NotNull | StringAssertionOptions.TrimmedLengthNotZero);
      Assert.Fail();
    }

    /* Now check the inverse condition: all of the ways the Check() method can be called
     * WITHOUT throwing an exception. 
     */

    [Test]
    public void CheckTest()
    {
      const string testParam = "Hello, world!";

      Assertion.Check("testParam", testParam, StringAssertionOptions.NotNull);
      Assertion.Check("testParam", testParam, StringAssertionOptions.LengthNotZero);
      Assertion.Check("testParam", testParam, StringAssertionOptions.TrimmedLengthNotZero);
      Assertion.Check("testParam", testParam, StringAssertionOptions.NotNull | StringAssertionOptions.TrimmedLengthNotZero);

      /* Generally, this is a sign of a bad testing methodology, i.e. asserting that
       * true is true.  In this case, it's here because none of the above statements
       * can really be tested when they DON'T throw an exception.  Essentially, if
       * this assertion is executed, then none of the above statements threw an
       * exception.
       */
      Assert.IsTrue(true);
    }
  }
}
