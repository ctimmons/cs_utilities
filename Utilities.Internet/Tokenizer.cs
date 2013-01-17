/* See UNLICENSE.txt file for license details. */

using System;

namespace Utilities.Internet
{
  internal class Tokenizer
  {
    public Int32 Position { get; set; }

    private String _text;

    private Tokenizer()
      : base()
    {
      this.ResetPosition();
    }

    public Tokenizer(String text)
      : this()
    {
      this._text = text;
    }

    public String GetToken()
    {
      var result = String.Empty;
      while (!Char.IsWhiteSpace(this._text[this.Position]) && !this.IsEndOfLine())
        result += this._text[this.Position++].ToString();
      this.GetWhitespace();
      return result;
    }

    private void GetWhitespace()
    {
      while (Char.IsWhiteSpace(this._text[this.Position]) && !this.IsEndOfLine())
        this.Position++;
    }

    public void SkipTokens(Int32 numberToSkip)
    {
      for (Int32 i = 0; i < numberToSkip; i++)
        this.GetToken();
    }

    public Int64 GetInt64()
    {
      return Convert.ToInt64(this.GetToken());
    }

    public Boolean IsEndOfLine()
    {
      return (this.Position == (this._text.Length - 1));
    }

    public String GetToEndOfLine()
    {
      var result = this._text.Substring(this.Position, this._text.Length - this.Position);
      this.Position = (this._text.Length - 1);
      return result;
    }

    public void ResetPosition()
    {
      this.Position = 0;
    }
  }
}
