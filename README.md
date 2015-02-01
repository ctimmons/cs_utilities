cs_utilities (A C# Utilities Library)
=====================================

Just a few projects of C# utility code I've accumulated over the years.

I've placed all of the code in the public domain.  See the UNLICENSE.txt file in the root folder of each project for the details.

The repository contains Visual Studio 2013 projects which are configured to use .Net 4.5.1, but most of the code should work with 3.5 or even earlier.

Most of the code in Utilities.Core has unit tests in Utilities.Core.Tests.  Unit testing Utilities.Internet proved to be a non-starter.  Instead of mock objects, I like to write integration or system test harnesses.  I've got some floating around for the internet-related code, but it's too raw/unstable/ugly to add to the repo right now.

The vast majority of methods are short, usually less than five lines of code.  I've tried to make the method and property names clearly state what they do, so the documentation is pretty sparse because I think it would be redundant.  For some classes I've included a comment at the beginning of the class that gives an overview and examples of what the class is all about.  The unit tests give additional usage examples.

There are no XML comments for most of the code, even though C# and Visual Studio have excellent support for that feature.  As I noted above, documenting all of those class members seemed redundant, so I only documented the code that really needed some explanation.  I envision these libraries being used in the same way I use them - as projects in a solution, not as DLLs in the GAC.  When used as projects in a solution, it's a simple matter of pressing F12 to see the code for any of the class members in these projects.  Or, since I put all of the code in the public domain, anyone can copy-n-paste the code into their own projects.  Considering these usage scenarios, writing detailed XML comments didn't seem like a good investment of my time.

The exception to my "no XML comment" approach is the Utilities.Sql project.  I've added XML comments to many of the public members because this code isn't simply a collection of small, independent methods.  Utilities.Sql contains several classes used to generate SQL Server TSQL stored procedures, and the corresponding C#/F#/Visual Basic classes and methods.

Dependencies
------------

Utilities.Core has no dependencies outside of .Net.

Utilities.Core.Tests depends on Utilities.Core and [NUnit](http://www.nunit.org/) (nunit.framework.dll).

Utilities.Internet depends on Utilities.Core and [Html Agility Pack](http://htmlagilitypack.codeplex.com/) (HtmlAgilityPack.dll).

Utilities.Sql depends on Utilities.Core.  Note, however, the code it generates may have dependencies on other assemblies, like Utilities.Core, Utilities.Sql, and/or Microsoft.SqlServer.Types.

(To get the Microsoft.SqlServer.Types assembly, go to [SQL Server 2012 SP1 Feature Pack](http://www.microsoft.com/en-us/download/details.aspx?id=35580) and expand the "Install Instructions" section.  Scroll down to the section labeled "Microsoft® System CLR Types for Microsoft® SQL Server® 2012" and download and run the appropriate 32 or 64 bit installer.

There are feature packs available for other versions of SQL Server.  Go to [Microsoft Search](http://search.microsoft.com/) and search for "sql server feature pack".)

Utilities.Sql.Tests depends on Utilities.Sql and [NUnit](http://www.nunit.org/) (nunit.framework.dll).

Highlights
----------

Utilities.Core includes abstractions for easier handling of strings and DateTimes, code for simplifying assertions (Assert.cs), and code for serializing/deserializing XML (Xml.cs).

Utilities.Sql provides a set of classes that read the metadata from one or more SQL Server databases.  The classes expose this metadata so it can be used in T4 templates to generate TSQL stored procedures, and C#/F#/Visual Basic database access code.  In other words, this project can be used to generate your own custom ORM, without the drawbacks of an ORM.  See the [t4_sql_examples](https://github.com/ctimmons/t4_sql_examples) repository for several examples.

Pull Requests and Copying
-------------------------

Please note this library is in the public domain.  I'm going to assume any pull requests are also in the public domain.  If the pull request code has a non-public domain license, I'm afraid I can't accept the request.

On the other hand, since this library is in the public domain, if you want to copy any of the library's code to your own projects, you're free to do so.  You don't even need to give me credit (though that would be nice).
