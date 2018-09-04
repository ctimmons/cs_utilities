/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Utilities.Core
{
  public class TextGenerator
  {
    private readonly StringBuilder _content = new StringBuilder();

    public String Content => this._content.ToString();

    public String Indent { get; private set;  }

    public Int32 IndentBy { get; set; }

    public TextGenerator PushIndent() => this.PushIndent(this.IndentBy);
    public TextGenerator PushIndent(Int32 numberOfSpaces) { this.Indent += " ".Repeat(numberOfSpaces); return this; }

    public TextGenerator PopIndent() => this.PopIndent(this.IndentBy);
    public TextGenerator PopIndent(Int32 numberOfSpaces) { this.Indent = " ".Repeat(Math.Max(0, this.Indent.Length - numberOfSpaces)); return this; }

    private static readonly Regex _indentTextRegex = new Regex("(\r\n|\n)", RegexOptions.Multiline);

    public String GetIndentedText(String text) => this.GetIndentedText(text, this.Indent);

    public String GetIndentedText(String text, Int32 indent) => this.GetIndentedText(text, " ".Repeat(indent));

    public String GetIndentedText(String text, String indentString) => indentString + _indentTextRegex.Replace(text, "$1" + indentString);

    public TextGenerator Write(String text) { this._content.Append(GetIndentedText(text)); return this; }

    public TextGenerator WriteLine(String text) { this._content.AppendLine(GetIndentedText(text)); return this; }

    public TextGenerator Clear() { this._content.Clear(); return this; }

    public TextGenerator SaveToFile(String filename) { File.WriteAllText(filename, this.Content); return this; }
  }
}
