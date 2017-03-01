/* See the LICENSE.txt file in the root folder for license details. */

using System;

namespace Utilities.Sql.SqlServer
{
  /// <summary>
  /// The three currently supported target languages are C#, F# and VB.
  /// <para>TSQL is always supported, so there's no need for a separate TSQL value in this enumeration.</para>
  /// <para>The languages are broken out by version number.  This is to allow for flexibility in generating things
  /// like properties, where the later versions of a language support auto properties, but the earlier versions do not.</para>
  /// <para>See <a href="http://en.wikipedia.org/wiki/C_Sharp_%28programming_language%29#Versions">C# (Programming Language)</a> for a complete table of C# version numbers.</para>
  /// <para>See <a href="http://en.wikipedia.org/wiki/Visual_Basic_.NET">Visual Basic.Net</a> for a list of VB.Net version numbers.</para>
  /// </summary>
  public enum TargetLanguage
  {
    /// <summary>
    /// .Net 1.0, Visual Studio 2002.
    /// </summary>
    CSharp_1_0,

    /// <summary>
    /// .Net 1.1, Visual Studio 2003.  And yes, it really is C# 1.2, not C# 1.1.
    /// </summary>
    CSharp_1_2,

    /// <summary>
    /// .Net 2.0, Visual Studio 2005.
    /// </summary>
    CSharp_2_0,

    /// <summary>
    /// .Net 3.0, Visual Studio 2008.
    /// </summary>
    CSharp_3_0,

    /// <summary>
    /// .Net 4.0, Visual Studio 2010.
    /// </summary>
    CSharp_4_0,

    /// <summary>
    /// .Net 4.5, Visual Studio 2012 and .Net 4.5.1, 2013.
    /// </summary>
    CSharp_5_0,

    /// <summary>
    /// Convenience value equal to the latest version of C#.
    /// </summary>
    CSharp_Latest = CSharp_5_0,


    /// <summary>
    /// .Net 4.0, Visual Studio 2010.
    /// </summary>
    FSharp_2_0,

    /// <summary>
    /// .Net 4.5, Visual Studio 2012.
    /// </summary>
    FSharp_3_0,

    /// <summary>
    /// .Net 4.5.1, Visual Studio 2013.
    /// </summary>
    FSharp_3_1,

    /// <summary>
    /// Convenience value equal to the latest version of F#.
    /// </summary>
    FSharp_Latest = FSharp_3_1,


    /// <summary>
    /// .Net 1.0, Visual Studio 2002.
    /// </summary>
    VisualBasic_7_0,

    /// <summary>
    /// .Net 1.1, Visual Studio 2003.
    /// </summary>
    VisualBasic_7_1,

    /// <summary>
    /// .Net 2.0, Visual Studio 2005.
    /// </summary>
    VisualBasic_8_0,

    /// <summary>
    /// .Net 3.0, Visual Studio 2008.
    /// </summary>
    VisualBasic_9_0,

    /// <summary>
    /// .Net 4.0, Visual Studio 2010.
    /// </summary>
    VisualBasic_10_0,

    /// <summary>
    /// .Net 4.5, Visual Studio 2012.
    /// </summary>
    VisualBasic_11_0,

    /// <summary>
    /// .Net 4.5.1, Visual Studio 2013. (Just a version number bump - no new language features).
    /// </summary>
    VisualBasic_12_0,

    /// <summary>
    /// Convenience value equal to the latest version of VB.Net.
    /// </summary>
    VisualBasic_Latest = VisualBasic_12_0
  }

  /// <summary>
  /// System.Enum types can't have methods directly attached to them, so extension methods are a necessary workaround.
  /// </summary>
  public static class TargetLanguageExtensionMethods
  {
    /// <summary>
    /// Not all versions of a target language support auto properties.  C# 3.0 and later, F# 3.0 and later, and VB.Net 10.0 and later do.
    /// </summary>
    public static Boolean DoesSupportAutoProperties(this TargetLanguage targetLanguage)
    {
      return
        ((targetLanguage >= TargetLanguage.CSharp_3_0) && (targetLanguage <= TargetLanguage.CSharp_Latest)) ||
        ((targetLanguage >= TargetLanguage.FSharp_3_0) && (targetLanguage <= TargetLanguage.FSharp_Latest)) ||
        ((targetLanguage >= TargetLanguage.VisualBasic_10_0) && (targetLanguage <= TargetLanguage.VisualBasic_Latest));
    }

    public static Boolean IsCSharp(this TargetLanguage targetLanguage)
    {
      return (targetLanguage >= TargetLanguage.CSharp_1_0) && (targetLanguage <= TargetLanguage.CSharp_Latest);
    }

    public static Boolean IsFSharp(this TargetLanguage targetLanguage)
    {
      return (targetLanguage >= TargetLanguage.FSharp_2_0) && (targetLanguage <= TargetLanguage.FSharp_Latest);
    }

    public static Boolean IsVisualBasic(this TargetLanguage targetLanguage)
    {
      return (targetLanguage >= TargetLanguage.VisualBasic_7_0) && (targetLanguage <= TargetLanguage.VisualBasic_Latest);
    }

    public static Boolean IsCaseSensitive(this TargetLanguage targetLanguage)
    {
      return targetLanguage.IsCSharp() || targetLanguage.IsFSharp();
    }
  }
}
