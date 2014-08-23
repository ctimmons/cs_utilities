/* See UNLICENSE.txt file for license details. */

using System;
using System.Linq;
using System.Web;

using Utilities.Core;

using HtmlAgilityPack;

namespace Utilities.Internet
{
  public static class HtmlAgilityUtils
  {
    public static HtmlDocument GetHtmlDocument(String queryString)
    {
      var htmlDocument = new HtmlDocument();
      htmlDocument.LoadHtml(HttpUtils.GetUrlAsString(queryString));
      return htmlDocument;
    }

    public static Int32 GetNumberOfLogicalColumns(HtmlNodeCollection tds)
    {
      /* Given a collection of TD elements (representing physical columns in a row),
         how many logical columns are there?  Calculate by taking into account
         the "colspan" attribute that may be present in each TD element. 
      
         For example:
      
           <TR>
             <TD>data</TD>
             <TD colspan = "3">more data</TD>
           </TR>

         represents two physical columns, but four logical columns.
      */

      var numberOfLogicalColumns = 0;
      foreach (var td in tds)
      {
        var colspanAttribute = td.Attributes["colspan"];
        if (colspanAttribute == null)
          numberOfLogicalColumns++;
        else
          numberOfLogicalColumns += Convert.ToInt32(colspanAttribute.Value);
      }

      return numberOfLogicalColumns;
    }

    public static String InnerHtml(this HtmlNodeCollection nodes)
    {
      return nodes.Aggregate("", (acc, source) => acc += source);
    }
  }
}
