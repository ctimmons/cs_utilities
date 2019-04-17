/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Utilities.Core
{
  /// <summary>
  /// This class is a cousin to the abstract <a href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.texttemplating.texttransformation">Microsoft.VisualStudio.TextTemplating.TextTransformation</a> class.
  /// <para><c>Microsoft's TextTranformation</c> is only used with T4 templates.  <c>TextGenerator</c> provides similar functionality thru methods that allow the construction
  /// of multiline blocks of text with varying levels of indentation.</para>
  /// <example>
  /// <code>
  /// 
  /// </code>
  /// </example>
  /// </summary>
  public class TextGenerator
  {
    private readonly StringBuilder _content = new StringBuilder();
    private readonly Stack<String> _indents = new Stack<String>();
    private String _standardIndentString = "";
    private String _currentIndentString = "";

    public TextGenerator()
      : base()
    {
    }

    public TextGenerator(String content)
      : this()
    {
      this._content.Append(content);
    }

    public String Content => this._content.ToString();

    public TextGenerator SetStandardIndentString(Int32 numberOfSpaces) { this._standardIndentString = " ".Repeat(numberOfSpaces); return this; }

    public TextGenerator SetStandardIndentString(String indent) { this._standardIndentString = indent; return this; }

    public TextGenerator ClearStandardIndentString() { this._standardIndentString = ""; return this; }

    /* Memoize _currentIndentString, rather than
       calculating it for every line of text. */
    private void RememberCurrentIndentString() => this._currentIndentString = this._indents.ToArray().Reverse().Join("");

    public TextGenerator ClearIndent() { this._indents.Clear(); this.RememberCurrentIndentString(); return this; }

    public TextGenerator PushIndent() { return this.PushIndent(this._standardIndentString); }

    public TextGenerator PushIndent(Int32 numberOfSpaces) { this.PushIndent(" ".Repeat(numberOfSpaces)); return this; }

    public TextGenerator PushIndent(String indent) { this._indents.Push(indent); this.RememberCurrentIndentString(); return this; }

    public TextGenerator PopIndent()
    {
      /* Pop() throws an exception if the stack is empty.
         Don't want that behavior.  Only pop if there's something
         on the stack.  In other words, popping the indent from 
         an empty stack is a no-op. */
      if (this._indents.Any())
      {
        this._indents.Pop();
        this.RememberCurrentIndentString();
      }

      return this;
    }

    public TextGenerator PushLine(String text) { this.PushIndent().WriteLine(text); return this; }

    public TextGenerator PushLineThenPop(String text) { this.PushLine(text).PopIndent(); return this; }

    public TextGenerator PopLine(String text) { this.PopIndent().WriteLine(text); return this; }

    private static readonly Regex _indentTextRegex = new Regex("(\r\n|\n)");

    private String GetIndentedText(String text) => this._currentIndentString + _indentTextRegex.Replace(text, "$1" + this._currentIndentString);

    public TextGenerator Write(String text) { this._content.Append(GetIndentedText(text)); return this; }

    public TextGenerator WriteLine(String text) { this._content.AppendLine(GetIndentedText(text)); return this; }

    public TextGenerator ClearContent() { this._content.Clear(); return this; }

    public TextGenerator SaveToFile(String filename) { File.WriteAllText(filename, this.Content); return this; }
  }
}