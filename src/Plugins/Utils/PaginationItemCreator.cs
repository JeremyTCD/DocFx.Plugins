using HtmlAgilityPack;
using System;

namespace JeremyTCD.DocFx.Plugins.Utils
{
    public static class PaginationItemCreator
    {
        public static HtmlNode CreatePaginationItem(HtmlNode article, string href)
        {
            {
                HtmlNode paginationItemNode = HtmlNode.CreateNode("<article class=\"pagination-item\"></article>");

                // Article url
                string articleUrl = "/" + href.Replace(".html", "");

                // Title
                HtmlNode titleNode = article.SelectSingleNode(".//h1");
                if (titleNode == null)
                {
                    // Title node is used as link to the article, it is mandatory
                    throw new InvalidOperationException($"{nameof(PaginationItemCreator)}: Article {href} has no title (mimo_pageTitle is unspecified). A title is required for an article to " +
                        "be included in the article list.");
                }
                HtmlNode titleAnchorNode = HtmlNode.CreateNode($"<a class=\"pagination-item__link\" href=\"{articleUrl}\"></a>");
                titleAnchorNode.InnerHtml = titleNode.InnerText;
                HtmlNode newTitleNode = HtmlNode.CreateNode("<h1 class=\"pagination-item__title\"></h1>"); // We might be converting banner titles to article titles
                newTitleNode.AppendChild(titleAnchorNode);
                paginationItemNode.AppendChild(newTitleNode);

                // Metadata
                HtmlNode metaNode = article.SelectSingleNode(".//div[contains(@class, 'meta')]");
                if (metaNode != null)
                {
                    metaNode = metaNode.CloneNode(true);
                    metaNode.SetAttributeValue("class", "pagination-item__metadata metadata");
                    // If node is reused instead of cloned, article node will no longer be searcheable. Not sure why.
                    paginationItemNode.AppendChild(metaNode);
                }

                // Content
                HtmlNode snippetNode = article.SelectSingleNode(".//p");
                if (snippetNode != null)
                {
                    // Fix hash links in content
                    foreach (HtmlNode htmlNode in snippetNode.SelectNodes("//a[starts-with(@href, '#')]"))
                    {
                        string newHref = articleUrl + htmlNode.GetAttributeValue("href", null);
                        htmlNode.SetAttributeValue("href", newHref);
                    }

                    // Snippet class
                    snippetNode.SetAttributeValue("class", "pagination-item__snippet");

                    // Append content
                    paginationItemNode.AppendChild(snippetNode.CloneNode(true));
                }

                return paginationItemNode;
            }
        }
    }
}
