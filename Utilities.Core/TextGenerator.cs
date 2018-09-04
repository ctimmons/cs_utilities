/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Utilities.Core
{
  public class TextGenerator
  {
    private readonly StringBuilder _content = new StringBuilder();
    private readonly Stack<String> _indents = new Stack<String>();
    private String _currentIndentString = "";

    public String Content => this._content.ToString();

    /* Memoize _currentIndentString, rather than
       calculating it for every line of text. */
    private void RefreshCurrentIndentString() => this._currentIndentString = String.Join("", this._indents.ToArray().Reverse());

    public TextGenerator ClearIndent() { this._indents.Clear(); this.RefreshCurrentIndentString(); return this; }

    public TextGenerator PushIndent(Int32 numberOfSpaces) { this.PushIndent(" ".Repeat(numberOfSpaces)); return this; }

    public TextGenerator PushIndent(String indent) { this._indents.Push(indent); this.RefreshCurrentIndentString(); return this; }

    public TextGenerator PopIndent()
    {
      /* Pop() throws an exception if the stack is empty.
         Don't want that behavior.  Only pop if there's something
         on the stack.  In othe words, popping the indent from 
         an empty stack is a no-op. */
      if (this._indents.Any())
      {
        this._indents.Pop();
        this.RefreshCurrentIndentString();
      }

      return this;
    }

    private static readonly Regex _indentTextRegex = new Regex("(\r\n|\n)");

    private String GetIndentedText(String text) => this._currentIndentString + _indentTextRegex.Replace(text, "$1" + this._currentIndentString);

    public TextGenerator Write(String text) { this._content.Append(GetIndentedText(text)); return this; }

    public TextGenerator WriteLine(String text) { this._content.AppendLine(GetIndentedText(text)); return this; }

    public TextGenerator ClearContent() { this._content.Clear(); return this; }

    public TextGenerator SaveToFile(String filename) { File.WriteAllText(filename, this.Content); return this; }
  }
}
