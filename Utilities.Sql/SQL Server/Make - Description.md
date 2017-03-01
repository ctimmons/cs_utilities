## OVERVIEW (TL;DR)

This make algorithm will efficiently compile T-SQL source files that conform to the following restrictions:

- Each source file contains one, and only one, CREATE statement.
- The source file contains a definition for a user-defined table type, or a non-type object.
- The source file's name must be the same as the object or type it contains (ignoring square brackets in the name).

Given a list of source files and a database connection, this make algorithm will compile only those files that either don't exist on the database server, or whose contents have changed since the last time this make algorithm was executed.

Regarding the compilation of a user-defined table type, the algorithm will drop and re-create any objects that depend on the type.


## HOW TO USE

See the [ctimmons/cs_tsql_make_example](https://github.com/ctimmons/cs_tsql_make_example) project on GitHub for example code.

Inputs:

- SQL Server connection object (opened).
- Folder(s) where the T-SQL files reside, and/or names of the individual T-SQL files.
- Optional default schema name used when a one-part T-SQL identifier is encountered (default is "dbo").
- Optional error callback/event handler.  The make algorithm will not stop on an error, but only log the errors that do occur by calling the callback and passing the System.Exception descendant of the error.
- Optional logging callback/event handler.

Assumptions:

- Each SQL source file holds one, and only one, SQL Server object (i.e. one CREATE statement).
- The file name is a one- or two-part SQL Server object name (square brackets in the file name are ignored).
- The file name, and the name of the SQL Server object it contains, are logically the same.

Outputs and Side Effects:

- One or more "state" data files will either be created or updated in the current user's application data folder.  The data files contain information related to the content and last-compiled date of each SQL file in the folder.
- Any SQL objects that don't yet exist or have changed since the last compilation run will be compiled on the specified instance of SQL Server.
- Any errors that occur will NOT throw an exception.  Instead, all exceptions are sent to the Make.ErrorEvent event handler.  The make algorithm will stop after the first exception is sent to the event handler.
- If a user-defined table type needs to be compiled, any type-related dependencies will be dropped and re-created. If the source file for a dependency is not available, an error will be registered, and make algorithm will stop. (The source for a dependency object is present in sys.sql_modules, but it might not be *all* of the source that was originally executed to compile the object.  Things in the object's original source file preamble like various SETs, and after the object is compiled, like GRANT statements.  Those things need to be executed when the object is recompiled, which can only realistically be done from the source file, not sys.sql_modules).


## A DETAILED INTRODUCTION

I use T4 templates to generate most of my T-SQL code.  I also sometimes make changes to a lot of T-SQL files.  In both cases I have to manually recompile each file individually.

User-defined table types are especially annoying to compile.  All objects that depend on the type must be dropped before the type can be re-compiled.  Then all of those dropped objects have to be recompiled (MS didn't bother implementing an ALTER TYPE statement).

It would be nice to have some kind of "make" algorithm that could detect which T-SQL files are out-of-date and automatically recompile them for me in the correct order.

A traditional make utility - like GNU Make or Microsoft's NMake - works by comparing the timestamps of related source, object, and compiled files.

The main drawbacks to using a traditional make utility for compiling SQL Server files are the assumptions the make utility makes:

First, a traditional make utility assumes everything is in the file system, so source and object files can be compared by their name and timestamp.
    
For T-SQL code, source files are stored in the file system, and compiled objects stored in a database.  Once an object is compiled, there's no way to tell what - if any - T-SQL source file it was compiled from.  SQL Server Management Studio (SSMS) allows programmers to create objects interactively, so there might not be a source file at all (though this is not recommended in for production development because the object's source code won't be in source control).

The second assumption is that source files and object files are on the same computer, so comparing their timestamps returns meaningful information because their timestamps were both set by the same clock.

While T-SQL source files are stored in the file system, SQL Server object files are stored in a database on a server. The database may be on a different computer, and might even be in a different time zone. Comparing two files' timestamps set by different computer clocks will always have a margin of error.

This means a traditional make utility can't deal with how SQL Server does things, so I wrote this little "make" algorithm.


## LIMITED SCOPE

SQL Server has a few quirks.  Therefore, this make algorithm has several limitations.


### COMPILE-TIME DEPENDENCIES ON USER-DEFINED TYPES

There are all kinds of problems with compiling user-defined types in SQL Server.

Broadly speaking, SQL Server places compilable T-SQL CREATE statements into two categories: user-defined types ("types") and everything else ("objects").

Objects have no compile-time dependencies.  In other words, SQL Server will compile an object if it references another object that doesn't exist at compile time.  For example, a CREATE PROCEDURE statement will successfully compile if it refers to a table that doesn't exist in the database at compile time.  Naturally, a runtime error will occur if the stored procedure is executed and the table doesn't exist at runtime.

However, a missing type will cause a compile-time error.  There's little danger of a built-in type not being available at compile-time.  User-defined types are a different (and poorly implemented) story.

SQL Server supports three kinds of user-defined types:  CLR, scalar and table.  See the [CREATE TYPE help entry](https://msdn.microsoft.com/en-us/library/ms175007%28v=sql.110%29.aspx) for details.

Microsoft's names for these different types are ambiguous.  They call user-defined scalar types "alias types", and CLR types "user-defined types".  MS did manage to correctly name user-defined table types.  Here's a table to add to the confusion:

Official MS Term   | How It Appears in SSMS     | Unambiguous Name That I Insist On Using
------------------------|-------------------------|-------------------------
Alias Type              | User-Defined Data Type  | User-Defined Scalar Type 
User-Defined Type       | User-Defined Type       | User-Defined CLR Type
User-Defined Table Type | User-Defined Table Type | User-Defined Table Type

I use the unambiguous names both in the code and this document.

I did not include support for user-defined CLR types.  I've never met a DBA who allows CLR types or CLR assemblies in their database, so I've never had a chance to use them in my code.  That might change in the future (see section "5. FUTURE ENHANCEMENTS" below).

User-defined scalar types (UDST) are a nice idea, but SQL Server's implementation is broken.  SQL Server provides CREATE TYPE and DROP TYPE statements, but no corresponding ALTER TYPE statement.  The programmer must manually drop or alter any object or table type that references the UDST before the UDST's definition can be changed. This makes it difficult - verging on impossible, especially on large tables - to change a UDST's definition.  As a result, this make algorithm doesn't support UDSTs because they're just too painful to work with.  See Aaron Bertrand's blog entry for a [reality check on UDSTs](http://sqlblog.com/blogs/aaron_bertrand/archive/2009/10/14/bad-habits-to-kick-using-alias-types.aspx).

That leaves user-defined table types (UDTT).  They suffer from the same limitations as UDSTs, but UDTTs are much more useful.  UDTTs allow multiple rows to be sent as a single parameter to a stored procedure via ADO.Net calls.  Speaking as a database programmer, this is a *great* feature.  This algorithm *does* support user-defined table types, as long as they don't reference user-defined scalar types or CLR types.

Because SQL Server throws a compile-time error for missing types, but not for missing objects, the make algorithm makes sure the UDTTs are compiled *before* any objects that rely on them.  The algorithm examines each source file and determine if it contains a UDTT definition.  Then the algorithm drops any objects that rely on the UDTTs so SQL Server doesn't throw an error if a UDTT is dropped with objects still referencing it.  Those dropped objects are tracked so the algorithm can make sure they're re-compiled after the UDTTs are created.  Finally the UDTTs are dropped and compiled.


### SCRIPT COMPLEXITY

#### Multiple Objects and/or Types in a Script

A script (i.e. a file containing T-SQL code) can contain any kind of code, including definitions for multiple objects.

The make algorithm's compilation unit is a source file. This poses a problem because a script can contain any kind of code, including definitions for multiple objects, as well as supporting code (explained below).

The algorithm wants to be as efficient as possible and only compile those objects that either don't exist in the database, or have changed since they were last compiled.  If a script contains definitions for multiple objects, it's difficult to determine which objects need to be compiled and which ones don't.  Even if only one of the objects in a script needs to be compiled, the make algorithm will still have to compile the entire script.

Disregarding dynamic SQL for a moment, it may be *technically* possible to use the ScriptDOM parser library and pick each script apart into its component objects.  But this would only reveal the structure of the code, and not its meaning (See the example below.  Supporting code can have far reaching effects, like revoking/granting permissions, among many other possible changes to the state of the database).  The make algorithm wouldn't know what parts of the script to compile and which ones not to.  Also, ScriptDOM can't parse strings of dynamic SQL.

A simple restriction solves this problem.  This make algorithm only works with script files that contain one object or user-defined table type definition (i.e. one CREATE statement).

#### Script Name

The script's filename must be the same as the object it contains.  If the object name is a one-part name, the script's filename should reflect that.  Ditto for two-part names.  The make algorithm doesn't work with three- or four-part names. Square brackets in the script's filename and object name are ignored by the make algorithm.  This means names like "dbo.myproc" and "[dbo].[myproc]" are treated as equal.

This restriction is necessary to allow the make algorithm to match the T-SQL source file with its corresponding object in the database's sys.objects table, or the user-defined table type in the sys.table_types table.

### Supporting Code in a Script

For this make utility to work correctly, a script can contain supporting code, like USE, IF and GRANT statements, but it can only contain one CREATE statement.

For example, here's a simple script named 'dbo.mydb_sp_say_hello.sql'.  Note both the script's filename and CREATE statement are the same, and both use a two-part T-SQL object name.  (Square brackets in filenames are optional/ignored by the make algorithm).

```sql
USE [mydb];
GO

IF OBJECT_ID(N'[dbo].[mydb_sp_say_hello]') IS NOT NULL
  DROP PROCEDURE [dbo].[mydb_sp_say_hello]; 
GO

SET NUMERIC_ROUNDABORT OFF;
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER, XACT_ABORT ON;
GO

CREATE PROCEDURE [dbo].[mydb_sp_say_hello]
AS
BEGIN
  SELECT "Hello, world!";
END
GO

REVOKE EXECUTE ON [dbo].[mydb_sp_say_hello] FROM PUBLIC;
GO

BEGIN
  DECLARE @environment_type INT;
  DECLARE @error INT;
  DECLARE @dev_environment INT = 0;
  DECLARE @qa_environment INT = 1;
  DECLARE @prod_environment INT = 2;

  EXEC dbo.sp_get_server_environment_type @environment_type OUTPUT;

  SET @error = @@ERROR;

  IF (@error != 0)
  BEGIN
    RAISERROR('[dbo].[mydb_sp_say_hello]: Error %ld. Unable to determine the server environment type. Object permissions will not be set.', 16, 1, @error);
    RETURN 1;
  END

  IF @environment_type IN (@dev_environment, @qa_environment)
  BEGIN
    GRANT EXECUTE ON [dbo].[mydb_sp_say_hello] TO db_developer;
  END
  ELSE IF @environment_type IN (@prod_environment)
  BEGIN
    GRANT EXECUTE ON [dbo].[mydb_sp_say_hello] TO db_prod;
  END
  ELSE
  BEGIN
    RAISERROR('[dbo].[mydb_sp_say_hello]: Invalid server environment type (%ld). Object permissions will not be set.', 16, 1, @environment_type);
    RETURN 2;
  END

  GRANT Execute ON [dbo].[mydb_sp_say_hello] TO db_readwrite;
  GRANT Execute ON [dbo].[mydb_sp_say_hello] TO db_appservices;
END
GO

SET NOCOUNT OFF;
GO
```

### SUMMARY OF LIMITATIONS

This make algorithm's limits are:

- No support for CLR types or assemblies
- No support for user-defined scalar types ("alias types")
- A T-SQL script file must only have one object definition (i.e. only one CREATE statement)
- The script file's name must be the same as the object it is creating (ignoring square brackets)
- Only one- or two-part T-SQL identifier names are allowed for the type or object to create.


## THE MAKE ALGORITHM

All make algorithms have the general form:

- Gather source files (inputs).
- Gather matching object/target files (outputs) and determine their dependencies.
- For each source file, if the matching target file is missing or out of date, process the source file and update dependencies.

This make algorithm follows the same general form.

As noted above, timestamps cannot be used to determine if a T-SQL file is out of date.  Some other method of detecting source file changes is needed.  This make algorithm checks a source file for out-of-dateness by comparing the contents of that source file with its previous contents.

The first time the make algorithm is run against a source file, an MD5 hash of that file's contents is stored in a file.  Subsequent runs of the make algorithm retrieve that old MD5 hash and compare it to a new MD5 hash of the source file.  If the hashes are different, the file has changed since the last run and needs to be compiled.

One side effect with this approach is that the first time the make algorithm is run, it will unconditionally compile *all* of the source files.

As for dependencies, SQL Server imposes a compile-time dependency on user-defined types.  Before a user-defined type can be compiled, all of its dependencies must be dropped first.  They must also be tracked so they can be re-compiled after the user-defined type is compiled.  After the dependencies are dropped, and because there is no ALTER TYPE statement, the user-defined type is dropped.  Now the user-defined type can be compiled, followed by the compilation of the previously dropped dependent objects.

The make algorithm looks like this:

- Gather source files (inputs).
- Load old MD5 content hashes for those source files.
- Compare the source files' MD5 content hashes to see which files need to be compiled.
- Ask SQL Server for a list of types and objects that don't yet exist on the server, user-defined types and their dependencies that need to be dropped and re-compiled, as well as the order in which the types and dependencies have to be dropped.
- Drop the required dependencies and user-defined types in the specified order.
- Save the source files' new MD5 content hashes for use in the next run.
- Compile all user-defined table types and objects that either don't exist yet on the server, or have been changed since the last run (outputs). The user-defined types and their dependencies are compiled in the reverse order in which they were dropped.


## FUTURE ENHANCEMENTS

Given the way SQL Server works, I don't see any way around the one-object-or-type-per-file restriction.

User-defined CLR types support might be implemented in the future.  But I don't ever foresee support for user-define scalar types.  The lack of an ALTER TYPE command, plus the high cost of re-creating affected columns and indexes negates any possible benefits user-defined scalar types might provide.

