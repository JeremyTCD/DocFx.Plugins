using HtmlAgilityPack;
using System;

namespace JeremyTCD.DocFx.Plugins.Utils
{
    public static class SnippetCreator
    {
        public static HtmlNode CreateSnippet(HtmlNode article, string href)//, int snippetLength)
        {
            HtmlNode snippet = HtmlNode.CreateNode("<article></article>");

            // Title
            HtmlNode titleNode = article.SelectSingleNode(".//h1[contains(@class, 'title')]");
            if (titleNode == null)
            {
                // Title node is used as link to the article, it is mandatory
                throw new InvalidOperationException($"{nameof(SnippetCreator)}: Article {href} has no title (mimo_pageTitle is unspecified). A title is required for an article to " +
                    $"be included in the article list.");
            }
            HtmlNode titleAnchorNode = HtmlNode.CreateNode($"<a href=\"/{href.Replace(".html", "")}\"></a>");
            titleAnchorNode.InnerHtml = titleNode.InnerText;
            HtmlNode newTitleNode = titleNode.CloneNode(false);
            newTitleNode.InnerHtml = "";
            newTitleNode.AppendChild(titleAnchorNode);
            snippet.AppendChild(newTitleNode);

            // Metadata
            HtmlNode metaNode = article.SelectSingleNode(".//div[contains(@class, 'meta')]");
            if(metaNode != null)
            {
                // If node is reused instead of cloned, article node will no longer be searcheable. Not sure why.
                snippet.AppendChild(metaNode.CloneNode(true));
            }

            // Content
            HtmlNode contentNode = article.SelectSingleNode(".//div[contains(@class, 'content')]");
            HtmlNode descriptionNode = contentNode.SelectSingleNode(".//p");
            if(descriptionNode != null)
            {
                HtmlNode newContentNode = contentNode.CloneNode(false);
                newContentNode.AppendChild(descriptionNode.CloneNode(true));
                snippet.AppendChild(newContentNode.CloneNode(true));
            }

            // TODO allow user to specify that snippet should be the first x characters instead of the first paragraph of the article.
            // TrimNode(snippet, 0, snippetLength);

            //HtmlNodeCollection headers = snippet.SelectNodes(".//*[self::h2 or self::h3 or self::h4 or self::h5 or self::h6]");
            //if (headers != null)
            //{
            //    foreach (HtmlNode node in headers)
            //    {
            //        node.Attributes.Add("class", "no-anchor" + node.Attributes["class"]?.Value ?? "");
            //    }
            //}

            return snippet;
        }
    }
}
