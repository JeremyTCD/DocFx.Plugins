using HtmlAgilityPack;
using JeremyTCD.DocFx.Plugins.Utils;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.MarkdownLite;
using Microsoft.DocAsCode.Plugins;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace JeremyTCD.DocFx.Plugins.SearchIndexGenerator
{
    [Export(nameof(SearchIndexGenerator), typeof(IPostProcessor))]
    public class SearchIndexGenerator : IPostProcessor
    {
        private static readonly Regex RegexWhiteSpace = new Regex(@"\s+", RegexOptions.Compiled);

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

            Dictionary<string, SearchIndexItem> SearchIndexItems = GetSearchIndexItems(outputFolder, manifest);
            if (SearchIndexItems.Count == 0)
            {
                return manifest;
            }

            // Sort dictionary so json is produced deterministically
            SortedDictionary<string, SearchIndexItem> sortedSearchIndexItems = new SortedDictionary<string, SearchIndexItem>(SearchIndexItems);

            // Create file name
            string json = JsonConvert.SerializeObject(sortedSearchIndexItems, Formatting.Indented);
            MD5 md5 = MD5.Create();
            byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(json));
            StringBuilder sb = new StringBuilder();
            foreach (byte bytes in hashBytes)
            {
                sb.Append(bytes.ToString("X2"));
            }
            string hash = sb.ToString();
            string fileName = string.Format(SearchIndexConstants.IndexFileNameFormat, hash);
            string indexFile = Path.Combine(outputFolder, "resources", fileName);

            // Add link element to conceptual documents
            string relativePath = PathUtility.MakeRelativePath(outputFolder, indexFile);
            foreach (ManifestItem manifestItem in manifest.Files)
            {
                if (manifestItem.DocumentType != "Conceptual")
                {
                    continue;
                }

                HtmlDocument htmlDoc = manifestItem.GetHtmlOutputDoc(outputFolder);
                HtmlNode headElement = htmlDoc.DocumentNode.SelectSingleNode("//head");
                HtmlNode linkElement = htmlDoc.CreateElement("link");
                linkElement.SetAttributeValue("rel", "preload");
                linkElement.SetAttributeValue("href", "/" + relativePath);
                linkElement.SetAttributeValue("as", "fetch");
                linkElement.SetAttributeValue("type", "application/json");
                linkElement.SetAttributeValue("crossorigin", "anonymous");
                headElement.AppendChild(linkElement);
                string relPath = manifestItem.GetHtmlOutputRelPath();
                File.WriteAllText(Path.Combine(outputFolder, relPath), htmlDoc.DocumentNode.OuterHtml);
            }

            // Write to disk
            OutputSearchIndex(relativePath, indexFile, manifest, json);

            return manifest;
        }

        private void OutputSearchIndex(string relativePath, string indexFile, Manifest manifest, string json)
        {
            Directory.CreateDirectory(Directory.GetParent(indexFile).FullName);
            File.WriteAllText(indexFile, json);

            var manifestItem = new ManifestItem
            {
                DocumentType = "Resource",
                Metadata = new Dictionary<string, object>(),
            };
            manifestItem.OutputFiles.Add("resource", new OutputFileInfo
            {
                RelativePath = PathUtility.MakeRelativePath(relativePath, indexFile),
            });

            manifest.Files?.Add(manifestItem);
        }

        private Dictionary<string, SearchIndexItem> GetSearchIndexItems(string outputFolder, Manifest manifest)
        {
            Dictionary<string, SearchIndexItem> SearchIndexItems = new Dictionary<string, SearchIndexItem>();

            foreach (ManifestItem manifestItem in manifest.Files)
            {
                if (manifestItem.DocumentType != "Conceptual")
                {
                    continue;
                }

                manifestItem.Metadata.TryGetValue(SearchIndexConstants.IncludeInSearchIndexKey, out object includeInSearchIndex);
                if (includeInSearchIndex as bool? == false)
                {
                    continue;
                }

                string relPath = manifestItem.GetHtmlOutputRelPath();
                HtmlNode articleNode = manifestItem.GetHtmlOutputArticleNode(outputFolder);
                StringBuilder stringBuilder = new StringBuilder();
                ExtractTextFromNode(articleNode, stringBuilder);
                string text = NormalizeNodeText(stringBuilder.ToString());

                manifestItem.Metadata.TryGetValue(SearchIndexConstants.SearchIndexSnippetLengthKey, out object length);
                int searchIndexSnippetLength = length as int? ?? SearchIndexConstants.DefaultArticleSnippetLength;

                HtmlNode snippet = SnippetCreator.CreateSnippet(articleNode, relPath);//, searchIndexSnippetLength);

                SearchIndexItems.Add(relPath, new SearchIndexItem
                {
                    RelPath = relPath,
                    SnippetHtml = snippet.OuterHtml,
                    Text = text,
                    Title = snippet.SelectSingleNode(".//h1[contains(@class, 'title')]/a").InnerText
                });
            }

            return SearchIndexItems;
        }

        private string NormalizeNodeText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            text = StringHelper.HtmlDecode(text);
            return RegexWhiteSpace.Replace(text, " ").Trim();
        }

        private void ExtractTextFromNode(HtmlNode node, StringBuilder stringBuilder)
        {
            // Note: Article's title is included separately in SearchIndexItem.Title
            if (node.Name == "h1")
            {
                return;
            }

            if (!node.HasChildNodes)
            {
                stringBuilder.Append(node.InnerText);
                stringBuilder.Append(" ");
            }
            else
            {
                foreach (HtmlNode childNode in node.ChildNodes)
                {
                    ExtractTextFromNode(childNode, stringBuilder);
                }
            }
        }
    }
}
