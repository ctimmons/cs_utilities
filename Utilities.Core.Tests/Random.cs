/* See the LICENSE.txt file in the root folder for license details. */

using System;

using NUnit.Framework;

namespace Utilities.Core.UnitTests
{
  [TestFixture]
  public class RandomRoutinesTests
  {
    [Test]
    public void GetCoinFlipTest()
    {
      // Flip a coin TEST_ITERATION times.  On balance, it should return
      // true ("heads") %50 of the time, + or - %MARGIN_OF_ERROR.

      const Int32 TEST_ITERATIONS = 100000;
      const Double MARGIN_OF_ERROR = 0.01d; // One percent.
      const Double LOWER_ACCEPTABLE_BOUND = 0.5d - MARGIN_OF_ERROR;
      const Double UPPER_ACCEPTABLE_BOUND = 0.5d + MARGIN_OF_ERROR;

      Int32 numberOfHeads = 0;

      for (Int32 i = 1; i <= TEST_ITERATIONS; i++)
        numberOfHeads += (RandomRoutines.GetCoinFlip() ? 1 : 0);

      var percentOfHeadsFlips = (Double) numberOfHeads / (Double) TEST_ITERATIONS;

      var success = ((LOWER_ACCEPTABLE_BOUND < percentOfHeadsFlips) && (percentOfHeadsFlips < UPPER_ACCEPTABLE_BOUND));

      Assert.IsTrue(
        success,
        String.Format("{0} out of {1} coin flips ({2}%) returned true.  Target was 50% (+/- {3}%).",
          numberOfHeads, TEST_ITERATIONS, percentOfHeadsFlips * 100, MARGIN_OF_ERROR * 100));
    }

    [Test]
    public void GetProbabilityCoinFlipTest()
    {
      // Flip a coin TEST_ITERATION times.  On balance, it should return
      // true ("heads") %PROBABILITY of the time, + or - %MARGIN_OF_ERROR.

      const Int32 TEST_ITERATIONS = 100000;
      const Double MARGIN_OF_ERROR = 0.01d; // One percent.
      const Double PROBABILITY = 0.38d; // # between 0 and 1 exclusive.
      const Double LOWER_ACCEPTABLE_BOUND = PROBABILITY - MARGIN_OF_ERROR;
      const Double UPPER_ACCEPTABLE_BOUND = PROBABILITY + MARGIN_OF_ERROR;

      Int32 numberOfHeadsFlips = 0;

      // Count the number of "heads" results.
      for (Int32 i = 1; i <= TEST_ITERATIONS; i++)
        numberOfHeadsFlips += (RandomRoutines.GetCoinFlip((Double) PROBABILITY) ? 1 : 0);

      var percentOfHeadsFlips = (Double) numberOfHeadsFlips / (Double) TEST_ITERATIONS;

      var success = (
        (LOWER_ACCEPTABLE_BOUND < percentOfHeadsFlips) &&
        (percentOfHeadsFlips < UPPER_ACCEPTABLE_BOUND));

      Assert.IsTrue(
        success,
        String.Format("{0} out of {1} probability coin flips ({2} percent) returned true.  Target was {3}% (+/- {4}%).",
          numberOfHeadsFlips, TEST_ITERATIONS, percentOfHeadsFlips * 100, PROBABILITY * 100, MARGIN_OF_ERROR * 100));
    }

    [Test]
    public void GetDoubleInRangeTest()
    {
      const Int32 TEST_ITERATIONS = 100000;
      const Double LOWER_BOUND = 5862.0;
      const Double UPPER_BOUND = 5962.0;

      Double number = 0.0;
      Boolean noNumbersOutsideOfRangeReturned = true;
      for (Int32 i = 1; i <= TEST_ITERATIONS; i++)
      {
        number = RandomRoutines.GetRandomDouble(LOWER_BOUND, UPPER_BOUND);

        if ((number < LOWER_BOUND) || (number > UPPER_BOUND))
        {
          noNumbersOutsideOfRangeReturned = false;
          break;
        }
      }

      Assert.IsTrue(
        noNumbersOutsideOfRangeReturned,
        String.Format("Number = {0}. Lower bound = {1}, and upper bound = {2}.",
        number, LOWER_BOUND, UPPER_BOUND));
    }

    [Test]
    public void GetStringInRangeTest()
    {
      const Int32 RESULT_LENGTH = 100;
      const Char LO_LOWER_CHAR = 'a';
      const Char HI_LOWER_CHAR = 'n';
      const Char LO_UPPER_CHAR = 'A';
      const Char HI_UPPER_CHAR = 'N';

      // Test all lowercase result.
      String result = RandomRoutines.GetRandomString(LO_LOWER_CHAR, HI_LOWER_CHAR,
        RESULT_LENGTH, RandomRoutines.LetterCaseMix.AllLowerCase);

      Assert.IsTrue(
        result.Length == RESULT_LENGTH,
        String.Format("All Lowercase Test: Incorrect String length. " +
        "Expected {0} characters, but received {1} characters.",
        RESULT_LENGTH, result.Length));

      Boolean success = true;
      Char offendingChar = '+';
      for (Int32 i = 0; i < RESULT_LENGTH; i++)
      {
        // Check each character to make it falls w/i the specified range.
        if ((result[i] < LO_LOWER_CHAR) || (result[i] > HI_LOWER_CHAR))
        {
          success = false;
          offendingChar = result[i];
          break;
        }
      }

      Assert.IsTrue(
        success,
        String.Format("All Lowercase Test: Character not in range. " +
        "The character '{0}' is not within the specified range of '{1}' and '{2}'.",
        offendingChar, LO_LOWER_CHAR, HI_LOWER_CHAR));

      // Test all uppercase result.
      result = RandomRoutines.GetRandomString(LO_UPPER_CHAR, HI_UPPER_CHAR,
        RESULT_LENGTH, RandomRoutines.LetterCaseMix.AllUpperCase);

      Assert.IsTrue(
        result.Length == RESULT_LENGTH,
        String.Format("All Uppercase Test: Incorrect String length. " +
        "Expected {0} characters, but received {1} characters.",
        RESULT_LENGTH, result.Length));

      success = true;
      offendingChar = '+';
      for (Int32 i = 0; i < RESULT_LENGTH; i++)
      {
        // Check each character to make it falls w/i the specified range.
        if ((result[i] < LO_UPPER_CHAR) || (result[i] > HI_UPPER_CHAR))
        {
          success = false;
          offendingChar = result[i];
          break;
        }
      }

      Assert.IsTrue(
        success,
        String.Format("All Uppercase Test: Character not in range. " +
        "The character '{0}' is not within the specified range of '{1}' and '{2}'.",
        offendingChar, LO_UPPER_CHAR, HI_UPPER_CHAR));

      // Test mixed-case result.
      result = RandomRoutines.GetRandomString(LO_UPPER_CHAR, HI_UPPER_CHAR,
        RESULT_LENGTH, RandomRoutines.LetterCaseMix.MixUpperCaseAndLowerCase);

      Assert.IsTrue(
        result.Length == RESULT_LENGTH,
        String.Format("Mixed-case Test: Incorrect String length. " +
        "Expected {0} characters, but received {1} characters.",
        RESULT_LENGTH, result.Length));

      success = true;
      offendingChar = '+';
      for (Int32 i = 0; i < RESULT_LENGTH; i++)
      {
        // Check each character to make it falls w/i the specified range.
        if ((Char.ToUpper(result[i]) < LO_UPPER_CHAR) || (Char.ToUpper(result[i]) > HI_UPPER_CHAR))
        {
          success = false;
          offendingChar = result[i];
          break;
        }
      }

      Assert.IsTrue(
        success,
        String.Format("Mixed-case Test: Character not in range. " +
        "The character '{0}' is not within the specified range of '{1}' and '{2}'.",
        offendingChar, LO_UPPER_CHAR, HI_UPPER_CHAR));
    }
  }
}
