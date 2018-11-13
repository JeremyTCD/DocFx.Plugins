﻿using HtmlAgilityPack;
using JeremyTCD.DocFx.Plugins.Utils;
using Microsoft.DocAsCode.Plugins;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;

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

                // Update all anchors that have hrefs starting with http. Since all internal links in mimo have to be relative (so that the site can
                // be published to paths, e.g jering.tech/utilities/<project name>), full urls are only used for external sites.
                foreach (HtmlNode htmlNode in htmlDoc.DocumentNode.SelectNodes("//a[starts-with(@href, 'http')]"))
                {
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