cs_utilities (A C# Utilities Library)
============

Just a few projects of C# utility code I've accumulated over the years.

I've placed all of the code, with a few exceptions, in the public domain.  See the UNLICENSE.txt file in the root folder of each project for the details.  Any exceptions to this policy are noted in the relevant source code file.

The projects in this repo are configured to use the latest version of .Net (4.5 as of this writing), but most of the code should work back to 3.5 or even earlier.

Most of the code in Utilities.Core has unit tests in Utilities.Core.Tests.  Unit testing Utilities.Internet proved to be a non-starter.  Instead of mock objects, I like to write integration or system test harnesses.  I've got some floating around for the internet-related code, but it's too raw/unstable/ugly to add to the repo right now.

The vast majority of methods are short, usually less than five lines of code.  I've tried to make the method and property names clearly state what they do, so the documentation is pretty sparse because I think it would be redundant.  For some classes I've included a comment at the beginning of the class that gives an overview and examples of what the class is all about.  The unit tests give additional usage examples.

There are no XML comments, even though C# and Visual Studio have excellent support for that feature.  As I noted above, documenting all of those class members seemed redundant, so I only documented the code that really needed some explanation.  I envision these libraries being used in the same way I use them - as projects in a solution, not as DLLs in the GAC.  When used as projects in a solution, it's a simple matter of pressing F12 to see the code for any of the class members in these projects.  Or, since I put all of the code in the public domain, anyone can copy-n-paste the code into their own projects.  Considering these usage scenarios, writing detailed XML comments didn't seem like a good investment of my time.

Dependencies
------------

Utilities.Tests depends on [NUnit](http://www.nunit.org/) (nunit.framework.dll).

Utilities.Internet depends on [Html Agility Pack](http://htmlagilitypack.codeplex.com/) (HtmlAgilityPack.dll).

Highlights
----------

Utilities.Core includes code for simplifying assertions (Assert.cs), processing command line items (CommandLine.cs) and serializing/deserializing XML (Xml.cs).

Utilities.Internet has a complete FTP client in Ftp.cs.
