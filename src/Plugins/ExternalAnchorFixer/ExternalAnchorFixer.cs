using HtmlAgilityPack;
using JeremyTCD.DocFx.Plugins.Utils;
using Microsoft.DocAsCode.Plugins;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;

namespace JeremyTCD.DocFx.Plugins.ExternalAnchorFixer
{
    // Links to external sites should open new tabs when clicked.
    [Export(nameof(ExternalAnchorFixer), typeof(IPostProcessor))]
    public class ExternalAnchorFixer : IPostProcessor
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

                // Get HtmlDocument
                HtmlDocument htmlDoc = manifestItem.GetHtmlOutputDoc(outputFolder);

                // Get base url
                string baseUrl = manifestItem.Metadata["mimo_baseUrl"] as string;

                // Update all anchors that have absolute hrefs pointing at external sites to open in separate tabs.
                // Make an exception for anchors with absolute hrefs pointing to internal URLs but with the new-tab class.
                foreach (HtmlNode htmlNode in htmlDoc.DocumentNode.SelectNodes("//a[starts-with(@href, 'http')]"))
                {
                    string[] classes = htmlNode.GetAttributeValue("class", null)?.Split(' ');
                    bool forceNewTab = classes?.Any(c => c == "new-tab") ?? false;

                    if (forceNewTab)
                    {
                        string newClasses = string.Join(" ", classes.Where(s => s != "new-tab"));
                        htmlNode.SetAttributeValue("class", newClasses);
                    }
                    else if (htmlNode.GetAttributeValue("href", null).StartsWith(baseUrl))
                    {
                        continue;
                    }

                    htmlNode.SetAttributeValue("target", "_blank");
                    // Prevents malicious sites from manipulating the window object https://mathiasbynens.github.io/rel-noopener/#hax
                    htmlNode.SetAttributeValue("rel", "noopener");
                }

                File.WriteAllText(Path.Combine(outputFolder, manifestItem.GetHtmlOutputRelPath()), htmlDoc.DocumentNode.OuterHtml);
            }

            return manifest;
        }
    }
}
