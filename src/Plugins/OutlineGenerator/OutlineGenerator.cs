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
                rootULElement.SetAttributeValue("class", "article-menu__outline outline");
                GenerateOutlineNodes(rootULElement, rootOutlineNode, outlineHtmlDoc);

                // Insert title
                HtmlNode articleMenuContentElement = documentNode.SelectSingleNode("//*[@class='article-menu__content dropdown__body']");

                // Insert outline tree
                articleMenuContentElement.AppendChild(rootULElement);

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
