/* See UNLICENSE.txt file for license details. */

using System;
using System.Text;

namespace Utilities.Core
{
  public static class RandomRoutines
  {
    private static readonly Random _rand = new Random();
    private static readonly Object _semaphore = new Object(); /* System.Random instance methods are not thread safe. */

    public static Boolean GetCoinFlip()
    {
      return GetCoinFlip(0.5);
    }

    public static Boolean GetCoinFlip(Double probability) // 0 < probability < 1
    {
      if ((probability <= 0.0) || (probability >= 1.0))
        throw new ArgumentOutOfRangeExceptionFmt(Properties.Resources.Random_ProbabilityNotInRange, probability);
      else
        lock (_semaphore)
          return (_rand.NextDouble() < probability);
    }

    public static Byte GetRandomByte(Byte low, Byte high)
    {
      return (Byte) GetRandomInt64(low, high);
    }

    public static Int16 GetRandomInt16(Int16 low, Int16 high)
    {
      return (Int16) GetRandomInt64(low, high);
    }

    public static Int32 GetRandomInt32(Int32 low, Int32 high)
    {
      return (Int32) GetRandomInt64(low, high);
    }

    public static Int64 GetRandomInt64(Int64 low, Int64 high)
    {
      return (Int64) GetRandomDouble(low, high);
    }

    public static Double GetRandomDouble(Double low, Double high)
    {
      if (low >= high)
        throw new ArgumentOutOfRangeExceptionFmt(Properties.Resources.Random_LowGreaterThanHigh, low, high);
      else
        lock (_semaphore)
          return (low + (_rand.NextDouble() * (high - low)));
    }

    public static DateTime GetRandomDateTime(DateTime lowDate, DateTime highDate)
    {
      if (lowDate > highDate)
        throw new ArgumentOutOfRangeExceptionFmt(Properties.Resources.Random_LowDateGreaterThanHighDate, lowDate, highDate);
      else
        return new DateTime(GetRandomInt64(lowDate.Ticks, highDate.Ticks));
    }

    public static DateTime GetRandomTime(DateTime date)
    {
      return GetRandomDateTime(date, date);
    }

    public enum LetterCaseMix
    {
      AllUpperCase,
      AllLowerCase,
      MixUpperCaseAndLowerCase
    };

    public static String GetRandomString(Char lowChar, Char highChar, Int32 count, LetterCaseMix letterCaseMix)
    {
      if (lowChar >= highChar)
        throw new ArgumentOutOfRangeExceptionFmt(Properties.Resources.Random_LowCharGreaterThanHighChar, lowChar, highChar);

      if (count < 1)
        throw new ArgumentOutOfRangeExceptionFmt(Properties.Resources.Random_CountOutOfRange, count);

      var result = new StringBuilder(count);
      Char randomChar;

      for (Int32 idx = 0; idx < count; idx++)
      {
        lock (_semaphore)
          randomChar = (Char) _rand.Next(lowChar, highChar + 1);

        switch (letterCaseMix)
        {
          case LetterCaseMix.AllUpperCase:
            result.Append(Char.ToUpper(randomChar));
            break;

          case LetterCaseMix.AllLowerCase:
            result.Append(Char.ToLower(randomChar));
            break;

          case LetterCaseMix.MixUpperCaseAndLowerCase:
            if (GetCoinFlip())
              result.Append(Char.ToUpper(randomChar));
            else
              result.Append(Char.ToLower(randomChar));
            break;

          default:
            throw new ArgumentOutOfRangeExceptionFmt(Properties.Resources.Random_UnknownLetterCaseMixValue, letterCaseMix);
        }
      }

      return result.ToString();
    }
  }
}
