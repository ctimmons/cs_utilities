/* See UNLICENSE.txt file for license details. */

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Utilities.Core;

namespace Utilities.Internet
{
  [Flags]
  public enum FtpListEntryType
  {
    Unknown = 0,
    File = 1,
    Folder = 2,
    Link = 4,
    All = File | Folder | Link
  }

  public class FtpListEntry
  {
    public FtpListEntryType EntryType { get; private set; }
    public Int64 Size { get; private set; }
    public String Name { get; private set; }
    public String FullName { get; private set; }
    public String FileExtension { get; private set; }
    public String LinkDestination { get; private set; }
    public String Owner { get; private set; }
    public String Group { get; private set; }
    public DateTime Date { get; private set; }
    public String RawData { get; private set; }

    private enum LineFormatType
    {
      Unknown,
      Unix,
      MSDOS
    }

    private static String[] _linkSeparator = new String[] { "->" };

    private FtpListEntry()
      : base()
    {
    }

    public FtpListEntry(String dirName, String line)
      : this()
    {
      if (String.IsNullOrWhiteSpace(line))
        throw new ArgumentException("Cannot construct an FTP list entry from a null or blank line.");

      switch (this.GetLineFormatType(line))
      {
        case LineFormatType.Unix:
          this.ParseAsUnixFormat(line);
          break;

        case LineFormatType.MSDOS:
          this.ParseAsMSDOSFormat(line);
          break;

        case LineFormatType.Unknown:
        default:
          throw new FtpLineFormatException(String.Format("The line '{0}' does not appear to have either a Unix/Linux or MS-DOS format.", line));
      }

      if ((this.EntryType == FtpListEntryType.File) || (this.EntryType == FtpListEntryType.Link))
        this.FileExtension = Path.GetExtension(this.Name);
      else
        this.FileExtension = String.Empty;

      this.FullName = dirName.AddTrailingForwardSlash() + this.Name;
      this.RawData = line;
    }

    private void ParseAsUnixFormat(String line)
    {
      var tokenizer = new Tokenizer(line);

      var entryType = tokenizer.GetToken()[0];
      if (entryType == 'd')
        this.EntryType = FtpListEntryType.Folder;
      else if (entryType == '-')
        this.EntryType = FtpListEntryType.File;
      else if (entryType == 'l')
        this.EntryType = FtpListEntryType.Link;
      else
        this.EntryType = FtpListEntryType.File;

      tokenizer.SkipTokens(1);
      this.Owner = tokenizer.GetToken();
      this.Group = tokenizer.GetToken();
      this.Size = tokenizer.GetInt64();

      /* Unix "ls" date formats are screwy.
      
         If the date is within the previous six months, no year is given and the date's format is "mmm dd hh:nn".
         If the date is older than six months, no time is given and the date's format is "mmm dd yyyy".
      
         Converting the first kind of time (no year given) to a full DateTime is problematic if
         January 1 falls within that six month period.  For example, if the ls entry's date is
         "Dec 12 13:30", and today is January 2, 2009, that means the entry's date is in 2008.
      
         Rather than go thru the effort of checking whether or not January 1 has been crossed,
         a simple check is done to see if the resulting date is later than today.  If it is,
         a year is subtracted. To use the example above, if "Dec 12 13:30" is converted to
         "Dec 12 2009", that is later than January 2, 2009, so one year is subtracted to
         give the correct date of December 12, 2008. */

      var month = tokenizer.GetToken();
      var day = tokenizer.GetToken();
      var yearOrTime = tokenizer.GetToken();

      if (yearOrTime.Contains(':'))
        this.Date = DateTime.Parse(String.Format("{0} {1} {2} {3}", month, day, DateTime.Now.Year.ToString(), yearOrTime));
      else
        this.Date = DateTime.Parse(String.Format("{0} {1} {2}", month, day, yearOrTime));

      if (this.Date > DateTime.Today)
        this.Date = this.Date.AddYears(-1);

      var fileAndOrLink = tokenizer.GetToEndOfLine();
      if (this.EntryType == FtpListEntryType.Link)
      {
        var fileAndLink = fileAndOrLink.Split(_linkSeparator, StringSplitOptions.RemoveEmptyEntries);
        this.Name = fileAndLink[0].Trim();
        this.LinkDestination = fileAndLink[1].Trim();
      }
      else // directory or file.
      {
        this.Name = fileAndOrLink;
        this.LinkDestination = String.Empty;
      }
    }

    private void ParseAsMSDOSFormat(String line)
    {
      var tokenizer = new Tokenizer(line);

      this.Date = DateTime.Parse(tokenizer.GetToken() + " " + tokenizer.GetToken());

      /* There's no way to tell if an entry is a link in MS-DOS format.
         Shortcuts appear as normal files. */

      var sizeOrDir = tokenizer.GetToken();
      if (sizeOrDir == "<DIR>")
      {
        this.EntryType = FtpListEntryType.Folder;
        this.Size = 0;
      }
      else
      {
        this.EntryType = FtpListEntryType.File;
        this.Size = Int64.Parse(sizeOrDir);
      }

      this.Name = tokenizer.GetToEndOfLine();
    }

    private Regex _unixLineFormatRegex = new Regex(".((r|-)(w|-)(x|-)){3}", RegexOptions.Singleline);
    private Regex _msdosLineFormatRegex = new Regex(@"\d{2}-\d{2}-\d{2,4}", RegexOptions.Singleline);

    private LineFormatType GetLineFormatType(String line)
    {
      /* The line's first element will either be a block of Unix permissions, or an MS-DOS date. */
      var firstElement = (new Tokenizer(line)).GetToken();

      if (this._unixLineFormatRegex.IsMatch(firstElement))
        return LineFormatType.Unix;
      else if (this._msdosLineFormatRegex.IsMatch(firstElement))
        return LineFormatType.MSDOS;
      else
        return LineFormatType.Unknown;
    }
  }
}
