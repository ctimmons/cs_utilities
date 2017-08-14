/* See the LICENSE.txt file in the root folder for license details. */

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
  /* See "Make - Description.md" for details on this algorithm and how to use it. */

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
    private readonly Dictionary<String, Dictionary<String, MakeItem>> _makeItems = new Dictionary<String, Dictionary<String, MakeItem>>(StringComparer.OrdinalIgnoreCase);
    private readonly String _makeItemsCacheDirectory;
    private readonly List<String> _filenames = new List<String>();

    private IEnumerable<MakeItem> _makeItemsFlatList
    {
      get
      {
        return this._makeItems.SelectMany(kvp => kvp.Value.Values.Select(i => i));
      }
    }

    public event EventHandler<MakeErrorEventArgs> ErrorEvent;

    protected virtual void RaiseErrorEvent(String message, Exception ex)
    {
      this.RaiseErrorEvent(new Exception(message, ex));
    }

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

      this._makeItemsCacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SQL Server Make Item Files");
      Directory.CreateDirectory(this._makeItemsCacheDirectory);
    }

    public void AddFile(String filename)
    {
      filename.Name("filename").NotNullEmptyOrOnlyWhitespace().FileExists();

      this._filenames.Add(Path.GetFullPath(filename));
    }

    public void AddFiles(params String[] filenames)
    {
      filenames.Name("filenames").NotNull();

      this.AddFiles(filenames.ToList());
    }

    public void AddFiles(IEnumerable<String> filenames)
    {
      filenames.Name("filenames").NotNull();

      foreach (var filename in filenames)
        this.AddFile(filename);
    }

    public void AddDirectory(String directory, String filemask = "*.*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
      directory.Name("directory").NotNullEmptyOrOnlyWhitespace().DirectoryExists();
      filemask.Name("filemask").NotNullEmptyOrOnlyWhitespace();

      filemask = filemask.Trim();

      var filenames = Directory.EnumerateFiles(directory, "*", searchOption);

      if ((filemask == "*") || (filemask == "*.*"))
      {
        this.AddFiles(filenames);
      }
      else
      {
        /* Directory.EnumerateFiles()'s searchPattern parameter has an odd behavior
           when matching filename extensions.
           (Read the "Remarks" section at https://msdn.microsoft.com/en-us/library/dd413233%28v=vs.110%29.aspx).
           That odd (IMO buggy) behavior is avoided by converting the filemask to an equivalent
           regular expression. */

        this.AddFiles(filenames.Where(f => filemask.GetRegexFromFilemask().IsMatch(Path.GetFileName(f))));
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
        this.LoadMakeItemsFromCache,
        this.SynchronizeMakeItemsCacheWithSourceFiles,
        this.UpdateMakeItemsWithServerData,
        this.DropDependencies,
        this.SaveMakeItemsToCache,
        this.CompileMakeItems);
    }

    private void LoadMakeItemsFromCache()
    {
      var uniqueDirectoryList = this._filenames.Select(filename => Path.GetDirectoryName(filename)).Distinct();

      foreach (var directory in uniqueDirectoryList)
      {
        var filename = Path.Combine(this._makeItemsCacheDirectory, directory.MD5Checksum());

        if (File.Exists(filename))
        {
          this._makeItems.Add(directory, XmlUtils.DeserializeObjectFromBinaryFile<Dictionary<String, MakeItem>>(filename));
          this.RaiseLogEvent(Properties.Resources.Make_ItemsLoaded, filename);
        }
        else
        {
          this._makeItems.Add(directory, new Dictionary<String, MakeItem>(StringComparer.OrdinalIgnoreCase));
          this.RaiseLogEvent(Properties.Resources.Make_ItemsDoNotExist, filename);
        }
      }
    }

    public void SynchronizeMakeItemsCacheWithSourceFiles()
    {
      foreach (var filename in this._filenames.Distinct())
        this.AddOrUpdateMakeItem(filename);

      /* Delete MakeItems from the cache that no longer have
         a corresponding source file.
         
         (Note: The "ToList()" call creates a new copy of references.
         That copy is what's enumerated over, allowing
         the "Remove()" call on the original this._makeItems to succeed.) */
      foreach (var kvp in this._makeItems)
        foreach (var kvp2 in kvp.Value.Where(kvp3 => !File.Exists(kvp3.Value.FullFilename)).ToList())
          kvp.Value.Remove(kvp2.Key);
    }

    private void AddOrUpdateMakeItem(String filename)
    {
      /* Why keep the MakeItems in a Dictionary<String, MakeItem>?  Why not a List<MakeItem>?
      
         Adding and updating items requires searching the existing items.
      
         The List.Exists() method has O(N) performance, which is unacceptable.

         The List.BinarySearch() method's performance is O(log N), which is
         acceptable.  However, BinarySearch() requires that an Item instance
         be passed to it, and - in this case - an IComparer as well.
         This would require a new MakeItem instance to be created for every
         search, even if that instance already exists in the list!
      
         The Dictionary.TryGetValue() method's performance approaches O(1).
         A new MakeItem instance only needs to be created when it doesn't yet
         exist in the dictionary. */

      var makeItems = this._makeItems[Path.GetDirectoryName(filename)];

      if (makeItems.TryGetValue(filename, out MakeItem makeItem))
      {
        var currentMD5Hash = FileUtils.GetMD5Checksum(filename);
        if (makeItem.FileContentsMD5Hash == currentMD5Hash)
        {
          makeItem.NeedsToBeCompiled = false;
          this.RaiseLogEvent(Properties.Resources.Make_UpdatedExistingItem_DoesNotNeedToBeCompiled, filename);
        }
        else
        {
          makeItem.NeedsToBeCompiled = true;
          makeItem.FileContentsMD5Hash = currentMD5Hash;
          this.RaiseLogEvent(Properties.Resources.Make_UpdatedExistingItem_NeedsToBeCompiled, filename);
        }
      }
      else
      {
        makeItems.Add(filename, new MakeItem(filename));
        this.RaiseLogEvent(Properties.Resources.Make_CreatedNewItem, filename);
      }
    }

    private void UpdateMakeItemsWithServerData()
    {
      var sqlTemplate = "Utilities.Sql.SQL_Server.GetUpdatedItems.sql".GetEmbeddedTextResource();

      var insertValues =
        this._makeItemsFlatList
        .Select(s => String.Format("('{0}', '{1}', '', '', {2}, '{3}', 'N', {4}, 'Y', 'N')",
            s.FullFilename,
            s.Filename.RemoveFileExtension(),
            (s.Type.Trim() == "") ? "NULL" : s.Type.SingleQuote(),
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
              var filename = reader["full_filename"].ToString();
              var makeItems = this._makeItems[Path.GetDirectoryName(filename)];

              if (makeItems.TryGetValue(filename, out MakeItem makeItem))
              {
                makeItem.SchemaName = reader["schema_name"].ToString();
                makeItem.ObjectName = reader["object_name"].ToString();
                makeItem.Type = reader["type"].ToString();
                makeItem.NeedsToBeCompiled = reader["needs_to_be_compiled"].ToString().AsBoolean();
                makeItem.IsPresentOnServer = reader["is_present_on_server"].ToString().AsBoolean();
                makeItem.DropOrder = Convert.ToInt32(reader["drop_order"]);
                makeItem.NeedsToBeDropped = reader["needs_to_be_dropped"].ToString().AsBoolean();

                this.RaiseLogEvent(Properties.Resources.Make_UpdatedItemForFile, makeItem.FullFilename);
              }
              else
              {
                this.RaiseErrorEvent(Properties.Resources.Make_PathnameNotFound, filename);
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
      var dropSqlCommands =
        this._makeItemsFlatList
        .Where(item => item.NeedsToBeDropped)
        .OrderBy(item => item.DropOrder)
        .Select(item => String.Format("DROP {0} [{1}].[{2}]", this.GetTypeName(item.Type), item.SchemaName, item.ObjectName))
        .ToList(); /* Force GetTypeName() to be evaluated before this list is enumerated. */

      foreach (var dropSql in dropSqlCommands)
      {
        this.Connection.ExecuteNonQuery(dropSql);
        this.RaiseLogEvent(Properties.Resources.Make_Executed, dropSql);
      }
    }

    public void SaveMakeItemsToCache()
    {
      foreach (var kvp in this._makeItems)
      {
        var filename = Path.Combine(this._makeItemsCacheDirectory, kvp.Key.MD5Checksum());
        /* Any class that implements IDictionary cannot be (easily) serialized to XML.
           In this case, it's easier to serialize to a binary format instead.
           See this blog post for more ways to serialize/deserialize an
           IDictionary to/from XML:
           http://theburningmonk.com/2010/05/net-tips-xml-serialize-or-deserialize-dictionary-in-csharp/ */
        XmlUtils.SerializeObjectToBinaryFile(kvp.Value, filename);
        this.RaiseLogEvent(Properties.Resources.Make_ItemsSaved, filename);
      }
    }

    private static readonly Regex _splitAtGoRegex = new Regex(@"^\s*GO\s*?.*?$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private void CompileMakeItems()
    {
      Func<String, IEnumerable<String>> getParts = f => _splitAtGoRegex.Split(File.ReadAllText(f)).Where(s => s.IsNotEmpty());

      using (var command = new SqlCommand() { Connection = this.Connection, CommandType = CommandType.Text })
      {
        /* User-defined table types (udtts) have a drop order of 1.  All other items have
           a drop order of 0.  When dropping items, non-udtts are dropped first and
           udtts are dropped second.  The reverse is true when compiling, hence the
           OrderByDescending() call. */
        foreach (var item in this._makeItemsFlatList.Where(i => i.NeedsToBeCompiled).OrderByDescending(i => i.DropOrder))
        {
          try
          {
            foreach (var part in getParts(item.FullFilename))
            {
              command.CommandText = part;
              command.ExecuteNonQuery();
            }

            this.RaiseLogEvent(Properties.Resources.Make_SuccessfullyCompiled, item.FullFilename);
          }
          catch (Exception ex)
          {
            this.RaiseErrorEvent(item.FullFilename, ex);
            break;
          }
        }
      }
    }
  }

  [Serializable]
  internal class MakeItem
  {
    public String Filename { get; private set; }
    public String FullFilename { get; private set; }
    public String SchemaName { get; set; }
    public String ObjectName { get; set; }
    public String FileContentsMD5Hash { get; set; }
    public String Type { get; set; }
    public Boolean NeedsToBeCompiled { get; set; }
    public Boolean IsPresentOnServer { get; set; }
    public Int32 DropOrder { get; set; }
    public Boolean NeedsToBeDropped { get; set; }

    private MakeItem()
      : base()
    {
    }

    private static readonly Regex _userDefinedTableTypeRegex = new Regex(@"create\s+type.*?as\s+table", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public MakeItem(String fullFilename)
      : this()
    {
      fullFilename.Name("fullFilename").NotNullEmptyOrOnlyWhitespace().FileExists();

      var fileContents = File.ReadAllText(fullFilename);

      this.Filename = Path.GetFileName(fullFilename);
      this.FullFilename = fullFilename;
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
