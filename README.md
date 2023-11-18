cs_utilities (A C# Utilities Library)
=====================================

NOTE: This repository is archived.  Its replacement is the [Lazy8](https://github.com/ctimmons/Lazy8) repository, which is built on .Net Core/5/6/7/8.

Just a few C# projects of utility code I've accumulated over the years.

License
-------

Unless otherwise noted, the code in this respository is governed by the MIT License.  See the LICENSE.txt file in the solution's root folder for the details.

Overview
--------

The repository contains Visual Studio 2015 projects which are configured to use .Net 4.6.1, but most of the code should work with 3.5 or even earlier.

Most of the code in Utilities.Core has unit tests in Utilities.Core.Tests.  Unit testing Utilities.Internet proved to be a non-starter.  Instead of mock objects, I like to write integration or system test harnesses.  I've got some floating around for the internet-related code, but it's too raw/unstable/ugly to add to the repo right now.

The majority of methods are short, usually less than five lines of code.  I've tried to make the method and property names clearly state what they do, so the documentation is pretty sparse because I think it would be redundant.  For some classes I've included a comment at the beginning of the class that gives an overview and examples of what the class is all about.  The unit tests give additional usage examples.

There are few XML comments for most of the code, even though C# and Visual Studio have excellent support for that feature.  As I noted above, documenting all of those class members seemed redundant, so I only documented the code that really needed some explanation.  I envision these libraries being used in the same way I use them - as projects in a solution, not as DLLs in the GAC.  When used as projects in a solution, it's a simple matter of pressing F12 to see the code for any of the class members in these projects.  Considering this usage scenario, writing detailed XML comments didn't seem like a good investment of my time.

The exception to my "no XML comment" approach is the Utilities.Sql project.  I've added XML comments to many of the public members because this code isn't simply a collection of small, independent methods.  Utilities.Sql contains several classes used to generate SQL Server TSQL stored procedures, and the corresponding C#/F#/Visual Basic classes and methods.

Dependencies
------------

Utilities.Core has no dependencies outside of .Net.

Utilities.Sql depends on Utilities.Core.  Note, however, the code it generates may have dependencies on other assemblies, like Utilities.Core, Utilities.Sql, and/or Microsoft.SqlServer.Types.

(To get the Microsoft.SqlServer.Types assembly, go to [SQL Server 2012 SP1 Feature Pack](http://www.microsoft.com/en-us/download/details.aspx?id=35580) and expand the "Install Instructions" section.  Scroll down to the section labeled "Microsoft® System CLR Types for Microsoft® SQL Server® 2012" and download and run the appropriate 32 or 64 bit installer.

There are feature packs available for other versions of SQL Server.  Go to [Microsoft Search](http://search.microsoft.com/) and search for "sql server feature pack".)

Utilities.Core.Tests and Utilities.Sql.Tests depend on the latest 3.x version of [NUnit](http://www.nunit.org/) (nunit.framework.dll).  Use NuGet to install NUnit 3.x, and use Visual Studio's Test Explorer to run the unit tests.

Highlights
----------

Utilities.Core includes abstractions for easier handling of strings and DateTimes, code for simplifying assertions (Assert.cs), and code for serializing/deserializing XML (Xml.cs).

Utilities.Sql provides a set of classes that read the metadata from one or more SQL Server databases.  The classes expose this metadata so it can be used in T4 templates to generate TSQL stored procedures, and C#/F#/Visual Basic database access code.  In other words, this project can be used to generate your own custom ORM, without the drawbacks of an ORM.  See the [t4_sql_examples](https://github.com/ctimmons/t4_sql_examples) repository for several examples.

Utilities.Sql also provides a "make" algorithm for compiling T-SQL source files, in the same spirit as GNU Make and Microsoft's NMake utilties.

Pull Requests and Copying
-------------------------

Please note this library is licensed under the MIT license.  Any pull requests will also be under that license.  If the pull request has a different license, I might not be able to acccept the request.

