/* See UNLICENSE.txt file for license details. */

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

using Utilities.Core;

namespace Utilities.Internet
{
  public static class HttpUtils
  {
    public static String GetUrlAsRoot(String url)
    {
      url.Name("url").NotNull();

      if (url.StartsWith("~/"))
        return url;
      else if (url.StartsWith("/"))
        return "~" + url;
      else
        return "~/" + url;
    }

    /// <summary>
    /// Get the application's root path in a safe and consistent manner.
    /// </summary>
    /// <returns>
    /// Returns the application's root path with a trailing slash
    /// appended.  E.g. on a development machine it will return
    /// http://localhost/mywebapp1/, and on a deployment machine it will return
    /// something like http://www.mysite.com/.
    /// </returns>
    public static String GetApplicationPath()
    {
      return
        Uri.UriSchemeHttp +
        Uri.SchemeDelimiter +
        HttpContext.Current.Request.Headers["Host"].ToLower() +
        HttpContext.Current.Response.ApplyAppPathModifier(
        StringUtils.AddTrailingForwardSlash(HttpContext.Current.Request.ApplicationPath.Trim()));
    }

    public static String GetApplicationPath(String partialUrl)
    {
      /* Remove the leading / character, since the last character
         in the String returned by GetApplicationPath is guaranteed
         to be a / character. */

      return HttpUtils.GetApplicationPath() + partialUrl.TrimStart("/".ToCharArray());
    }

    public static String GetSecureApplicationPath()
    {
      return GetSecureApplicationPath("");
    }

    public static String GetSecureApplicationPath(String partialUrl)
    {
      return HttpUtils.GetApplicationPath().Replace("http://", "https://") + partialUrl;
    }

    public static String GetStringQueryParam(String paramName)
    {
      return GetStringQueryParam(paramName, "");
    }

    public static String GetStringQueryParam(String paramName, String defaultValue)
    {
      /* Replace all carriage returns and line feeds in the parameter w/ an empty String.
       * This is to prevent an "HTTP splitting" attack whereby the attacker
       * tries to "split" the HTTP header via embedded cr/lf characters. */

      return Regex.Replace(HttpContext.Current.Request.Params[paramName] ?? defaultValue, @"[\r\n]", "");
    }

    public static Int32 GetIntQueryStringParam(String paramName)
    {
      /* This will raise an ArgumentNullException if paramName doesn't exist in 
         the URL's querystring. Let the caller worry about handling it.  
         
         That also simplifies the try/catch block below, since it will only 
         have to deal with exceptions raised by Int32.Parse. */
      String paramValue = GetStringQueryParam(paramName);

      /* Even though the parameter name exists, its data might not be a valid integer.
         Throw an single known exception if that's the case.  This is coded this way to simplify
         the caller's response code in catching this exception.  Int32.Parse can raise
         three possible exceptions, and the code below simply abstracts those three
         possibilities into one generic ArgumentException. */
      try
      {
        return Int32.Parse(paramValue);
      }
      catch
      {
        throw new ArgumentExceptionFmt("The URL querystring parameter '{0}' value of {1} is not a valid System.Int32.", paramName, paramValue);
      }
    }

    /// <summary>
    /// Overloaded version of GetIntQueryStringParam which provides for a default
    /// return value if the parameter either does not exist, or its value 
    /// is not a valid <see cref="System.Int32"/>.
    /// </summary>
    /// <param name="paramName">Name of URL querystring parameter.</param>
    /// <param name="defaultValue">
    /// A default value that will be returned if the <paramref name="paramName"/> is
    /// not found in the URL's querystring, or it is not a valid <see cref="System.Int32"/>.
    /// <returns>The querystring parameter's data as a <see cref="System.Int32"/>.</returns>
    public static Int32 GetIntQueryStringParam(String paramName, Int32 defaultValue)
    {
      try
      {
        return GetIntQueryStringParam(paramName);
      }

      /* The "catch" clauses must be in this order, i.e. most specific type
       * of exception to the most general type.  In this case, ArgumentNullException
       * descends from ArgumentException, so it (ArgumentNullException) must come
       * first.
       */

      catch (ArgumentNullException) // paramName isn't in the URL's querystring.
      {
        return defaultValue;
      }
      catch (ArgumentException) // paramName exists, but it's not a valid System.Int32.
      {
        return defaultValue;
      }

      /* Let any other kind of exception percolate up the call stack. */
    }

    public static String GetUrlAsString(String url)
    {
      var request = (HttpWebRequest) WebRequest.Create(new Uri(url));
      request.AllowAutoRedirect = true;
      /* Some websites only return content for web browsers, and not other applications.
         So instead of having a user agent like '.Net App', a "real" user agent
         string that mimics a web browser has to be used. */
      request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:31.0) Gecko/20100101 Firefox/31.0";
      request.Headers["Accept-Language"] = "en-us";
      request.Credentials = CredentialCache.DefaultNetworkCredentials;
      request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

      try
      {
        using (var response = request.GetResponse())
          using (var responseStream = response.GetResponseStream())
            using (var reader = new StreamReader(responseStream, Encoding.ASCII))
              return reader.ReadToEnd().Trim();
      }
      catch (Exception ex)
      {
        return ex.Message;
      }
    }

    public static String[] GetUrlAsStringArray(String url)
    {
      return GetUrlAsString(url).Split("\n".ToCharArray());
    }

    /// <summary>
    /// Given a URL, the base domain will be returned.
    /// </summary>
    /// <param name="url">Any URL.</param>
    /// <returns>If the domain is "localhost", that is returned.  Otherwise,
    /// the return value is a String in the form "name.domain_type".</returns>
    /// <remarks>
    /// This method does not process complex URIs with @ or : characters.
    /// Use the System.Uri or System.UriBuilder classes to parse
    /// the URI, and use the Host property as the url parameter to this method.
    /// </remarks>
    /// <example>
    /// Uri uri = new Uri("http://userinfo@mail.www.mydomain.com:8080");
    /// String s = GetUrlBaseDomain(uri.Host);
    /// 
    /// will return
    /// 
    /// "mydomain.com".
    /// </example>
    public static String GetUrlBaseDomain(String url)
    {
      var host = (new Uri(url)).Host;
      var lastDotIndex = host.LastIndexOf('.');
      if (lastDotIndex == -1)
        return host;
      else
      {
        lastDotIndex = host.LastIndexOf('.', lastDotIndex - 1);
        if (lastDotIndex == -1)
          return host;
        else
          return host.Substring(lastDotIndex + 1);
      }
    }
  }
}
