using HtmlAgilityPack;
using JeremyTCD.DocFx.Plugins.Utils;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Plugins;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace JeremyTCD.DocFx.Plugins.SearchIndexGenerator
{
    [Export(nameof(SearchIndexGenerator), typeof(IPostProcessor))]
    public class SearchIndexGenerator : IPostProcessor
    {
        private static readonly Regex _regexWhiteSpace = new Regex(@"\s+", RegexOptions.Compiled);

        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        // Blocks that're inline by default - https://developer.mozilla.org/en-US/docs/Web/HTML/Inline_elements.
        // We use these to lump article text up according to the "block" element they're contained in.
        // This approach isn't perfect because "inline" elements may be used as blocks, e.g <a> element can contain flow content.
        // Nonetheless, this methods provides better search results than not bothering to split text up at all.
        private static readonly List<string> _inlineElementNames = new List<string>
        {
            "abbr", "acronym", "audio", "b", "bdi", "bdo", "big", "br", "button", "canvas", "cite", "code", "data", "datalist", "del", "dfn",
            "em", "embed", "i", "iframe", "img", "input", "ins", "kbd", "label", "map", "mark", "meter", "noscript", "object", "output", "picture",
            "progress", "q", "ruby", "s", "samp", "script", "select", "slot", "small", "span", "strong", "sub", "sup", "svg", "template", "textarea",
            "time", "u", "tt", "var", "video", "wbr"
        };

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

            List<SearchIndexItem> searchIndexItems = GetSearchIndexItems(outputFolder, manifest);
            if (searchIndexItems.Count == 0)
            {
                return manifest;
            }
            searchIndexItems.Sort(); // We're just sorting for deterministic output, so we use the simplest comparer

            // Create file name
            string indexFile = Path.Combine(outputFolder, "resources", "index.json");

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
                HtmlNode linkElement = htmlDoc.CreateElement("meta");
                // Recommendation is to "register a new link type" if content is a URL - https://html.spec.whatwg.org/multipage/semantics.html#other-metadata-names.
                // Seems like bad advice, only a couple of organizations have applied what about the millions of lesser use cases?
                linkElement.SetAttributeValue("name", "mimo-search-index");
                linkElement.SetAttributeValue("content", "/" + relativePath);
                headElement.AppendChild(linkElement);
                string relPath = manifestItem.GetHtmlOutputRelPath();
                File.WriteAllText(Path.Combine(outputFolder, relPath), htmlDoc.DocumentNode.OuterHtml);
            }

            // Write to disk
            string json = JsonConvert.SerializeObject(searchIndexItems, Formatting.Indented, _jsonSerializerSettings);
            OutputSearchIndex(relativePath, indexFile, manifest, json);

            return manifest;
        }

        private void OutputSearchIndex(string relativePath, string indexFile, Manifest manifest, string json)
        {
            string directory = Directory.GetParent(indexFile).FullName;
            Directory.CreateDirectory(directory); // If not created, create

            // Delete existing index
            foreach (string file in Directory.GetFiles(directory, "index.*json"))
            {
                File.Delete(file);
            }

            // Create new index
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

        private List<SearchIndexItem> GetSearchIndexItems(string outputFolder, Manifest manifest)
        {
            List<SearchIndexItem> SearchIndexItems = new List<SearchIndexItem>();

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

                // Text
                HtmlNode articleNode = manifestItem.GetHtmlOutputArticleNode(outputFolder);
                articleNode.SelectSingleNode("//h1")?.Remove(); // Remove title
                articleNode.SelectSingleNode("//div[@class='article__metadata metadata']")?.Remove(); // Remove metadata
                foreach (HtmlNode commentNode in articleNode.SelectNodes("//comment()"))
                {
                    commentNode.Remove();
                }
                Stack<string> text = new Stack<string>();
                ExtractTextFromNode(articleNode, text, false);

                // Date
                manifestItem.Metadata.TryGetValue("mimo_date", out object date);

                // Title
                manifestItem.Metadata.TryGetValue("mimo_pageTitle", out object title);

                // Description
                manifestItem.Metadata.TryGetValue("mimo_pageDescription", out object description);

                List<string> textList = text.ToList();
                textList.Reverse();
                SearchIndexItems.Add(new SearchIndexItem
                {
                    Title = title as string,
                    RelPath = "/" + manifestItem.GetHtmlOutputRelPath().Replace(".html", ""),
                    Date = date as string,
                    Text = textList,
                    Description = description as string
                });
            }

            return SearchIndexItems;
        }

        private string NormalizeNodeText(string text)
        {
            text = HttpUtility.HtmlDecode(text);
            return _regexWhiteSpace.Replace(text, " ").Trim();
        }

        // We split text up by block to avoid search results like "Version A version is a snapshot of a piece of software."
        // where "Version" is actually the header for "A version is a snapshot of a piece of software."
        // Instead we get "Version... A version is a snapshot of a piece of software.".
        private void ExtractTextFromNode(HtmlNode node, Stack<string> text, bool append)
        {
            string blockText = append ? text.Pop() + " " : null; // We should pool StringBuilders
            bool previousBlockNodeIsPara = false;
            foreach (HtmlNode childNode in node.ChildNodes)
            {
                if (childNode.NodeType == HtmlNodeType.Text || IsInlineElement(childNode)) // Inline node or text
                {
                    string inlineText = childNode.InnerText;
                    if (inlineText != null)
                    {
                        blockText += childNode.InnerText;
                    }
                }
                else // Block node
                {
                    bool childNodeIsPara = childNode.Name == "p";
                    ExtractTextFromNode(childNode, text, previousBlockNodeIsPara && childNodeIsPara); // Combine consecutive paragraphs
                    previousBlockNodeIsPara = childNodeIsPara;
                }
            }

            if (!string.IsNullOrWhiteSpace(blockText))
            {
                text.Push(NormalizeNodeText(blockText));
            }
        }

        private bool IsInlineElement(HtmlNode node)
        {
            string name = node.Name;
            if (name == "a")
            {
                // If anchor contains any non inline elements, we treat the element as a block element
                foreach (HtmlNode childNode in node.ChildNodes)
                {
                    if (childNode.NodeType == HtmlNodeType.Element &&
                        _inlineElementNames.BinarySearch(childNode.Name) < 0)
                    {
                        return false;
                    }
                }
            }
            else if (_inlineElementNames.BinarySearch(name) < 0)
            {
                return false;
            }

            return true;
        }
    }
}
