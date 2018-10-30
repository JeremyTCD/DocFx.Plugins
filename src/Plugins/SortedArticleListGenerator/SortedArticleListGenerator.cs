using HtmlAgilityPack;
using JeremyTCD.DocFx.Plugins.Utils;
using Microsoft.DocAsCode.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.IO;

namespace JeremyTCD.DocFx.Plugins.SortedArticleList
{
    [Export(nameof(SortedArticleListGenerator), typeof(IPostProcessor))]
    public class SortedArticleListGenerator : IPostProcessor
    {
        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            // Do nothing
            return metadata;
        }

        public Manifest Process(Manifest manifest, string outputFolder)
        {
            if (outputFolder == null)
            {
                throw new ArgumentNullException("Base directory cannot be null");
            }

            List<SortedArticleListItem> salItems = GetSalItems(outputFolder, manifest);
            if (salItems.Count == 0)
            {
                return manifest;
            }

            salItems.Sort((x, y) => DateTime.Compare(y.Date, x.Date));
            HtmlNode salNode = GenerateSalNode(salItems);
            InsertSalNode(outputFolder, manifest, salNode);

            return manifest;
        }

        private HtmlNode GenerateSalNode(List<SortedArticleListItem> salItems)
        {
            HtmlNode salNode = HtmlNode.CreateNode($"<div></div>");

            foreach (SortedArticleListItem salItem in salItems)
            {
                salNode.AppendChild(salItem.SnippetNode);
            }

            return salNode;
        }

        private void InsertSalNode(string outputFolder, Manifest manifest, HtmlNode salItemsNode)
        {
            foreach (ManifestItem manifestItem in manifest.Files)
            {
                if (manifestItem.DocumentType != "Conceptual")
                {
                    continue;
                }

                manifestItem.Metadata.TryGetValue(SortedArticleListConstants.EnableSalKey, out object enableSal);
                if (enableSal as bool? != true)
                {
                    continue;
                }

                string relPath = manifestItem.GetHtmlOutputRelPath();

                HtmlDocument htmlDoc = manifestItem.GetHtmlOutputDoc(outputFolder);
                HtmlNode salWrapperNode = htmlDoc.
                    DocumentNode.
                    SelectSingleNode($"//div[@id='{SortedArticleListConstants.SalAllItemsNodeId}']");
                if (salWrapperNode == null)
                {
                    throw new InvalidDataException($"{nameof(SortedArticleListGenerator)}: Html output {relPath} has no sorted article list all-items node");
                }
                salWrapperNode.AppendChildren(salItemsNode.ChildNodes);

                htmlDoc.Save(Path.Combine(outputFolder, relPath));
            }
        }

        private List<SortedArticleListItem> GetSalItems(string outputFolder, Manifest manifest)
        {
            List<SortedArticleListItem> salItems = new List<SortedArticleListItem>();

            foreach (ManifestItem manifestItem in manifest.Files)
            {
                if (manifestItem.DocumentType != "Conceptual")
                {
                    continue;
                }

                manifestItem.Metadata.TryGetValue(SortedArticleListConstants.IncludeInSalKey, out object includeInSal);
                if (includeInSal as bool? == false)
                {
                    continue;
                }

                manifestItem.Metadata.TryGetValue(SortedArticleListConstants.SalSnippetLengthKey, out object length);
                int salSnippetLength = length as int? ?? SortedArticleListConstants.DefaultSalSnippetLength;

                HtmlNode articleNode = manifestItem.GetHtmlOutputArticleNode(outputFolder);
                string relPath = manifestItem.GetHtmlOutputRelPath().Replace(".html", "");
                HtmlNode snippetNode = SnippetCreator.CreateSnippet(articleNode, relPath);//, salSnippetLength);

                DateTime date = default(DateTime);
                try
                {
                    date = DateTime.ParseExact(manifestItem.Metadata[SortedArticleListConstants.DateKey] as string,
                        // Info on custom date formats https://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.85).aspx
                        new string[] { "MMM d, yyyy", "d" },
                        DateTimeFormatInfo.InvariantInfo,
                        DateTimeStyles.AllowWhiteSpaces);
                }
                catch
                {
                    throw new InvalidDataException($"{nameof(SortedArticleListGenerator)}: Article {manifestItem.SourceRelativePath} has an invalid {SortedArticleListConstants.DateKey}");
                }

                salItems.Add(new SortedArticleListItem
                {
                    RelPath = relPath,
                    SnippetNode = snippetNode,
                    Date = date
                });
            }

            return salItems;
        }
    }
}
