/* See the LICENSE.txt file in the root folder for license details. */

using System;

namespace Utilities.Internet
{
  public static class WebPageUtils
  {
    public static String GetExternalAnchorElement(String url, String displayText)
    {
      return String.Format("<A href=\"{0}\">{1}</A>", url, displayText);
    }

    public static String GetExternalAnchorElement(String url, String displayText, String cssClass)
    {
      return String.Format("<A href=\"{0}\" class=\"{2}\">{1}</A>", url, displayText, cssClass);
    }

    public static String GetImageAnchorElement(String imageName, Int32 border, Int32 width, Int32 height)
    {
      return String.Format("<IMG src=\"{0}\" border=\"{1}\" width=\"{2}\" height=\"{3}\">",
        HttpUtils.GetApplicationPath() + imageName, border, width, height);
    }

    public static String GetImageAnchorElement(String imageName, Int32 border, Int32 width, Int32 height, String cssClass)
    {
      return String.Format("<IMG src=\"{0}\" border=\"{1}\" width=\"{2}\" height=\"{3}\" class=\"{4}\">",
        HttpUtils.GetApplicationPath() + imageName, border, width, height, cssClass);
    }

    public static void RegisterJavaScriptFile(System.Web.UI.Page page, String key, String filename)
    {
      if (!page.ClientScript.IsStartupScriptRegistered(key))
        page.ClientScript.RegisterStartupScript(page.GetType(), key,
          String.Format("<SCRIPT language=\"javascript\" src=\"{0}\"></SCRIPT>",
          HttpUtils.GetApplicationPath("scripts/" + filename)));
    }

    /* Spammers are getting more ingenious every day (bastards!).
       One trick is for a spammer to use a web-bot to rip e-mail addresses from a web page.

       To prevent the 'bot from recognizing an e-mail address, this
       series of methods is designed to generate small JavaScript
       fragments to dynamically show the e-mail address. */

    /// <summary>
    /// Return a fragment of JavaScript code that generates a spam-proof
    /// e-mail anchor tag.
    /// </summary>
    /// <param name="user">A String containing the user's name (i.e. the text
    /// that appears on the left side of an e-mail address's @ character).</param>
    /// <param name="domain">A String containing the domain name (i.e. the text
    /// that appears on the right side of an e-mail address's @ character).</param>
    /// <returns>A fragment of JavaScript code that generates an HTML anchor tag.</returns>
    public static String GetAntiSpamEMailTag(String user, String domain)
    {
      const String script = @"
<SCRIPT language=""JavaScript"">
<!--
  user = ""{0}"";
  site = ""{1}"";
  document.write('<a href=""mailto:' + user + '@' + site + '"">' + user + '@' + site + '</a>');
//-->
</SCRIPT>
";
      return String.Format(script, user, domain);
    }

    /// <summary>
    /// Return a JavaScript fragment of code that generates a spam-proof
    /// e-mail anchor tag.
    /// </summary>
    /// <param name="user">A String containing the user's name (i.e. the text
    /// that appears on the left side of an e-mail address's @ character).</param>
    /// <param name="domain">A String containing the domain name (i.e. the text
    /// that appears on the right side of an e-mail address's @ character).</param>
    /// <param name="displayText">The text to display as a hyperlink to the 
    /// e-mail address.</param>
    /// <returns>A fragment of JavaScript code that generates an HTML anchor tag.</returns>
    public static String GetAntiSpamEMailTag(String user, String domain, String displayText)
    {
      const String script = @"
<SCRIPT language=""JavaScript"">
<!--
  user = ""{0}"";
  site = ""{1}"";
  document.write('<a href=""mailto:' + user + '@' + site + '"">{2}</a>');
//-->
</SCRIPT>
";
      return String.Format(script, user, domain, displayText);
    }

    /// <summary>
    /// Return a JavaScript fragment of code that generates a spam-proof
    /// e-mail anchor tag, and allows the specification of a CSS class
    /// for the anchor.
    /// </summary>
    /// <param name="user">A String containing the user's name (i.e. the text
    /// that appears on the left side of an e-mail address's @ character).</param>
    /// <param name="domain">A String containing the domain name (i.e. the text
    /// that appears on the right side of an e-mail address's @ character).</param>
    /// <param name="cssClass">A String containing the CSS class for the
    /// generated anchor tag.</param>
    /// <returns>A fragment of JavaScript code that generates an HTML anchor tag.</returns>
    public static String GetCSSAntiSpamEMailTag(String user, String domain, String cssClass)
    {
      const String script = @"
<SCRIPT language=""JavaScript"">
<!--
  user = ""{0}"";
  site = ""{1}"";
  document.write('<a href=""mailto:' + user + '@' + site + '"" class = ""{2}""'"">' + user + '@' + site + '</a>');
//-->
</SCRIPT>
";
      return String.Format(script, user, domain, cssClass);
    }

    /// <summary>
    /// Return a JavaScript fragment of code that generates a spam-proof
    /// e-mail anchor tag.
    /// </summary>
    /// <param name="user">A String containing the user's name (i.e. the text
    /// that appears on the left side of an e-mail address's @ character).</param>
    /// <param name="domain">A String containing the domain name (i.e. the text
    /// that appears on the right side of an e-mail address's @ character).</param>
    /// <param name="displayText">The text to display as a hyperlink to the 
    /// e-mail address.</param>
    /// <param name="cssClass">A String containing the CSS class for the
    /// generated anchor tag.</param>
    /// <returns>A fragment of JavaScript code that generates an HTML anchor tag.</returns>
    public static String GetCSSAntiSpamEMailTag(String user, String domain, String displayText, String cssClass)
    {
      const String script = @"
<SCRIPT language=""JavaScript"">
<!--
  user = ""{0}"";
  site = ""{1}"";
  document.write('<a href=""mailto:' + user + '@' + site + '"" class = ""{3}""'"">{2}</a>');
//-->
</SCRIPT>
";
      return String.Format(script, user, domain, displayText, cssClass);
    }
  }
}
