/* See the LICENSE.txt file in the root folder for license details. */

/* NOTE:
   
   The make algorithm exercised by these tests generates a log file.
   The log file is stored in a temporary folder that will be deleted after the tests
   are finished.
   
   If you want to save the log file for later viewing, go to the Cleanup() method
   and uncomment the block of code that moves the log file to a destinationFilename of
   your choosing. */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;

using NUnit.Framework;

using Utilities.Core;

namespace Utilities.Sql.SqlServer.Tests
{
  [TestFixture]
  public class MakeTests
  {
    /* The tests are executed serially, not in parallel,
       so just use one static SqlCommand rather than
       creating/destroying an SqlCommand for each test. */
    private static SqlCommand _command = new SqlCommand();

    private static SqlConnection _connection = new SqlConnection("Server=(local);Database=master;Trusted_Connection=True;");
    private static String _databaseName;

    private static String _tempFolder;
    private static String _independentSpFilename;
    private static String _independentSpObjectName;
    private static String _dependentSpFilename;
    private static String _dependentSpObjectName;
    private static String _udttFilename;
    private static String _udttObjectName;
    private static String _logFilename;

    private static StreamWriter _streamWriter;
    private static Log _log;

    private static readonly List<String> _emptyStringList = new List<String>();

    [OneTimeSetUp]
    public void Init()
    {
      _tempFolder = Path.Combine(Path.GetTempPath(), GetTempName());
      Directory.CreateDirectory(_tempFolder);

      _logFilename = Path.Combine(_tempFolder, "log.txt");
      _streamWriter = new StreamWriter(_logFilename); /* Overwrites existing logFilename. */
      _log = new Log(_streamWriter);

      _independentSpObjectName = "independent_sp";
      _independentSpFilename = Path.Combine(_tempFolder, _independentSpObjectName + ".sql");
      _dependentSpObjectName = "dependent_sp";
      _dependentSpFilename = Path.Combine(_tempFolder, _dependentSpObjectName + ".sql");
      _udttObjectName = "udtt";
      _udttFilename = Path.Combine(_tempFolder, _udttObjectName + ".sql");

      _command.Connection = _connection;

      _databaseName = GetTempName();
      _connection.Open();
      _connection.ExecuteNonQuery($"CREATE DATABASE [{_databaseName}];");
      _connection.ChangeDatabase(_databaseName);
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
      _streamWriter.Close();

      /* Can't drop a database if the connection is using it.
         Every MS SQL Server instance has a 'master' database, so
         switching to it should always work. */
      _connection.ChangeDatabase("master");
      _connection.ExecuteNonQuery($"DROP DATABASE [{_databaseName}];");
      _connection.Close();

      /* Uncomment the code below to save the log file
         for later viewing. */

      /*
      var destinationFilename = @"c:\temp\log.txt";
      Directory.CreateDirectory(Path.GetDirectoryName(destinationFilename));
      File.Delete(destinationFilename);
      File.Copy(_logFilename, destinationFilename);
      */

      Directory.Delete(_tempFolder, true /* Recursive delete. */);
    }

    /* Luckily the random eight character string returned by this simple code
       should always meet the criteria for a valid SQL Server identifier. */
    private String GetTempName() => Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

    /* The make algorithm cares only about content changes,
       not timestamps.
       
       For stored procedures, setting a version string in the test
       source SQL files is how this test suite changes the contents.
       
       Note the version must be in the CREATE statement's body.
       SQL Server only retains stored procedure CREATE statements
       in sys.sql_modules. All of the source code outside of the
       CREATE statement is discarded by SQL Server after compilation. */

    private enum FileVersion { One, Two }

    private Boolean DoesStringContainFileVersion(String s, FileVersion fileVersion) => s.ContainsCI($"FileVersion_{fileVersion}");

    private void CreateIndependentSPFile(FileVersion fileVersion)
    {
      var sql = $@"
USE [{_databaseName}];
GO

IF OBJECT_ID(N'dbo.independent_sp') IS NOT NULL
  DROP PROCEDURE dbo.independent_sp;
GO

CREATE PROCEDURE dbo.independent_sp
  (
    @id INT
  )
AS
BEGIN
  SET NOCOUNT ON;

  -- FileVersion_{fileVersion};

  SET NOCOUNT OFF;
END;

GO
";

      File.WriteAllText(_independentSpFilename, sql);
    }

    private void CreateDependentSPFile(FileVersion fileVersion)
    {
      var sql = $@"
USE [{_databaseName}];
GO

IF OBJECT_ID(N'dbo.dependent_sp') IS NOT NULL
  DROP PROCEDURE dbo.dependent_sp;
GO

CREATE PROCEDURE dbo.dependent_sp
  (
    @udtt dbo.udtt READONLY
  )
AS
BEGIN
  SET NOCOUNT ON;

  -- FileVersion_{fileVersion};

  SET NOCOUNT OFF;
END;

GO
";

      File.WriteAllText(_dependentSpFilename, sql);
    }

    /* For user-defined table types (udtt), SQL Server doesn't retain
       *any* of the source code after compilation.  The udtt name is
       recorded in sys.table_types, and the udtt's column definitions
       are stored in sys.columns.
       
       To detect changes in the content of a udtt, something in the logical format
       of the udtt must change between versions.  About the only thing
       that can reasonably change are the column definitions.
       So this method just alters the udtt's sole column name to
       include the given version. */

    private void CreateUdttFile(FileVersion fileVersion)
    {
      var sql = $@"
USE [{_databaseName}];
GO

CREATE TYPE dbo.udtt AS TABLE
(
  [FileVersion_{fileVersion}] NVARCHAR(MAX) NOT NULL
);
GO
";

      File.WriteAllText(_udttFilename, sql);
    }

    private String GetStoredProcedureContentsFromServer(String storedProcedureName)
    {
      var sql = $@"
SELECT
    M.definition
  FROM
    sys.sql_modules AS M
    INNER JOIN sys.objects AS O ON O.object_id = M.object_id
  WHERE
    O.name = '{storedProcedureName}';";

      var table = _connection.GetDataSet(sql).Tables[0];
      if (table.Rows.Count == 0)
        throw new Exception($"Could not find stored procedure '{storedProcedureName}'.");
      else
        return table.Rows[0]["definition"].ToString();
    }

    private IEnumerable<String> GetUdttColumnNames(String udttName)
    {
      var sql = $@"
SELECT
    C.name
  FROM
    sys.table_types AS TT
    INNER JOIN sys.columns AS C ON TT.type_table_object_id = C.object_id
  WHERE
    TT.name = '{udttName}';";

      var table = _connection.GetDataSet(sql).Tables[0];
      if (table.Rows.Count == 0)
        throw new Exception($"Could not find any columns for UDTT '{udttName}'.");
      else
        return table.AsEnumerable().Select(row => row.Field<String>("name"));
    }

    private Boolean DoesIndependentSPServerObjectExist(FileVersion fileVersion) => DoesStringContainFileVersion(GetStoredProcedureContentsFromServer("independent_sp"), fileVersion);
    private Boolean DoesDependentSPServerObjectExist(FileVersion fileVersion) => DoesStringContainFileVersion(GetStoredProcedureContentsFromServer("dependent_sp"), fileVersion);
    private Boolean DoesUdttServerObjectExist(FileVersion fileVersion) => DoesStringContainFileVersion(GetUdttColumnNames("udtt").Join(","), fileVersion);

    /* Protect all of the DROP statements with IF/THEN logic.
       This prevents any nasty surprises if a test tries to
       drop a non-existent server object. */

    private void DropIndependentSP()
    {
      var sql = @"
IF OBJECT_ID(N'dbo.independent_sp') IS NOT NULL
  DROP PROCEDURE dbo.independent_sp;";

      _connection.ExecuteNonQuery(sql);
    }

    private void DropDependentSP()
    {
      var sql = @"
IF OBJECT_ID(N'dbo.dependent_sp') IS NOT NULL
  DROP PROCEDURE dbo.dependent_sp;";

      _connection.ExecuteNonQuery(sql);
    }

    private void DropUdtt()
    {
      var sql = @"
IF EXISTS(SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'udtt')
  DROP TYPE dbo.udtt;";

      /* Just to be safe, make sure the stored procedure
         that depends on this UDTT is dropped before the UDTT is. */
      DropDependentSP();
      _connection.ExecuteNonQuery(sql);
    }

    private void CheckCompiledObjects(Int32 testNumber, IEnumerable<String> objectNamesThatWereCompiled, IEnumerable<String> objectNamesThatShouldHaveBeenCompiled)
    {
      var objectNamesThatWereCompiledButShouldNotHaveBeen = objectNamesThatWereCompiled.Except(objectNamesThatShouldHaveBeenCompiled);
      var objectNamesThatWereNotCompiledButShouldHaveBeen = objectNamesThatShouldHaveBeenCompiled.Except(objectNamesThatWereCompiled);
      var exceptionMessages = new List<String>();

      if (objectNamesThatWereCompiledButShouldNotHaveBeen.Any())
        exceptionMessages.Add($"{testNumber}. These objects were compiled but should not have been: { objectNamesThatWereCompiledButShouldNotHaveBeen.Join(", ") }");

      if (objectNamesThatWereNotCompiledButShouldHaveBeen.Any())
        exceptionMessages.Add($"{testNumber}. These objects were not compiled but should have been: { objectNamesThatWereNotCompiledButShouldHaveBeen.Join(", ") }");

      if (exceptionMessages.Any())
        throw new Exception(exceptionMessages.Join());
    }

    [Test]
    public void SqlMakeTest_Simple()
    {
      /* Overview:

           This is a set of very simple tests.

           Three files are created:

             - A user-defined table type (udtt).
             - A dependent stored procedure.
               This object references the udtt.
               SQL Server requires that the udtt exist before
               this dependent sp can be compiled, and - conversely -
               the dependent sp has to be dropped before the udtt
               can be dropped.
             - An independent stored procedure (sp).
               It's "independent" in that it has no dependencies
               on the udtt.
             
           Tests 1 thru 7 create, modify and delete the above source files,
           then run the make algorithm to ensure it behaves correctly.
           (The "File Tests" section.)
         
           Tests 8 thru 11 exercise the make algorithm by dropping and recreating
           the corresponding SQL Server objects for the above stored procedures and udtt.
           (The "SQL Server Object Tests" section.)
      
         Implementation Note:
      
           NUnit doesn't guarantee that individual test methods will be
           run in any kind of order.  But these particular tests are
           tightly coupled to one another via the source files they work with.
           That requires the tests to be run in a specific order.
           Therefore, all of the tests are contained in one test method
           where their execution order can be explicitly stated.
         
           The tests are very similar to one another, so they're differentiated
           from each other by a number in the error message they return.
           This makes it easier to tell which test failed. */

      /*******************************************

       File Tests

       These tests manipulate the source files to see if the
       make algorithm creates (or ignores) the correct corresponding
       SQL Server objects.

       *******************************************/

      /*****************************************************************************************
         1. Initial environment: create and make all three files.
            All three should exist on the server as version 1. */
      CreateIndependentSPFile(FileVersion.One);
      CreateDependentSPFile(FileVersion.One);
      CreateUdttFile(FileVersion.One);

      var make = new Make(_connection);
      make.AddDirectory(_tempFolder, "*.sql", SearchOption.AllDirectories);
      make.LogEvent += (_, e) => _log.WriteLine(EventLogEntryType.Information, e.GetMessage());
      make.ErrorEvent += (_, e) => _log.WriteLine(EventLogEntryType.Error, e.GetException().GetAllExceptionMessages());
      make.Run();

      Assert.IsTrue(DoesIndependentSPServerObjectExist(FileVersion.One), "1. No independent V1.");
      Assert.IsTrue(DoesDependentSPServerObjectExist(FileVersion.One), "1. No dependent V1.");
      Assert.IsTrue(DoesUdttServerObjectExist(FileVersion.One), "1. No udtt V1.");

      /* The Assert.IsTrue calls only check for the existence of objects on the server.
         Those assertions cannot tell if the make algorithm did too much work.
         There are two kinds of extra work:
           compiling an object more than once, and
           compiling an object when it doesn't have to
         The make algorithm will throw an exception if it tries to compile an object more than once.
         But the algorithm can't check itself to ensure it's not compiling an object when it doesn't have to.
         After all, that's the make algorithm's only purpose,
         and that's a bug that has to be checked for by theses tests.
         That's what the CheckCompiledObjects() method does. */
      CheckCompiledObjects(1, make.CompiledObjectNames, new[] { _independentSpObjectName, _dependentSpObjectName, _udttObjectName });

      /*****************************************************************************************
         2. Update the indpendent stored procedure to version 2.
            Assert that only that server object is a version 2.
            The dependent stored procedure and udtt should still
            be a version 1. */

      CreateIndependentSPFile(FileVersion.Two);

      make.Run();

      Assert.IsTrue(DoesIndependentSPServerObjectExist(FileVersion.Two), "2. No independent V2.");
      Assert.IsTrue(DoesDependentSPServerObjectExist(FileVersion.One), "2. No dependent V1.");
      Assert.IsTrue(DoesUdttServerObjectExist(FileVersion.One), "2. No udtt V1.");
      CheckCompiledObjects(1, make.CompiledObjectNames, new[] { _independentSpObjectName });

      /*****************************************************************************************
         3. Delete the independent stored procedure file.
            Its corresponding server object should remain untouched
            by the make algorithm (i.e. make should never drop an object
            if its file doesn't exist).  All of the previous assertions
            should still be true. */

      File.Delete(_independentSpFilename);

      make.Run();

      Assert.IsTrue(DoesIndependentSPServerObjectExist(FileVersion.Two), "3. No independent V2.");
      Assert.IsTrue(DoesDependentSPServerObjectExist(FileVersion.One), "3. No dependent V1.");
      Assert.IsTrue(DoesUdttServerObjectExist(FileVersion.One), "3. No udtt V1.");
      CheckCompiledObjects(3, make.CompiledObjectNames, _emptyStringList);

      /*****************************************************************************************
         4. Restore the independent stored procedure file at version 1, and
            update the dependent stored procedure file to version 2. */

      CreateIndependentSPFile(FileVersion.One);
      CreateDependentSPFile(FileVersion.Two);

      make.Run();

      Assert.IsTrue(DoesIndependentSPServerObjectExist(FileVersion.One), "4. No independent V1.");
      Assert.IsTrue(DoesDependentSPServerObjectExist(FileVersion.Two), "4. No dependent V2.");
      Assert.IsTrue(DoesUdttServerObjectExist(FileVersion.One), "4. No udtt V1.");
      CheckCompiledObjects(4, make.CompiledObjectNames, new[] { _independentSpObjectName, _dependentSpObjectName });

      /*****************************************************************************************
         5. Delete the dependent stored procedure file.
            Its corresponding server object should remain untouched
            by the make algorithm (i.e. make should never drop an object
            if its file doesn't exist).  All of the previous assertions
            should still be true. */

      File.Delete(_dependentSpFilename);

      make.Run();

      Assert.IsTrue(DoesIndependentSPServerObjectExist(FileVersion.One), "5. No independent V1.");
      Assert.IsTrue(DoesDependentSPServerObjectExist(FileVersion.Two), "5. No dependent V2.");
      Assert.IsTrue(DoesUdttServerObjectExist(FileVersion.One), "5. No udtt V1.");
      CheckCompiledObjects(5, make.CompiledObjectNames, _emptyStringList);

      /*****************************************************************************************
         6. Restore the dependent stored procedure file at version 1, and
            update the udtt file to version 2. */

      CreateDependentSPFile(FileVersion.One);
      CreateUdttFile(FileVersion.Two);

      make.Run();

      Assert.IsTrue(DoesIndependentSPServerObjectExist(FileVersion.One), "6. No independent V1.");
      Assert.IsTrue(DoesDependentSPServerObjectExist(FileVersion.One), "6. No dependent V1.");
      Assert.IsTrue(DoesUdttServerObjectExist(FileVersion.Two), "6. No udtt V2.");
      CheckCompiledObjects(6, make.CompiledObjectNames, new[] { _dependentSpObjectName, _udttObjectName });

      /*****************************************************************************************
         7. Delete the udtt file.
            Its corresponding server object should remain untouched
            by the make algorithm (i.e. make should never drop an object
            if its file doesn't exist).  All of the previous assertions
            should still be true. */

      File.Delete(_udttFilename);

      make.Run();

      Assert.IsTrue(DoesIndependentSPServerObjectExist(FileVersion.One), "7. No independent V1.");
      Assert.IsTrue(DoesDependentSPServerObjectExist(FileVersion.One), "7. No dependent V1.");
      Assert.IsTrue(DoesUdttServerObjectExist(FileVersion.Two), "7. No udtt V2.");
      CheckCompiledObjects(7, make.CompiledObjectNames, _emptyStringList);

      /*******************************************

       SQL Server Object Tests

       These tests delete the objects on the server to see if the
       make algorithm correctly re-creates them.

       *******************************************/

      /*****************************************************************************************
         8. Restore the initial file environment, then drop all of the
            corresponding server objects.  Essentially the same as test #1. */

      CreateIndependentSPFile(FileVersion.One);
      CreateDependentSPFile(FileVersion.One);
      CreateUdttFile(FileVersion.One);

      DropUdtt(); /* Also drops the dependent SP. */
      DropIndependentSP();

      make.Run();

      Assert.IsTrue(DoesIndependentSPServerObjectExist(FileVersion.One), "8. No independent V1.");
      Assert.IsTrue(DoesDependentSPServerObjectExist(FileVersion.One), "8. No dependent V1.");
      Assert.IsTrue(DoesUdttServerObjectExist(FileVersion.One), "8. No udtt V1.");
      CheckCompiledObjects(8, make.CompiledObjectNames, new[] { _independentSpObjectName, _dependentSpObjectName, _udttObjectName });

      /*****************************************************************************************
         9. Drop the independent stored procedure server object. */

      DropIndependentSP();

      make.Run();

      Assert.IsTrue(DoesIndependentSPServerObjectExist(FileVersion.One), "9. No independent V1.");
      Assert.IsTrue(DoesDependentSPServerObjectExist(FileVersion.One), "9. No dependent V1.");
      Assert.IsTrue(DoesUdttServerObjectExist(FileVersion.One), "9. No udtt V1.");
      CheckCompiledObjects(9, make.CompiledObjectNames, new[] { _independentSpObjectName });

      /*****************************************************************************************
         10. Drop the dependent stored procedure server object. */

      DropDependentSP();

      make.Run();

      Assert.IsTrue(DoesIndependentSPServerObjectExist(FileVersion.One), "10. No independent V1.");
      Assert.IsTrue(DoesDependentSPServerObjectExist(FileVersion.One), "10. No dependent V1.");
      Assert.IsTrue(DoesUdttServerObjectExist(FileVersion.One), "10. No udtt V1.");
      CheckCompiledObjects(10, make.CompiledObjectNames, new[] { _dependentSpObjectName });

      /*****************************************************************************************
         11. Drop the udtt (which also drops the dependent stored procedure server object). */

      DropUdtt(); /* Also drops the dependent SP. */

      make.Run();

      Assert.IsTrue(DoesIndependentSPServerObjectExist(FileVersion.One), "11. No independent V1.");
      Assert.IsTrue(DoesDependentSPServerObjectExist(FileVersion.One), "11. No dependent V1.");
      Assert.IsTrue(DoesUdttServerObjectExist(FileVersion.One), "11. No udtt V1.");
      CheckCompiledObjects(11, make.CompiledObjectNames, new[] { _dependentSpObjectName, _udttObjectName });
    }
  }
}
