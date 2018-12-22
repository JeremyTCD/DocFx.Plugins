using HtmlAgilityPack;
using JeremyTCD.DocFx.Plugins.Utils;
using Microsoft.DocAsCode.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;

namespace JeremyTCD.DocFx.Plugins.TocEmbedder
{
    [Export(nameof(TocEmbedder), typeof(IPostProcessor))]
    public class TocEmbedder : IPostProcessor
    {
        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            // Do nothing
            return metadata;
        }

        // TOCs require extra round trips and front end logic. This method embeds them when generating the page.
        public Manifest Process(Manifest manifest, string outputFolder)
        {
            if (outputFolder == null)
            {
                throw new ArgumentNullException("Base directory cannot be null");
            }

            foreach (ManifestItem manifestItem in manifest.Files)
            {
                if (manifestItem.DocumentType != "Conceptual")
                {
                    continue;
                }

                // Get document path
                string documentRelPath = manifestItem.GetHtmlOutputRelPath();
                Uri documentBaseUri = new Uri(outputFolder + "\\");
                Uri documentAbsUri = new Uri(documentBaseUri, documentRelPath);
                string documentPath = documentAbsUri.AbsolutePath.Replace(".html", "");

                // Get document Node
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.Load(documentAbsUri.AbsolutePath, Encoding.UTF8);
                HtmlNode documentNode = htmlDoc.DocumentNode;

                // Navbar
                // Get navbar path
                HtmlNode metaNavNode = documentNode.SelectSingleNode("//meta[@property='docfx:navrel']");
                string navRelPath = metaNavNode.GetAttributeValue("content", null);
                metaNavNode.Remove();
                Uri navAbsUri;
                if (navRelPath.StartsWith("/")) // Root of file scheme is the drive, we want the _site folder to be the root
                {
                    navAbsUri = new Uri(documentBaseUri, navRelPath.Substring(1));
                }
                else
                {
                    navAbsUri = new Uri(documentAbsUri, navRelPath);
                }

                // Get navbar
                HtmlDocument navHtmlDoc = new HtmlDocument();
                navHtmlDoc.Load(navAbsUri.AbsolutePath, Encoding.UTF8);
                HtmlNode navDocumentNode = navHtmlDoc.DocumentNode;

                // Clean hrefs and set active category
                HtmlNodeCollection navAnchorNodes = navDocumentNode.SelectNodes("//a");
                CleanHrefs(navAnchorNodes, documentBaseUri, navAbsUri);
                SetActive(navAnchorNodes, documentPath, documentAbsUri, documentBaseUri, true);

                // Add navbar to page
                HtmlNode navbarNode = documentNode.SelectSingleNode("//*[@id='page-header-navbar']");
                navbarNode.AppendChild(navDocumentNode);

                // Category Menu
                manifestItem.Metadata.TryGetValue("mimo_disableCategoryMenu", out object disableCategoryMenu);
                HtmlNode metaTocNode = documentNode.SelectSingleNode("//meta[@property='docfx:tocrel']");
                metaTocNode?.Remove();
                if (disableCategoryMenu as bool? != true)
                {
                    // Get TOC path
                    manifestItem.Metadata.TryGetValue("mimo_toc", out object toc);
                    if (!(toc is string tocRelPath))
                    {
                        tocRelPath = metaTocNode.GetAttributeValue("content", null);
                    }
                    else
                    {
                        tocRelPath = tocRelPath.Replace(".yml", ".html");
                    }
                    Uri tocAbsUri;
                    if (tocRelPath.StartsWith("/")) // Root of file scheme is the drive, we want the _site folder to be the root
                    {
                        tocAbsUri = new Uri(documentBaseUri, tocRelPath.Substring(1));
                    }
                    else
                    {
                        tocAbsUri = new Uri(documentAbsUri, tocRelPath);
                    }

                    // Get TOC
                    HtmlDocument tocHtmlDoc = new HtmlDocument();
                    tocHtmlDoc.Load(tocAbsUri.AbsolutePath, Encoding.UTF8);
                    HtmlNode tocDocumentNode = tocHtmlDoc.DocumentNode;

                    // Insert SVGs
                    HtmlNode buttonNode = tocHtmlDoc.CreateElement("button");
                    HtmlNode svgNode = tocHtmlDoc.CreateElement("svg");
                    HtmlNode useNode = tocHtmlDoc.CreateElement("use");
                    useNode.SetAttributeValue("xlink:href", "#material-design-chevron-right");
                    svgNode.AppendChild(useNode);
                    buttonNode.AppendChild(svgNode);
                    HtmlNodeCollection iconRequiringNodes = tocDocumentNode.SelectNodes("//li[@class='expandable']/a|//li[@class='expandable']/span");
                    if (iconRequiringNodes != null)
                    {
                        foreach (HtmlNode htmlNode in iconRequiringNodes)
                        {
                            htmlNode.PrependChild(buttonNode.Clone());
                        }
                    }

                    // Clean hrefs and set active category
                    HtmlNodeCollection tocAnchorNodes = tocDocumentNode.SelectNodes("//a");
                    CleanHrefs(tocAnchorNodes, documentBaseUri, tocAbsUri);
                    SetActive(tocAnchorNodes, documentPath, documentAbsUri, documentBaseUri);

                    // Add TOC to page
                    HtmlNode categoryPagesNode = documentNode.SelectSingleNode("//*[@id='category-pages']");
                    categoryPagesNode.AppendChild(tocDocumentNode);

                    // Breadcrumbs
                    List<(string, string, string)> breadcrumbs = new List<(string, string, string)>();

                    // Traverse up from active anchor, retrieve text from each one
                    HtmlNode currentNode = tocDocumentNode.SelectSingleNode("//*[@class='active']").ParentNode;
                    while (currentNode.Name != "nav")
                    {
                        if (currentNode.Name == "li")
                        {
                            HtmlNode textNode = currentNode.SelectSingleNode("a|span");
                            breadcrumbs.Add((textNode.Name, textNode.InnerText.Trim(), textNode.GetAttributeValue("href", null)));
                        }
                        currentNode = currentNode.ParentNode;
                    }

                    // Get current category name
                    HtmlNode activeNavbarAnchor = navbarNode.SelectSingleNode("//*[@class='active']");
                    if (activeNavbarAnchor != null)
                    {
                        string href = activeNavbarAnchor.GetAttributeValue("href", null);
                        if (breadcrumbs.Last().Item3 != href) { // E.g navbar anchor is ./articles and toc topmost active anchor is also ./articles, redundant to list it twice

                            breadcrumbs.Add(("a", activeNavbarAnchor.InnerText.Trim(), href));
                        }
                    }

                    // Create UL
                    HtmlNode ulElement = htmlDoc.CreateElement("ul");
                    foreach ((string elementName, string text, string href) in breadcrumbs)
                    {
                        HtmlNode liElement = htmlDoc.CreateElement("li");
                        HtmlNode textElement = htmlDoc.CreateElement(elementName);
                        textElement.InnerHtml = text;
                        if (href != null)
                        {
                            textElement.SetAttributeValue("href", href);
                        }
                        liElement.AppendChild(textElement);

                        ulElement.PrependChild(liElement);
                    }

                    // Add breadcrumbs to page
                    HtmlNode breadcrumbsNode = documentNode.SelectSingleNode("//nav[@id='category-menu-header']");
                    breadcrumbsNode.PrependChild(ulElement);
                }

                // Save changes
                htmlDoc.Save(documentAbsUri.AbsolutePath);
            }

            return manifest;
        }

        // Clean Hrefs (basically, makes hrefs absolute so AbsolutePathResolver can clean them up)
        private void CleanHrefs(HtmlNodeCollection anchorNodes, Uri documentBaseUri, Uri tocAbsUri)
        {
            foreach (HtmlNode anchorNode in anchorNodes)
            {
                string hrefRelToToc = anchorNode.GetAttributeValue("href", null);

                // If href is already an absolute URL, continue
                if (Uri.TryCreate(hrefRelToToc, UriKind.Absolute, out Uri testHrefRelToToc)
                    && (testHrefRelToToc.Scheme == "http" || testHrefRelToToc.Scheme == "https"))
                {
                    continue;
                }

                Uri hrefAbsUri = new Uri(tocAbsUri, hrefRelToToc);
                Uri hrefRelDocumentBaseUri = documentBaseUri.MakeRelativeUri(hrefAbsUri);
                anchorNode.SetAttributeValue("href", "/" + hrefRelDocumentBaseUri);
            }
        }

        private void SetActive(HtmlNodeCollection anchorNodes, string documentPath, Uri documentAbsUri, Uri documentBaseUri, bool matchDir = false)
        {
            foreach (HtmlNode anchorNode in anchorNodes)
            {
                // TODO What to do if href is absolute?
                string href = anchorNode.GetAttributeValue("href", null);
                Uri pageAbsUri;
                if (href.StartsWith("/")) // Root of file scheme is the drive, we want the _site folder to be the root
                {
                    pageAbsUri = new Uri(documentBaseUri, href.Substring(1));
                }
                else
                {
                    pageAbsUri = new Uri(documentAbsUri, href);
                }

                if (pageAbsUri.AbsolutePath == documentPath) // Direct match, anchor refers to the document
                {
                    anchorNode.SetAttributeValue("class", "active");
                    break;
                }

                if (!matchDir) // We only want direct matches, continue
                {
                    continue;
                }

                // TODO consider closes match?
                if (documentPath.StartsWith(pageAbsUri.AbsolutePath)) // E.g pageAbsUri is <site>/articles, document is <site>/articles/my-article. 
                {
                    anchorNode.SetAttributeValue("class", "active");
                    break;
                }
            }
        }
    }
}
