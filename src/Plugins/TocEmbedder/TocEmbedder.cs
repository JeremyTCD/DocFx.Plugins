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
                HtmlNode metaNavbarNode = documentNode.SelectSingleNode("//meta[@property='docfx:navrel']");
                string navbarRelPath = metaNavbarNode.GetAttributeValue("content", null);
                metaNavbarNode.Remove();
                // Root of file scheme is the drive, we want the _site folder to be the root
                Uri navbarAbsUri = navbarRelPath.StartsWith("/") ? new Uri(documentBaseUri, navbarRelPath.Substring(1)) : new Uri(documentAbsUri, navbarRelPath);

                // Get navbar
                HtmlDocument navbarHtmlDoc = new HtmlDocument();
                navbarHtmlDoc.Load(navbarAbsUri.AbsolutePath, Encoding.UTF8);
                HtmlNode navbarDocumentNode = navbarHtmlDoc.DocumentNode;

                // Set navbar classes
                navbarDocumentNode.SelectSingleNode("ul").SetAttributeValue("class", "navbar__list");
                foreach (HtmlNode navbarItemNode in navbarDocumentNode.SelectNodes("/ul/li"))
                {
                    navbarItemNode.SetAttributeValue("class", "navbar__item");
                }

                // Clean hrefs, set classes and set active category
                HtmlNodeCollection navbarAnchorNodes = navbarDocumentNode.SelectNodes("//a");
                foreach (HtmlNode navbarAnchorNode in navbarAnchorNodes)
                {
                    navbarAnchorNode.SetAttributeValue("class", "navbar__link");
                }
                CleanHrefs(navbarAnchorNodes, documentBaseUri, navbarAbsUri);
                HtmlNode activeNavbarNode = GetActiveNode(navbarAnchorNodes, documentPath, documentAbsUri, documentBaseUri, true);
                activeNavbarNode?.SetAttributeValue("class", "active");

                // Add navbar to page
                HtmlNode navbarNode = documentNode.SelectSingleNode("//*[@class='page-header__content dropdown__body']");
                HtmlNode navbarWrapper = navbarHtmlDoc.CreateElement("nav");
                navbarWrapper.SetAttributeValue("class", "page-header__navbar navbar");
                navbarWrapper.AppendChild(navbarDocumentNode);
                navbarNode.PrependChild(navbarWrapper);

                // Category Menu
                HtmlNode metaCatMenuNode = documentNode.SelectSingleNode("//meta[@property='docfx:tocrel']");
                metaCatMenuNode?.Remove();

                // Get cat menu path
                manifestItem.Metadata.TryGetValue("mimo_toc", out object catMenuRelPathRaw);
                if (!(catMenuRelPathRaw is string catMenuRelPath))
                {
                    catMenuRelPath = metaCatMenuNode.GetAttributeValue("content", null);
                }
                else
                {
                    catMenuRelPath = catMenuRelPath.Replace(".yml", ".html");
                }
                // Root of file scheme is the drive, we want the _site folder to be the root
                Uri catMenuAbsUri = catMenuRelPath.StartsWith("/") ? new Uri(documentBaseUri, catMenuRelPath.Substring(1)) : new Uri(documentAbsUri, catMenuRelPath);

                // Breadcrumbs
                manifestItem.Metadata.TryGetValue("mimo_disableBreadcrumbs", out object disableBreadcrumbsRaw);
                manifestItem.Metadata.TryGetValue("mimo_disableCategoryMenu", out object disableCategoryMenuRaw);
                bool disableCategoryMenu = disableCategoryMenuRaw as bool? == true; // false if unspecified
                bool disableBreadcrumbs = disableBreadcrumbsRaw as bool? == true; // false if unspecified
                bool createBreadcrumbs = !disableCategoryMenu || !disableBreadcrumbs;
                List<(string, string, string)> breadcrumbs = createBreadcrumbs ? new List<(string, string, string)>() : null;

                if (catMenuAbsUri.AbsoluteUri != navbarAbsUri.AbsoluteUri) // If they're equal, current document does not display a category menu (navbar is never the category menu of a document)
                {
                    // Get cat menu
                    HtmlDocument catMenuHtmlDoc = new HtmlDocument();
                    catMenuHtmlDoc.Load(catMenuAbsUri.AbsolutePath, Encoding.UTF8);
                    HtmlNode catMenuDocumentNode = catMenuHtmlDoc.DocumentNode;

                    // Convert cat menu into a collapsible menu
                    // Create master buttonNode
                    HtmlNode masterButtonNode = catMenuHtmlDoc.CreateElement("button");
                    masterButtonNode.SetAttributeValue("title", "Expand category");
                    masterButtonNode.SetAttributeValue("aria-label", "Expand category");
                    HtmlNode masterSvgNode = catMenuHtmlDoc.CreateElement("svg");
                    masterSvgNode.SetAttributeValue("class", "collapsible-menu__expand-icon");
                    HtmlNode masterUseNode = catMenuHtmlDoc.CreateElement("use");
                    masterUseNode.SetAttributeValue("xlink:href", "#material-design-chevron-right");
                    masterSvgNode.AppendChild(masterUseNode);
                    masterButtonNode.AppendChild(masterSvgNode);
                    HtmlNode divNodeMaster = catMenuHtmlDoc.CreateElement("div");
                    divNodeMaster.SetAttributeValue("class", "collapsible-menu__hover-area");
                    HtmlNode divNodeWithButtonMaster = divNodeMaster.Clone();
                    divNodeWithButtonMaster.AppendChild(masterButtonNode);

                    // Set classes and add button
                    foreach (HtmlNode catMenuLINode in catMenuDocumentNode.SelectNodes("//li"))
                    {
                        HtmlNode ulNode = catMenuLINode.SelectSingleNode("./ul");
                        if (ulNode != null)
                        {
                            ulNode.SetAttributeValue("class", "collapsible-menu__inner-list");
                            catMenuLINode.SetAttributeValue("class", "collapsible-menu__node collapsible-menu__node_expandable");
                            HtmlNode divNode = divNodeWithButtonMaster.Clone();
                            HtmlNode anchorNode = catMenuLINode.SelectSingleNode("./a");

                            if (anchorNode != null)
                            {
                                anchorNode.SetAttributeValue("class", "collapsible-menu__node-content collapsible-menu__node-content_link");
                                divNode.SetAttributeValue("class", "collapsible-menu__hover-area collapsible-menu__hover-area_contains-link");
                                anchorNode.Remove();
                                divNode.AppendChild(anchorNode);
                            }
                            else
                            {
                                HtmlNode spanNode = catMenuLINode.SelectSingleNode("./span");
                                spanNode.SetAttributeValue("class", "collapsible-menu__node-content collapsible-menu__node-content_text");
                                spanNode.Remove();
                                divNode.AppendChild(spanNode);
                            }
                            catMenuLINode.PrependChild(divNode);
                        }
                        else
                        {
                            catMenuLINode.SetAttributeValue("class", "collapsible-menu__node");
                            HtmlNode anchorNode = catMenuLINode.SelectSingleNode("./a");
                            anchorNode.SetAttributeValue("class", "collapsible-menu__node-content collapsible-menu__node-content_link");
                            anchorNode.Remove();
                            HtmlNode divNode = divNodeMaster.Clone();
                            divNode.SetAttributeValue("class", "collapsible-menu__hover-area collapsible-menu__hover-area_contains-link");
                            divNode.AppendChild(anchorNode);
                            catMenuLINode.AppendChild(divNode);
                        }
                    }

                    HtmlNodeCollection buttonNodes = catMenuDocumentNode.SelectNodes("//button");
                    if (buttonNodes != null)
                    {
                        foreach (HtmlNode buttonNode in buttonNodes)
                        {
                            buttonNode.SetAttributeValue("class", "collapsible-menu__expand-button");
                        }
                    }

                    // Clean hrefs and set active category
                    HtmlNodeCollection catMenuAnchorNodes = catMenuDocumentNode.SelectNodes("//a");
                    CleanHrefs(catMenuAnchorNodes, documentBaseUri, catMenuAbsUri);
                    HtmlNode activeCatMenuNode = GetActiveNode(catMenuAnchorNodes, documentPath, documentAbsUri, documentBaseUri);
                    if (activeCatMenuNode != null)
                    {
                        activeCatMenuNode.ParentNode.SetAttributeValue("class",
                            activeCatMenuNode.ParentNode.GetAttributeValue("class", null) + " collapsible-menu__hover-area_active");

                        // Traverse up from active anchor, retrieve text from each one and add class collapsible-menu__node_expanded if class collapsible-menu__node_expandable exists
                        HtmlNode currentNode = activeCatMenuNode.ParentNode;
                        while (currentNode.Name != "#document")
                        {
                            if (currentNode.Name == "li")
                            {
                                HtmlNode textNode = currentNode.SelectSingleNode("./a|./span|./div/a|./div/span");
                                breadcrumbs?.Add((textNode.Name, textNode.InnerText.Trim(), textNode.GetAttributeValue("href", null)));

                                string classValue = currentNode.GetAttributeValue("class", null);
                                string[] classes = classValue?.Split(' ');
                                if (classes?.Contains("collapsible-menu__node_expandable") == true)
                                {
                                    currentNode.SetAttributeValue("class", classValue + " collapsible-menu__node_expanded");
                                }
                            }
                            currentNode = currentNode.ParentNode;
                        }
                    }

                    // Add category menu pages to page
                    HtmlNode catMenuContentNode = documentNode.SelectSingleNode("//div[@class='category-menu__content dropdown__body']");
                    if (catMenuContentNode != null)
                    {
                        HtmlNode collapsibleMenuNode = catMenuHtmlDoc.CreateElement("div");
                        collapsibleMenuNode.SetAttributeValue("class", "collapsible-menu category-menu__collapsible-menu");
                        // Scrollable
                        HtmlNode scrollableNode = htmlDoc.CreateElement("div");
                        scrollableNode.SetAttributeValue("class", "collapsible-menu__scrollable-indicators scrollable-indicators scrollable-indicators_axis_vertical");
                        HtmlNode startIndicatorElement = htmlDoc.CreateElement("div");
                        startIndicatorElement.SetAttributeValue("class", "scrollable-indicators__indicator scrollable-indicators__indicator_start");
                        HtmlNode endIndicatorElement = htmlDoc.CreateElement("div");
                        endIndicatorElement.SetAttributeValue("class", "scrollable-indicators__indicator scrollable-indicators__indicator_end");
                        HtmlNode ulElement = catMenuDocumentNode.SelectSingleNode("/ul");
                        ulElement.SetAttributeValue("class", "scrollable-indicators__scrollable");
                        scrollableNode.AppendChild(ulElement);
                        scrollableNode.AppendChild(startIndicatorElement);
                        scrollableNode.AppendChild(endIndicatorElement);
                        collapsibleMenuNode.AppendChild(scrollableNode);
                        catMenuContentNode.AppendChild(collapsibleMenuNode);
                    }

                    // Get current category name
                    if (createBreadcrumbs)
                    {
                        HtmlNode activeNavbarAnchor = navbarNode.SelectSingleNode("//*[@class='active']");
                        if (activeNavbarAnchor != null)
                        {
                            string href = activeNavbarAnchor.GetAttributeValue("href", null);
                            string name = activeNavbarAnchor.InnerText.Trim();
                            breadcrumbs.Add(("a", name, href));
                        }
                    }
                }
                else if (createBreadcrumbs)
                {
                    // If catMenuRelPath == navRelPath, other than root, only 1 level in its breadcrumbs, e.g "Website Name | About"
                    manifestItem.Metadata.TryGetValue("mimo_pageTitle", out object pageTitle);
                    manifestItem.Metadata.TryGetValue("mimo_pageRelPath", out object pageRelPath);

                    breadcrumbs.Add(("a", Convert.ToString(pageTitle), Convert.ToString(pageRelPath)));
                }

                if (createBreadcrumbs)
                {
                    // Add root breadcrumb
                    manifestItem.Metadata.TryGetValue("mimo_websiteName", out object websiteName);
                    breadcrumbs.Add(("a", Convert.ToString(websiteName), "/"));

                    // Create breadcrumbs UL
                    HtmlNode ulElement = htmlDoc.CreateElement("ul");
                    ulElement.SetAttributeValue("class", "bar-separated-list__list scrollable-indicators__scrollable");
                    for (int i = 0; i < breadcrumbs.Count; i++)
                    {
                        (string elementName, string text, string href) = breadcrumbs[i];

                        HtmlNode liElement = htmlDoc.CreateElement("li");
                        liElement.SetAttributeValue("class", "bar-separated-list__item");
                        HtmlNode textElement = htmlDoc.CreateElement(elementName);
                        textElement.SetAttributeValue("class",
                            "bar-separated-list__item-content " + (elementName == "a" ? "bar-separated-list__item-content_link" : "bar-separated-list__item-content_text"));
                        textElement.InnerHtml = text;
                        if (href != null)
                        {
                            textElement.SetAttributeValue("href", href);
                        }
                        liElement.AppendChild(textElement);
                        ulElement.PrependChild(liElement);
                    }

                    HtmlNode barSeparatedListElement = htmlDoc.CreateElement("div");
                    barSeparatedListElement.SetAttributeValue("class", "bar-separated-list bar-separated-list_interactive");
                    HtmlNode scrollableNode = htmlDoc.CreateElement("div");
                    scrollableNode.SetAttributeValue("class", "bar-separated-list__scrollable-indicators scrollable-indicators scrollable-indicators_axis_horizontal");
                    HtmlNode startIndicatorElement = htmlDoc.CreateElement("div");
                    startIndicatorElement.SetAttributeValue("class", "scrollable-indicators__indicator scrollable-indicators__indicator_start");
                    HtmlNode endIndicatorElement = htmlDoc.CreateElement("div");
                    endIndicatorElement.SetAttributeValue("class", "scrollable-indicators__indicator scrollable-indicators__indicator_end");
                    scrollableNode.AppendChild(ulElement);
                    scrollableNode.AppendChild(startIndicatorElement);
                    scrollableNode.AppendChild(endIndicatorElement);
                    barSeparatedListElement.AppendChild(scrollableNode);

                    if (!disableCategoryMenu)
                    {
                        // Add breadcrumbs to page
                        HtmlNode categoryMenuHeaderNode = documentNode.SelectSingleNode("//nav[contains(@class,'category-menu')]/header");
                        HtmlNode barSeparatedListElementClone = barSeparatedListElement.CloneNode(true);
                        categoryMenuHeaderNode.PrependChild(barSeparatedListElement);

                    }

                    if (!disableBreadcrumbs)
                    {
                        HtmlNode breadcrumbsNode = documentNode.SelectSingleNode("//nav[@class='breadcrumbs']");
                        breadcrumbsNode?.AppendChild(barSeparatedListElement.CloneNode(true));
                    }
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

        private HtmlNode GetActiveNode(HtmlNodeCollection anchorNodes, string documentPath, Uri documentAbsUri, Uri documentBaseUri, bool matchDir = false)
        {
            foreach (HtmlNode anchorNode in anchorNodes)
            {
                // TODO What to do if href is absolute?
                string href = anchorNode.GetAttributeValue("href", null);
                // Root of file scheme is the drive, we want the _site folder to be the root
                Uri anchorNodeTargetUri = href.StartsWith("/") ? new Uri(documentBaseUri, href.Substring(1)) : new Uri(documentAbsUri, href);

                if (anchorNodeTargetUri.AbsolutePath == documentPath) // Direct match, anchor refers to the document
                {
                    return anchorNode;
                }
                else if (!matchDir) // We only want direct matches, continue
                {
                    continue;
                }

                // TODO what if some anchor node targets are parts of the same path, e.g <site>/articles/new/ and <site>/articles. Consider closes match?
                if (documentPath.StartsWith(anchorNodeTargetUri.AbsolutePath)) // E.g pageAbsUri is <site>/articles, document is <site>/articles/my-article. 
                {
                    return anchorNode;
                }
            }

            return null;
        }
    }
}
