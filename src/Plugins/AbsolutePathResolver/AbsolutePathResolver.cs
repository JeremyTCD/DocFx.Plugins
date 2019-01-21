using HtmlAgilityPack;
using JeremyTCD.DocFx.Plugins.Utils;
using Microsoft.DocAsCode.Plugins;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;

namespace JeremyTCD.DocFx.Plugins.AbsolutePathResolver
{
    // An absolute path reference (https://tools.ietf.org/html/rfc3986#section-4.2) is a relative path that begins with '/'.
    // Since we will be "nesting" sites, e.g hosting utilities at jering.tech/utilities/<utility name>, absolute path references for these
    // sites must be replaced with relative path references.
    [Export(nameof(AbsolutePathResolver), typeof(IPostProcessor))]
    public class AbsolutePathResolver : IPostProcessor
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

                // Find current manifest items relative path to the site's root
                string relPath = manifestItem.GetHtmlOutputRelPath();
                string prefix = null;

                // 404.html may be served at any url, so it must contain only absolute urls
                if (relPath == "404.html")
                {
                    prefix = manifestItem.Metadata["mimo_baseUrl"] as string;
                }
                else
                {
                    int numLevels = relPath.Count(x => x == '/');
                    if (numLevels == 0)
                    {
                        prefix = ".";
                    }
                    else
                    {
                        prefix = "..";
                        for (int i = 1; i < numLevels; i++)
                        {
                            prefix += "/..";
                        }
                    }
                }

                // Get HtmlDocument
                HtmlDocument htmlDoc = manifestItem.GetHtmlOutputDoc(outputFolder);

                // Update all href and src attributes
                foreach(HtmlNode htmlNode in htmlDoc.DocumentNode.SelectNodes("//*[starts-with(@href, '/')]"))
                {
                    string newHref = prefix + htmlNode.GetAttributeValue("href", null);
                    htmlNode.SetAttributeValue("href", newHref);
                }
                foreach (HtmlNode htmlNode in htmlDoc.DocumentNode.SelectNodes("//*[starts-with(@src, '/')]"))
                {
                    string newSrc = prefix + htmlNode.GetAttributeValue("src", null);
                    htmlNode.SetAttributeValue("src", newSrc);
                }

                File.WriteAllText(Path.Combine(outputFolder, relPath), htmlDoc.DocumentNode.OuterHtml);
            }

            return manifest;
        }
    }
}
