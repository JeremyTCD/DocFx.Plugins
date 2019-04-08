using HtmlAgilityPack;
using JeremyTCD.DocFx.Plugins.Utils;
using Microsoft.DocAsCode.Plugins;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;

namespace JeremyTCD.DocFx.Plugins.OutlineGenerator
{
    [Export(nameof(OutlineGenerator), typeof(IPostProcessor))]
    public class OutlineGenerator : IPostProcessor
    {
        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            // Do nothing
            return metadata;
        }

        // If article menu is enabled, generates outline and inserts it
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

                manifestItem.Metadata.TryGetValue("mimo_disableArticleMenu", out object disableArticleMenu);
                if (disableArticleMenu as bool? == true)
                {
                    continue;
                }

                // Get document Node
                HtmlNode documentNode = manifestItem.
                    GetHtmlOutputDoc(outputFolder).
                    DocumentNode;

                // Get main article node
                HtmlNode mainArticleNode = documentNode.SelectSingleNode($"//article[@class='{UtilsConstants.MainArticleClasses}']");

                // Generate outline tree for article
                OutlineNode rootOutlineNode = new OutlineNode();
                GenerateOutlineTree(mainArticleNode, rootOutlineNode);

                // Render outline tree
                var outlineHtmlDoc = new HtmlDocument();
                HtmlNode rootULElement = outlineHtmlDoc.CreateElement("ul");
                GenerateOutlineNodes(rootULElement, rootOutlineNode, outlineHtmlDoc);

                // Insert title
                HtmlNode articleMenuContentElement = documentNode.SelectSingleNode("//*[@class='article-menu__content dropdown__body']");

                // Scrollable indicators
                HtmlNode level2ULElement = rootULElement.SelectSingleNode("li/ul");
                if (level2ULElement != null) // If there is no level2ULElement, there's no scrolling
                {
                    // Wrap level 2 ul element in a div together with indicators, insert wrapper as child of level 2 ul element's parent
                    level2ULElement.SetAttributeValue("class", "scrollable-indicators__scrollable outline__scrollable");
                    level2ULElement.Remove();
                    HtmlNode scrollableIndicatorsNode = outlineHtmlDoc.CreateElement("div");
                    scrollableIndicatorsNode.SetAttributeValue("class", "scrollable-indicators");
                    HtmlNode startIndicatorElement = outlineHtmlDoc.CreateElement("div");
                    startIndicatorElement.SetAttributeValue("class", "scrollable-indicators__indicator scrollable-indicators__indicator--vertical-start");
                    HtmlNode endIndicatorElement = outlineHtmlDoc.CreateElement("div");
                    endIndicatorElement.SetAttributeValue("class", "scrollable-indicators__indicator scrollable-indicators__indicator--vertical-end");
                    scrollableIndicatorsNode.AppendChild(level2ULElement);
                    scrollableIndicatorsNode.AppendChild(startIndicatorElement);
                    scrollableIndicatorsNode.AppendChild(endIndicatorElement);
                    HtmlNode level1LIElement = rootULElement.SelectSingleNode("li");
                    level1LIElement.AppendChild(scrollableIndicatorsNode);
                }

                // Insert outline tree
                HtmlNode outlineNode = outlineHtmlDoc.CreateElement("div");
                outlineNode.SetAttributeValue("class", "article-menu__outline outline");
                outlineNode.AppendChild(rootULElement);
                articleMenuContentElement.AppendChild(outlineNode);

                // Save
                string relPath = manifestItem.GetHtmlOutputRelPath();
                File.WriteAllText(Path.Combine(outputFolder, relPath), documentNode.OuterHtml);
            }

            return manifest;
        }

        private void GenerateOutlineNodes(HtmlNode ulElement, OutlineNode outlineNodes, HtmlDocument outlineHtmlDoc)
        {
            foreach (OutlineNode childOutlineNode in outlineNodes.Children)
            {
                HtmlNode liElement = outlineHtmlDoc.CreateElement("li");
                if(childOutlineNode.Level == 1)
                {
                    liElement.SetAttributeValue("class", "outline__root-node");
                }
                HtmlNode aElement = outlineHtmlDoc.CreateElement("a");
                aElement.SetAttributeValue("href", childOutlineNode.Href);
                HtmlNode spanElement = outlineHtmlDoc.CreateElement("span");
                spanElement.InnerHtml = childOutlineNode.Content;
                aElement.AppendChild(spanElement);
                liElement.AppendChild(aElement);
                ulElement.AppendChild(liElement);
                ulElement.SetAttributeValue("style", $"--level: {childOutlineNode.Level}");

                if (childOutlineNode.Children.Count > 0)
                {
                    HtmlNode childULElement = outlineHtmlDoc.CreateElement("ul");
                    liElement.AppendChild(childULElement);

                    GenerateOutlineNodes(childULElement, childOutlineNode, outlineHtmlDoc);
                }
            }
        }

        private OutlineNode GenerateOutlineTree(HtmlNode sectioningContentNode, OutlineNode parentOutlineNode)
        {
            OutlineNode outlineNode = null;

            foreach (HtmlNode childNode in sectioningContentNode.ChildNodes)
            {
                if (childNode.Name == "header")
                {
                    HtmlNode headingElement = childNode.SelectSingleNode("*");
                    int level = headingElement.Name[1] - 48; // http://www.asciitable.com/
                    outlineNode = new OutlineNode
                    {
                        Content = headingElement.InnerText,
                        Level = level,
                        Href = "#" + sectioningContentNode.Id
                    };
                    parentOutlineNode.Children.Add(outlineNode);
                }

                // Intentionally skips sub trees since they do not contribute to the document's outline. Sub trees are children of 
                // sectioning content roots other than section and article.
                if (childNode.Name == "section" && outlineNode?.Level < 3) // Don't include h4s, h5s and h6s
                {
                    GenerateOutlineTree(childNode, outlineNode);
                }
            }

            return null;
        }
    }
}
