cs_utilities (A C# Utilities Library)
=====================================

Just a few projects of C# utility code I've accumulated over the years.

I've placed all of the code in the public domain.  See the UNLICENSE.txt file in the root folder of each project for the details.

The projects in this repo are configured to use the latest version of .Net (4.5 as of this writing), but most of the code should work with 3.5 or even earlier.

Most of the code in Utilities.Core has unit tests in Utilities.Core.Tests.  Unit testing Utilities.Internet proved to be a non-starter.  Instead of mock objects, I like to write integration or system test harnesses.  I've got some floating around for the internet-related code, but it's too raw/unstable/ugly to add to the repo right now.

The vast majority of methods are short, usually less than five lines of code.  I've tried to make the method and property names clearly state what they do, so the documentation is pretty sparse because I think it would be redundant.  For some classes I've included a comment at the beginning of the class that gives an overview and examples of what the class is all about.  The unit tests give additional usage examples.

There are no XML comments, even though C# and Visual Studio have excellent support for that feature.  As I noted above, documenting all of those class members seemed redundant, so I only documented the code that really needed some explanation.  I envision these libraries being used in the same way I use them - as projects in a solution, not as DLLs in the GAC.  When used as projects in a solution, it's a simple matter of pressing F12 to see the code for any of the class members in these projects.  Or, since I put all of the code in the public domain, anyone can copy-n-paste the code into their own projects.  Considering these usage scenarios, writing detailed XML comments didn't seem like a good investment of my time.

Dependencies
------------

Utilities.Core has no dependencies outside of .Net.

Utilities.Core.Tests depends on Utilities.Core and [NUnit](http://www.nunit.org/) (nunit.framework.dll). 

Utilities.Internet depends on Utilities.Core and [Html Agility Pack](http://htmlagilitypack.codeplex.com/) (HtmlAgilityPack.dll).

Highlights
----------

Utilities.Core includes code for simplifying assertions (Assert.cs), processing command line items (CommandLine.cs) and serializing/deserializing XML (Xml.cs).

Utilities.Internet has a complete FTP client in Ftp.cs.

Pull Requests and Copying
-------------------------

Please note this library is in the public domain.  I'm going to assume any pull requests are also in the public domain.  If the pull request code has a non-public domain license, I'm afraid I can't accept the request.

On the other hand, since this library is in the public domain, if you want to copy any of the library's code to your own projects, you're free to do so.  You don't need to ask my permission or even give me credit (though that would be nice).
