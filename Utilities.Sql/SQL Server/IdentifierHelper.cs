using System;
using System.Collections.Generic;
using System.Linq;

using Utilities.Core;

namespace Utilities.Sql.SqlServer
{
  public static class IdentifierHelper
  {
    private static readonly List<String> _csharpKeywords =
      new List<String>()
      {
        /* Keywords. */
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "cons",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "void",
        "volatile",
        "while",

        /* Contextual keywords and reserved words. */
        "alias",
        "ascending",
        "by",
        "const",
        "descending",
        "equals",
        "field",
        "from",
        "get",
        "group",
        "into",
        "join",
        "let",
        "method",
        "on",
        "orderby",
        "param",
        "partial",
        "property",
        "select",
        "set",
        "type",
        "var",
        "where",
        "yield",

        /* Not formally listed in the language spec as keywords
           or reserved words, but should be treated as such anyways. */
        "async",
        "await"
      };

    private static readonly List<String> _fsharpKeywords =
      new List<String>()
      {
        /* Keywords. */
        "_",
        "abstract",
        "and",
        "as",
        "assert",
        "asr",
        "base",
        "begin",
        "bool",
        "bytearray",
        "char",
        "class",
        "default",
        "delegate",
        "do",
        "done",
        "downcast",
        "downto",
        "elif",
        "else",
        "end",
        "exception",
        "explicit",
        "extern",
        "false",
        "finally",
        "float32",
        "float64",
        "for",
        "fun",
        "function",
        "global",
        "if",
        "in",
        "inherit",
        "inline",
        "instance",
        "int",
        "int8",
        "int16",
        "int32",
        "int64",
        "interface",
        "internal",
        "land",
        "lazy",
        "let",
        "lor",
        "lsl",
        "lsr",
        "lxor",
        "match",
        "member",
        "method",
        "mod",
        "module",
        "mutable",
        "namespace",
        "native",
        "new",
        "null",
        "object",
        "of",
        "open",
        "or",
        "override",
        "private",
        "public",
        "rec",
        "return",
        "sig",
        "static",
        "string",
        "struct",
        "then",
        "to",
        "true",
        "try",
        "type",
        "uint",
        "uint8",
        "uint16",
        "uint32",
        "uint64",
        "unmanaged",
        "unsigned",
        "upcast",
        "use",
        "val",
        "value",
        "valuetype",
        "vararg",
        "void",
        "when",
        "while",
        "with",
        "yield",

        /* Reserved words. */
        "atomic",
        "break",
        "checked",
        "component",
        "const",
        "constraint",
        "constructor",
        "continue",
        "eager",
        "fixed",
        "fori",
        "functor",
        "include",
        "measure",
        "method",
        "mixin",
        "object",
        "parallel",
        "params",
        "process",
        "protected",
        "pure",
        "recursive",
        "sealed",
        "tailcall",
        "trait",
        "virtual",
        "volatile"
      };

    private static readonly List<String> _visualBasicKeywords =
      new List<String>()
      {
        /* Keywords. */
        "AddHandler",
        "AddressOf",
        "Alias",
        "And",        "AndAlso",
        "As",
        "Boolean",
        "ByRef",        "Byte",
        "ByVal",
        "Call",
        "Case",        "Catch",
        "CBool",
        "CByte",
        "CChar",        "CDate",
        "CDbl",
        "CDec",
        "Char",        "CInt",
        "Class",
        "Class_Finalize",
        "Class_Initialize",
        "CLng",
        "CObj",        "Const",
        "Continue",
        "CSByte",
        "CShort",        "CSng",
        "CStr",
        "CType",
        "CUInt",        "CULng",
        "CUShort",
        "Date",
        "Decimal",        "Declare",
        "Default",
        "Delegate",
        "Dim",        "DirectCast",
        "Do",
        "Double",
        "Each",        "Else",
        "ElseIf",
        "End",
        "EndIf",        "Enum",
        "Erase",
        "Error",
        "Event",        "Exit",
        "False",
        "Finally",
        "For",        "Friend",
        "Function",
        "Get",
        "GetType",        "GetXmlNamespace",
        "Global",
        "GoSub",
        "GoTo",        "Handles",
        "If",
        "Implements",
        "Imports",        "In",
        "Inherits",
        "Integer",
        "Interface",        "Is",
        "IsNot",
        "Let",
        "Lib",        "Like",
        "Long",
        "Loop",
        "Me",        "Mod",
        "Module",
        "MustInherit",
        "MustOverride",        "MyBase",
        "MyClass",
        "Namespace",
        "Narrowing",        "New",
        "Next",
        "Not",
        "Nothing",        "NotInheritable",
        "NotOverridable",
        "Object",
        "Of",        "On",
        "Operator",
        "Option",
        "Optional",        "Or",
        "OrElse",
        "Overloads",
        "Overridable",        "Overrides",
        "ParamArray",
        "Partial",
        "Private",        "Property",
        "Protected",
        "Public",
        "RaiseEvent",        "ReadOnly",
        "ReDim",
        "REM",
        "RemoveHandler",        "Resume",
        "Return",
        "SByte",
        "Select",        "Set",
        "Shadows",
        "Shared",
        "Short",        "Single",
        "Static",
        "Step",
        "Stop",        "String",
        "Structure",
        "Sub",
        "SyncLock",        "Then",
        "Throw",
        "To",
        "True",        "Try",
        "TryCast",
        "TypeOf",
        "UInteger",        "ULong",
        "UShort",
        "Using",
        "Variant",        "Wend",
        "When",
        "While",
        "Widening",        "With",
        "WithEvents",
        "WriteOnly",
        "Xor",
        /* Sometimes treated like keywords, but not really keywords. */
        "Aggregate",
        "AM",
        "Ansi",
        "Async",
        "Auto",
        "Await",
        "Binary",
        "By",
        "Compare",
        "Custom",
        "Distinct",
        "Equals",
        "From",
        "Group",
        "Infer",
        "Into",
        "IsFalse",
        "IsTrue",
        "Iterator",
        "Join",
        "Let",
        "Mid",
        "Off",
        "Order",
        "Out",
        "PM",
        "Preserve",
        "Select",
        "Skip",
        "Strict",
        "Take",
        "Text",
        "Unicode",
        "Until",
        "Where"
      };

    private static List<String> _targetLanguageKeywords;
    private static Configuration _configuration;
    private static Boolean _isInitialized = false;

    public static void Init(Configuration configuration)
    {
      _configuration = configuration;

      if (configuration.TargetLanguage.IsCSharp())
        _targetLanguageKeywords = _csharpKeywords;
      else if (configuration.TargetLanguage.IsFSharp())
        _targetLanguageKeywords = _fsharpKeywords;
      else if (configuration.TargetLanguage.IsVisualBasic())
        _targetLanguageKeywords = _visualBasicKeywords;
      else
        throw new NotImplementedException(String.Format(Properties.Resources.UnknownTargetLanguageValue, configuration.TargetLanguage));

      _isInitialized = true;
    }

    /// <summary>
    /// Convert an SQL identifier, like a database name or column name, into a valid
    /// identifier for the target language (C#, F#, etc.) that was set in
    /// the configuration.
    /// <para>NOTE: IdentifierHelper.Init() must be called before this method is called for the first time, otherwise an exception will be thrown.</para>
    /// <para>System.dll has code providers for both C# (Microsoft.CSharp.CSharpCodeProvider) and
    /// Visual Basic (Microsoft.VisualBasic.VBCodeProvider).  Why not use code from those classes
    /// to determine if a given string is a valid identifier?</para>
    /// <para>There are several reasons.  One is that .Net does not provide a corresponding F# code provider.
    /// The second reason is that the code providers' CreateValidIdentifier() method
    /// is written in a very naive way.  C#'s CreateValidIdentifier("42 is the answer!") returns "42 is the answer!", which is laughably wrong.
    /// A third reason is that the code providers' IsValidIdentifier() doesn't recognize all of the keywords/reserved words I think it should, like all of the
    /// LINQ keywords, and new keywords like C#'s async and await. (I understand these aren't really keywords, but they certainly shouldn't
    /// be treated as valid identifiers).</para>
    /// <para>The result of these drawbacks is this method, coupled with a lengthy list of keywords, reserved words, and
    /// any other words that shouldn't be used as identifiers in C#, VB, or F#.</para>
    /// </summary>
    /// <param name="sqlIdentifier"></param>
    /// <returns></returns>
    public static String GetTargetLanguageIdentifier(String sqlIdentifier)
    {
      if (!_isInitialized)
        throw new Exception(Properties.Resources.IdentiferHelperInitNotCalled);

      sqlIdentifier.Name("sqlIdentifier").NotNullEmptyOrOnlyWhitespace();

      var result = sqlIdentifier.Replace(" ", "_").Replace(".", "_");

      if (Char.IsDigit(result[0]))
        result = "_" + result;

      var stringComparison =
        _configuration.TargetLanguage.IsCaseSensitive()
          ? StringComparison.CurrentCulture
          : StringComparison.CurrentCultureIgnoreCase;

      if (_targetLanguageKeywords.Exists(keyword => String.Equals(keyword, result, stringComparison)))
        result = "_" + result;

      /* In C#, any identifier that starts with two underscores is assumed to be a special identifier
         reserved for the compiler.  Prepend another underscore to prevent C# compiler from choking
         on the generated code. */
      if (_configuration.TargetLanguage.IsCSharp() && (result.Length > 2) && result.StartsWith("__") && (result[2] != '_'))
        result = "_" + result;

      return result;
    }

    private readonly static Char[] _brackets = "[]".ToCharArray();

    /// <summary>
    /// Given a T-SQL identifier, return the same identifier with all of its
    /// constituent parts wrapped in square brackets.
    /// </summary>
    public static String GetNormalizedSqlIdentifier(String identifier)
    {
      identifier.Name("identifier").NotNullEmptyOrOnlyWhitespace();

      Func<String, String> wrap = s => s.Any() ? String.Concat("[", s.Trim(_brackets), "]") : "";

      return
        identifier
        /* Keep empty array elements, because parts of a multi-part T-SQL identifier
           can be empty (e.g. "server.database..object", where the schema name is omitted). */
        .Split(".".ToCharArray(), StringSplitOptions.None)
        .Select(element => wrap(element))
        .Join(".");
    }

    public static String GetStrippedSqlIdentifier(String identifier)
    {
      return identifier.Replace("[", "").Replace("]", "");
    }
  }
}
