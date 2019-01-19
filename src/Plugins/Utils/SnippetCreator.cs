﻿using HtmlAgilityPack;
using System;

namespace JeremyTCD.DocFx.Plugins.Utils
{
    public static class SnippetCreator
    {
        public static HtmlNode CreateSnippet(HtmlNode article, string href)//, int snippetLength)
        {
            HtmlNode snippet = HtmlNode.CreateNode("<article></article>");

            // Article url
            string articleUrl = "/" + href.Replace(".html", "");

            // Title
            HtmlNode titleNode = article.SelectSingleNode(".//h1[contains(@class, 'title')]");
            if (titleNode == null)
            {
                // Title node is used as link to the article, it is mandatory
                throw new InvalidOperationException($"{nameof(SnippetCreator)}: Article {href} has no title (mimo_pageTitle is unspecified). A title is required for an article to " +
                    $"be included in the article list.");
            }
            HtmlNode titleAnchorNode = HtmlNode.CreateNode($"<a href=\"{articleUrl}\"></a>");
            titleAnchorNode.InnerHtml = titleNode.InnerText;
            HtmlNode newTitleNode = titleNode.CloneNode(false);
            newTitleNode.InnerHtml = "";
            newTitleNode.AppendChild(titleAnchorNode);
            snippet.AppendChild(newTitleNode);

            // Metadata
            HtmlNode metaNode = article.SelectSingleNode(".//div[contains(@class, 'meta')]");
            if (metaNode != null)
            {
                // If node is reused instead of cloned, article node will no longer be searcheable. Not sure why.
                snippet.AppendChild(metaNode.CloneNode(true));
            }

            // Content
            HtmlNode descriptionNode = article.SelectSingleNode(".//p");
            if (descriptionNode != null)
            {
                // Fix hash links in content
                foreach(HtmlNode htmlNode in descriptionNode.SelectNodes("//a[starts-with(@href, '#')]"))
                {
                    string newHref = articleUrl + htmlNode.GetAttributeValue("href", null);
                    htmlNode.SetAttributeValue("href", newHref);
                }

                // Append content
                snippet.AppendChild(descriptionNode.CloneNode(true));
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
