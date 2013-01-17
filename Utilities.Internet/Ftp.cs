/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Utilities.Internet
{
  public enum FtpMode
  {
    Binary,
    Text
  }

  public class FtpPathException : Exception
  {
    public FtpPathException(String message)
      : base(message)
    {
    }

    public FtpPathException(String message, Exception exception)
      : base(message, exception)
    {
    }
  }

  public class FtpLineFormatException : Exception
  {
    public FtpLineFormatException(String message)
      : base(message)
    {
    }
  }

  public class Ftp
  {
    public FtpMode FtpMode { get; set; }
    public Boolean IsCaseSensitive { get; set; }
    public Boolean ShouldKeepConnectionAlive { get; set; }
    
    private Uri _currentDirectoryUri;
    private readonly Uri _baseUri;
    private readonly String _username;
    private readonly String _password;
    private readonly Char[] _forwardSlashCharacterArray = "/".ToCharArray();

    private Ftp()
      : base()
    {
    }

    public Ftp(String address, Int32 port, String username, String password)
      : this()
    {
      address = address.TrimEnd(this._forwardSlashCharacterArray);

      if (!address.StartsWith("ftp://"))
        address = "ftp://" + address;

      if (port != 0)
        address += ":" + port.ToString();

      this._currentDirectoryUri = new Uri(address);
      this._baseUri = new Uri(this._currentDirectoryUri.GetLeftPart(UriPartial.Authority));
      this._username = username;
      this._password = password;

      this.FtpMode = FtpMode.Binary;
      this.IsCaseSensitive = true;
      this.ShouldKeepConnectionAlive = true;
    }

    public Ftp(String address, String username, String password)
      : this(address, 21, username, password)
    {
    }

    public Ftp(String address, Int32 port)
      : this(address, port, "anonymous", "here@there.com")
    {
    }

    public Ftp(String address)
      : this(address, 21, "anonymous", "here@there.com")
    {
    }

    private FtpWebRequest GetRequest(String method)
    {
      return this.GetRequest(method, this._currentDirectoryUri);
    }

    private FtpWebRequest GetRequest(String method, Uri uri)
    {
      var request = (FtpWebRequest) WebRequest.Create(uri);
      request.Method = method;
      request.Credentials = new NetworkCredential(this._username, this._password);
      request.KeepAlive = this.ShouldKeepConnectionAlive;
      request.UseBinary = (this.FtpMode == FtpMode.Binary);
      request.UsePassive = true;
      return request;
    }

    private void ValidateDirectoryName(String dirName)
    {
      if (String.IsNullOrWhiteSpace(dirName))
        throw new FtpPathException(
          String.Format("'{0}' is not a valid FTP directory name.  A directory name cannot be null or consist only of whitespace.",
            dirName ?? "NULL"));
    }

    private void ValidateFileName(String filename)
    {
      if (String.IsNullOrWhiteSpace(filename))
        throw new FtpPathException(
          String.Format("'{0}' is not a valid FTP file name.  A file name cannot be null or consist only of whitespace.",
            filename ?? "NULL"));
    }

    private Regex _repeatedForwardSlashesRegex = new Regex("/+", RegexOptions.Singleline);

    private String GetNormalizedDirectoryName(String dirName)
    {
      /* A dirName consisting solely of one or more forward slashes is considered 
         to refer to the root directory. */
      if (dirName.Trim().Trim("/".ToCharArray()) == String.Empty)
        return "/";
      else
        return _repeatedForwardSlashesRegex.Replace(dirName + "/", "/").Trim();
    }

    private Regex GetRegex(String regexPattern)
    {
      return new Regex(regexPattern.Trim(), RegexOptions.Singleline | (this.IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase));
    }

    public void ChangeDirectory(String dirName)
    {
      this.ValidateDirectoryName(dirName);
      dirName = this.GetNormalizedDirectoryName(dirName);
      var newDirectoryCandidate = new Uri(this._currentDirectoryUri, dirName);

      if (this.DoesDirectoryExist(newDirectoryCandidate.AbsolutePath))
        this._currentDirectoryUri = newDirectoryCandidate;
      else
        throw new FtpPathException(String.Format("The change directory operation cannot be completed because the directory '{0}' does not exist.", dirName));
    }

    public String GetCurrentDirectory()
    {
      return this._currentDirectoryUri.AbsolutePath;
    }

    private Char[] _forwardSlash = "/".ToCharArray();

    public Boolean DoesDirectoryExist(String directoryName)
    {
      this.ValidateDirectoryName(directoryName);
      directoryName = this.GetNormalizedDirectoryName(directoryName);

      if (directoryName == "/")
        return true;
      else
        try
        {
          /* Path.GetFileName will return an empty string if the string it's passed ends with a forward slash. */
          return this.GetFileList(directoryName + "/..", Path.GetFileName(directoryName.TrimEnd(this._forwardSlash)), FtpListEntryType.Folder).Any();
        }
        catch
        {
          return false;
        }
    }

    public Boolean DoesFileExist(String pathname)
    {
      this.ValidateFileName(pathname);
      pathname = this.GetNormalizedDirectoryName(pathname);

      if (pathname == "/")
        return true;
      else
      {
        try
        {
          pathname = pathname.TrimEnd(this._forwardSlash);
          var directoryName = Path.GetDirectoryName(pathname).Replace(@"\", "/") + "/";

          /* Path.GetFileName will return an empty string if the string it's passed ends with a forward slash. */
          return this.GetFileList(directoryName, Path.GetFileName(pathname), FtpListEntryType.File).Any();
        }
        catch
        {
          return false;
        }
      }
    }

    public void MakeDirectory(String dirName)
    {
      this.ValidateDirectoryName(dirName);
      dirName = this.GetNormalizedDirectoryName(dirName);

      var isRelativeDirectory = (dirName[0] != '/');
      var currentDirectory = (isRelativeDirectory ? String.Empty : "/");
      var rootUri = (isRelativeDirectory ? this._currentDirectoryUri : this._baseUri);
      var directorySegments = dirName.Split(this._forwardSlashCharacterArray, StringSplitOptions.RemoveEmptyEntries);

      foreach (var segment in directorySegments)
      {
        currentDirectory += segment + "/";
        var uri = new Uri(rootUri, currentDirectory);

        if (this.DoesDirectoryExist(uri.AbsolutePath))
          continue;

        var request = this.GetRequest(WebRequestMethods.Ftp.MakeDirectory, uri);
        try
        {
          using (var response = (FtpWebResponse) request.GetResponse())
          {
            /* Nothing to process in the response when creating a directory. */
          }
        }
        catch (Exception ex)
        {
          throw new FtpPathException(String.Format("Unable to create directory '{0}'.  Possible reasons are that the '{1}' account may not have sufficient permissions, or the '{2}' target directory name is invalid.  The error message returned by the FTP server is: {3}.",
            uri.AbsolutePath, this._username, dirName, ex.Message), ex);
        }
      }
    }

    public void DeleteDirectory(String dirName)
    {
      this.ValidateDirectoryName(dirName);
      dirName = this.GetNormalizedDirectoryName(dirName);

      var uri = new Uri(this._currentDirectoryUri, dirName);
      var request = this.GetRequest(WebRequestMethods.Ftp.RemoveDirectory, uri);
      try
      {
        using (var response = (FtpWebResponse) request.GetResponse())
        {
          /* Nothing to process in the response when deleting a directory. */
        }
      }
      catch (Exception ex)
      {
        throw new FtpPathException(String.Format("Unable to delete directory '{0}'.  Possible reasons are that the '{1}' account may not have sufficient permissions, or the '{2}' target directory name is invalid, or the directory is not empty.  The error message returned by the FTP server is: {3}.",
          uri.AbsoluteUri, this._username, dirName, ex.Message), ex);
      }
    }

    public void DeleteContentsOfDirectory(String dirName)
    {
      this.ValidateDirectoryName(dirName);
      dirName = this.GetNormalizedDirectoryName(dirName);

      var listEntries = this.GetFileList(dirName, ".*");

      foreach (var directory in listEntries.Where(listEntry => listEntry.EntryType == FtpListEntryType.Folder))
      {
        /* The calls to Thread.Sleep seems to be necessary for some FTP servers.  My only guess is the
           affected servers process the delete operations in some kind of queue, and the pause is needed
           to give the server time to process the queue. */

        this.DeleteContentsOfDirectory(directory.FullName);
        Thread.Sleep(10);

        this.DeleteDirectory(directory.FullName);
        Thread.Sleep(10);
      }

      foreach (var file in listEntries.Where(listEntry => listEntry.EntryType == FtpListEntryType.File))
      {
        this.DeleteFile(file.FullName);
        Thread.Sleep(10);
      }
    }

    public void DeleteFile(String filename)
    {
      this.ValidateFileName(filename);

      var uri = new Uri(this._currentDirectoryUri, filename.Trim());
      var request = this.GetRequest(WebRequestMethods.Ftp.DeleteFile, uri);
      try
      {
        using (var response = (FtpWebResponse) request.GetResponse())
        {
          /* Nothing to process in the response when deleting a file. */
        }
      }
      catch (Exception ex)
      {
        throw new FtpPathException(String.Format("Unable to delete file '{0}'.  Possible reasons are that the {1} account may not have sufficient permissions, or the '{2}' target file name is invalid.  The error message returned by the FTP server is: {3}.",
          uri.AbsoluteUri, this._username, filename, ex.Message), ex);
      }
    }

    public void UploadFile(String sourcePath, String destinationFilename)
    {
      using (var sourceStream = File.OpenRead(sourcePath))
        this.UploadStream(sourceStream, destinationFilename);
    }

    public void UploadString(String s, String destinationFilename)
    {
      using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(s)))
        this.UploadStream(sourceStream, destinationFilename);
    }

    public void UploadStream(Stream sourceStream, String destinationFilename)
    {
      this.ValidateFileName(destinationFilename);

      using (var webClient = new WebClient())
      {
        webClient.Credentials = new NetworkCredential(this._username, this._password);
        var destinationUri = new Uri(this._currentDirectoryUri, destinationFilename);
        using (var destinationStream = webClient.OpenWrite(destinationUri))
          sourceStream.CopyTo(destinationStream);
      }
    }

    public String DownloadToString(String sourcePath)
    {
      using (var destinationStream = new MemoryStream())
      {
        this.DownloadToStream(sourcePath, destinationStream);
        destinationStream.Position = 0;
        using (var streamReader = new StreamReader(destinationStream))
          return streamReader.ReadToEnd();
      }
    }

    public void DownloadToFile(String sourcePath, String destinationPath)
    {
      Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
      using (var destinationStream = File.OpenWrite(destinationPath))
        this.DownloadToStream(sourcePath, destinationStream);
    }

    public void DownloadToStream(String sourcePath, Stream destinationStream)
    {
      using (var webClient = new WebClient())
      {
        webClient.Credentials = new NetworkCredential(this._username, this._password);
        var sourceUri = new Uri(this._currentDirectoryUri, sourcePath);
        using (var sourceStream = webClient.OpenRead(sourceUri))
          sourceStream.CopyTo(destinationStream);
      }
    }

    public List<String> GetRawFileList()
    {
      return this.GetRawFileList(this.GetCurrentDirectory());
    }

    public List<String> GetRawFileList(String dirName)
    {
      var request = this.GetRequest(WebRequestMethods.Ftp.ListDirectoryDetails, new Uri(this._baseUri, this.GetNormalizedDirectoryName(dirName)));

      using (var response = (FtpWebResponse) request.GetResponse())
      {
        using (var reader = new StreamReader(response.GetResponseStream()))
        {
          var filenames = new List<String>();

          String line;
          while ((line = reader.ReadLine()) != null)
          {
            line = line.Trim();
            if (line != String.Empty)
              filenames.Add(line);
          }

          return filenames;
        }
      }
    }

    public List<FtpListEntry> GetFileList()
    {
      return this.GetFileList(this.GetRegex(".*"), FtpListEntryType.All);
    }

    public List<FtpListEntry> GetFileList(String regexPattern)
    {
      return this.GetFileList(this.GetRegex(regexPattern), FtpListEntryType.All);
    }

    public List<FtpListEntry> GetFileList(String regexPattern, FtpListEntryType listEntryType)
    {
      return this.GetFileList(this.GetRegex(regexPattern), listEntryType);
    }

    public List<FtpListEntry> GetFileList(Regex regex)
    {
      return this.GetFileList(regex, FtpListEntryType.All);
    }

    public List<FtpListEntry> GetFileList(Regex regex, FtpListEntryType listEntryType)
    {
      return this.GetFileList(this.GetCurrentDirectory(), regex, listEntryType);
    }

    public List<FtpListEntry> GetFileList(String dirName, String regexPattern)
    {
      return this.GetFileList(dirName, this.GetRegex(regexPattern), FtpListEntryType.All);
    }

    public List<FtpListEntry> GetFileList(String dirName, String regexPattern, FtpListEntryType listEntryType)
    {
      return this.GetFileList(dirName, this.GetRegex(regexPattern), listEntryType);
    }

    public List<FtpListEntry> GetFileList(String dirName, Regex regex)
    {
      return this.GetFileList(dirName, regex, FtpListEntryType.All);
    }

    public List<FtpListEntry> GetFileList(String dirName, Regex regex, FtpListEntryType listEntryType)
    {
      var regexPattern = regex.ToString().Trim();
      var isGlobalRegex = ((regexPattern == ".*") || (regexPattern == ".*?"));

      return
        this.GetRawFileList(dirName)
        .ConvertAll<FtpListEntry>(line => new FtpListEntry(dirName, line))
        .Where(
          listEntry =>
            (listEntry != null) &&
            ((listEntry.EntryType & listEntryType) > 0) &&
            (isGlobalRegex || regex.IsMatch(listEntry.Name)))
        .OrderBy(listEntry => listEntry.Name)
        .ToList();
    }
  }
}
