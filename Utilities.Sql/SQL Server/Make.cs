/* See UNLICENSE.txt file for license details. */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Utilities.Core;

namespace Utilities.Sql.SqlServer
{
  /* See "Make - Description.txt" for details on this algorithm and how to use it. */

  public class LogEventArgs : EventArgs
  {
    private String _message;
    private Boolean _isJitOptimized = Assembly.GetExecutingAssembly().IsJITOptimized();

    public LogEventArgs(String message, Int32 stackDepth = 2)
    {
      var stackFrame = new StackFrame(stackDepth, true /* Get the file name, line number, and column number of the stack frame. */);
      var methodBase = stackFrame.GetMethod();
      var lineNumber = stackFrame.GetFileLineNumber();

      this._message = String.Format("{0}.{1}{2} - {3}",
        methodBase.DeclaringType.FullName,
        methodBase.Name,
        /* In JIT optimized builds, lineNumber will either be 0 or wildly inaccurate.
           In non-optimized builds, lineNumber will be non-zero and accurate. */
        this._isJitOptimized ? "" : " - Line " + lineNumber.ToString(),
        message);
    }

    public virtual String GetMessage()
    {
      return this._message;
    }
  }

  public class MakeErrorEventArgs : EventArgs
  {
    private Exception _exception;

    public MakeErrorEventArgs(Exception exception)
    {
      this._exception = exception;
    }

    public virtual Exception GetException()
    {
      return this._exception;
    }
  }

  public class Make
  {
    public SqlConnection Connection { get; private set; }
    public String DefaultSchema { get; set; }

    private Boolean _didErrorOccur = false;
    private readonly Dictionary<String, Dictionary<String, Item>> _items = new Dictionary<String, Dictionary<String, Item>>();
    private readonly String _itemsFolder;

    /* A pathname is an absolute path, i.e. a folder plus filename (e.g. c:\temp\myfile.txt). */
    private readonly List<String> _pathnames = new List<String>();

    private IEnumerable<Item> _allItems
    {
      get
      {
        return this._items.SelectMany(kvp => kvp.Value.Values.Select(i => i));
      }
    }

    public event EventHandler<MakeErrorEventArgs> ErrorEvent;

    protected virtual void RaiseErrorEvent(String format, params Object[] args)
    {
      this.RaiseErrorEvent(new Exception(String.Format(format, args)));
    }

    protected virtual void RaiseErrorEvent(Exception ex)
    {
      this._didErrorOccur = true;
      (new MakeErrorEventArgs(ex)).Raise(this, ref ErrorEvent);
    }

    public event EventHandler<LogEventArgs> LogEvent;

    protected virtual void RaiseLogEvent(String format, params Object[] args)
    {
      this.RaiseLogEvent(String.Format(format, args), 3);
    }

    protected virtual void RaiseLogEvent(String message, Int32 stackDepth = 2)
    {
      (new LogEventArgs(message, stackDepth)).Raise(this, ref LogEvent);
    }

    private Make()
      : base()
    {
    }

    public Make(SqlConnection connection)
      : this()
    {
      /* Don't check to see if the connection is open - yet.
         The caller might not want to open the connection until just
         before the Run() method is called. */
      connection.Name("connection").NotNull();

      this.DefaultSchema = "dbo";
      this.Connection = connection;

      this._itemsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SQL Server Make Item Files");
      Directory.CreateDirectory(this._itemsFolder);
    }

    public void AddFile(String pathname)
    {
      pathname.Name("pathname").NotNullEmptyOrOnlyWhitespace().FileExists();

      this._pathnames.Add(Path.GetFullPath(pathname));
    }

    public void AddFiles(params String[] pathnames)
    {
      pathnames.Name("pathnames").NotNull();

      foreach (var pathname in pathnames)
        this.AddFile(pathname);
    }

    public void AddFiles(IEnumerable<String> pathnames)
    {
      pathnames.Name("pathnames").NotNull();

      foreach (var pathname in pathnames)
        this.AddFile(pathname);
    }

    public void AddFolder(String folder, String filemask = "*.*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
      folder.Name("folder").NotNullEmptyOrOnlyWhitespace().DirectoryExists();
      filemask.Name("filemask").NotNullEmptyOrOnlyWhitespace();

      filemask = filemask.Trim();

      var pathnames = Directory.EnumerateFiles(folder, "*", searchOption);

      if ((filemask == "*") || (filemask == "*.*"))
      {
        this.AddFiles(pathnames);
      }
      else
      {
        /* Directory.EnumerateFiles()'s searchPattern parameter has an odd behavior
           when matching pathname extensions.
           (Read the "Remarks" section at https://msdn.microsoft.com/en-us/library/dd413233%28v=vs.110%29.aspx).
           That odd (IMO buggy) behavior is avoided by converting the filemask to an equivalent
           regular expression. */

        var filemaskRegexPattern =
          filemask
          /* Escape all regex-related characters except '*' and '?'. */
          .RegexEscape('*', '?')
          /* Convert '*' and '?' to their regex equivalents. */
          .Replace('?', '.')
          .Replace("*", ".*?");
        var _saneFilemaskRegex = new Regex(filemaskRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        this.AddFiles(pathnames.Where(f => _saneFilemaskRegex.IsMatch(Path.GetFileName(f))));
      }
    }

    private void Sequence(params Action[] actions)
    {
      foreach (var action in actions)
      {
        if (this._didErrorOccur)
          return;

        try
        {
          action();
        }
        catch (Exception ex)
        {
          this.RaiseErrorEvent(ex);
        }
      }
    }

    public void Run()
    {
      this.Connection.Name("this.Connection").IsOpen();

      this.Sequence(
        this.LoadItems,
        this.SynchronizeItemsWithSourceFiles,
        this.UpdateItemsWithServerData,
        this.DropDependencies,
        this.SaveItems,
        this.CompileItems);
    }

    private void LoadItems()
    {
      var folders = this._pathnames.Select(pathname => Path.GetDirectoryName(pathname)).Distinct();

      foreach (var folder in folders)
      {
        var pathname = Path.Combine(this._itemsFolder, StringUtils.MD5Checksum(folder));

        if (File.Exists(pathname))
        {
          this._items.Add(folder, XmlUtils.DeserializeObjectFromBinaryFile<Dictionary<String, Item>>(pathname));
          this.RaiseLogEvent(Properties.Resources.Make_ItemsLoaded, pathname);
        }
        else
        {
          this._items.Add(folder, new Dictionary<String, Item>());
          this.RaiseLogEvent(Properties.Resources.Make_ItemsDoNotExist, pathname);
        }
      }
    }

    public void SynchronizeItemsWithSourceFiles()
    {
      foreach (var pathname in this._pathnames.Distinct())
        this.AddOrUpdateItem(pathname);

      /* Delete items that don't have a corresponding source file.
         
         (Note: The "ToList()" call creates a new copy of item references.
         That copy is what's enumerated over, allowing
         the "Remove()" call on the original this._items to succeed.) */
      foreach (var kvp in this._items)
        foreach (var kvp2 in kvp.Value.Where(kvp3 => !File.Exists(kvp3.Value.Pathname)).ToList())
          kvp.Value.Remove(kvp2.Key);
    }

    private void AddOrUpdateItem(String pathname)
    {
      /* Why keep the items in a Dictionary<String, Item>?  Why not a List<Item>?
      
         Adding and updating items requires searching the existing items.
      
         The List.Exists() method has O(N) performance, which is unacceptable.

         The List.BinarySearch() method's performance is O(log N), which is
         acceptable.  However, BinarySearch() requires that an Item instance
         be passed to it, and - in this case - an IComparer as well.
         This would require a new Item instance to be created for every
         search, even if that instance already exists in the list!
      
         The Dictionary.TryGetValue() method's performance approaches O(1).
         A new Item instance only needs to be created when it doesn't yet
         exist in the dictionary. */

      var items = this._items[Path.GetDirectoryName(pathname)];

      Item item;
      if (items.TryGetValue(pathname, out item))
      {
        var currentMD5Hash = FileUtils.GetMD5Checksum(pathname);
        if (item.FileContentsMD5Hash == currentMD5Hash)
        {
          item.NeedsToBeCompiled = false;
        }
        else
        {
          item.NeedsToBeCompiled = true;
          item.FileContentsMD5Hash = currentMD5Hash;
        }

        this.RaiseLogEvent(Properties.Resources.Make_UpdatedExistingItem, pathname);
      }
      else
      {
        items.Add(pathname, new Item(pathname));
        this.RaiseLogEvent(Properties.Resources.Make_CreatedNewItem, pathname);
      }
    }

    private void UpdateItemsWithServerData()
    {
      var sqlTemplate = "Utilities.Sql.SQL_Server.GetUpdatedItems.sql".GetEmbeddedTextResource();

      var insertValues =
        this._allItems
        .Select(s => String.Format("('{0}', '{1}', '', '', {2}, '{3}', 'N', {4}, 'Y', 'N')",
            s.Pathname,
            s.Filename.RemoveFileExtension(),
            (s.Type.Trim() == "") ? "NULL" : "'" + s.Type + "'",
            s.NeedsToBeCompiled.AsYOrN(),
            s.DropOrder))
        .JoinAndIndent("," + Environment.NewLine, 2);

      var sql = String.Format(sqlTemplate, insertValues, this.DefaultSchema);

      this.RaiseLogEvent(insertValues);

      using (var command = new SqlCommand() { Connection = this.Connection, CommandType = CommandType.Text, CommandText = sql })
      {
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            if (reader["does_file_exist"].ToString().AsBoolean())
            {
              var pathname = reader["pathname"].ToString();
              var items = this._items[Path.GetDirectoryName(pathname)];

              Item item;
              if (items.TryGetValue(pathname, out item))
              {
                item.SchemaName = reader["schema_name"].ToString();
                item.ObjectName = reader["object_name"].ToString();
                item.Type = reader["type"].ToString();
                item.NeedsToBeCompiled = reader["needs_to_be_compiled"].ToString().AsBoolean();
                item.IsPresentOnServer = reader["is_present_on_server"].ToString().AsBoolean();
                item.DropOrder = Convert.ToInt32(reader["drop_order"]);
                item.NeedsToBeDropped = reader["needs_to_be_dropped"].ToString().AsBoolean();

                this.RaiseLogEvent(Properties.Resources.Make_UpdatedItemForFile, item.Pathname);
              }
              else
              {
                this.RaiseErrorEvent(Properties.Resources.Make_PathnameNotFound, pathname);
              }
            }
            else
            {
              this.RaiseErrorEvent(Properties.Resources.Make_SourceFileDoesNotExist,
                reader["schema_name"], reader["object_name"], reader["type"]);
            }
          }
        }
      }
    }

    private String GetTypeName(String type)
    {
      switch (type.Trim().ToUpper())
      {
        case "AF":
          return "AGGREGATE";
        case "D":
          return "DEFAULT";
        case "FN":
        case "IF":
        case "TF":
          return "FUNCTION";
        case "P":
          return "PROCEDURE";
        case "R":
          return "RULE";
        case "SN":
          return "SYNONYM";
        case "SO":
          return "SEQUENCE";
        case "TR":
          return "TRIGGER";
        case "TT":
          return "TYPE";
        // Correctly modifying tables is too complex of a problem to tackle right now.
        //case "U":
        //  return "TABLE";
        case "V":
          return "VIEW";
        default:
          throw new ExceptionFmt(Properties.Resources.Make_UnknownType, type);
      }
    }

    private void DropDependencies()
    {
      var itemsToBeDropped =
        this._allItems
        .Where(item => item.NeedsToBeDropped)
        .OrderBy(item => item.DropOrder);

      foreach (var item in itemsToBeDropped)
      {
        var dropSql = String.Format("DROP {0} [{1}].[{2}]", this.GetTypeName(item.Type), item.SchemaName, item.ObjectName);
        this.Connection.ExecuteNonQuery(dropSql);
        this.RaiseLogEvent(Properties.Resources.Make_Executed, dropSql);
      }
    }

    public void SaveItems()
    {
      foreach (var kvp in this._items)
      {
        var pathname = Path.Combine(this._itemsFolder, StringUtils.MD5Checksum(kvp.Key));
        XmlUtils.SerializeObjectToBinaryFile(kvp.Value, pathname);
        this.RaiseLogEvent(Properties.Resources.Make_ItemsSaved, pathname);
      }
    }

    private static readonly Regex _splitAtGoRegex = new Regex(@"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private void CompileItems()
    {
      Func<String, IEnumerable<String>> getParts = f => _splitAtGoRegex.Split(File.ReadAllText(f)).Where(s => s.IsNotEmpty());

      using (var command = new SqlCommand() { Connection = this.Connection, CommandType = CommandType.Text })
      {
        /* User-defined table types (udtts) have a drop order of 1.  All other items have
           a drop order of 0.  When dropping items, non-udtts are dropped first and
           udtts are dropped second.  The reverse is true when compiling, hence the
           OrderByDescending() call. */
        foreach (var item in this._allItems.Where(i => i.NeedsToBeCompiled).OrderByDescending(i => i.DropOrder))
        {
          try
          {
            foreach (var part in getParts(item.Pathname))
            {
              command.CommandText = part;
              command.ExecuteNonQuery();
            }

            this.RaiseLogEvent(Properties.Resources.Make_SuccessfullyCompiled, item.Pathname);
          }
          catch (Exception ex)
          {
            this.RaiseErrorEvent(ex);
            break;
          }
        }
      }
    }
  }

  [Serializable]
  internal class Item
  {
    public String Filename { get; private set; }
    public String Pathname { get; private set; }
    public String SchemaName { get; set; }
    public String ObjectName { get; set; }
    public String FileContentsMD5Hash { get; set; }
    public String Type { get; set; }
    public Boolean NeedsToBeCompiled { get; set; }
    public Boolean IsPresentOnServer { get; set; }
    public Int32 DropOrder { get; set; }
    public Boolean NeedsToBeDropped { get; set; }

    private Item()
      : base()
    {
    }

    private static readonly Regex _userDefinedTableTypeRegex = new Regex(@"create\s+type.*?as\s+table", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public Item(String pathname)
      : this()
    {
      pathname.Name("pathname").NotNullEmptyOrOnlyWhitespace().FileExists();

      var fileContents = File.ReadAllText(pathname);

      this.Filename = Path.GetFileName(pathname);
      this.Pathname = pathname;
      this.SchemaName = "";
      this.ObjectName = "";
      this.FileContentsMD5Hash = fileContents.MD5Checksum();
      this.Type = _userDefinedTableTypeRegex.IsMatch(fileContents) ? "TT" : "  ";
      this.NeedsToBeCompiled = true;
      this.IsPresentOnServer = false;
      this.DropOrder = 0;
      this.NeedsToBeDropped = false;
    }
  }
}
