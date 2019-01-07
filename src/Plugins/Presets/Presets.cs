using Microsoft.DocAsCode.Build.ConceptualDocuments;
using Microsoft.DocAsCode.Plugins;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;

namespace JeremyTCD.DocFx.Plugins.Presets
{
    [Export(nameof(ConceptualDocumentProcessor), typeof(IDocumentBuildStep))]
    public class Presets : IDocumentBuildStep
    {
        public int BuildOrder => 999;

        public string Name => nameof(Presets);

        /// <summary>
        /// Expand presets. By doing expanding in the "Build" method, we guarantee that the options are exposed by <see cref="FileMetadataExposer"/>,
        /// which runs its logic in a "Postbuild" method. Note that the DocFx build phase (not to be confused with the "Build" method)
        /// just generates models. Building pages from templates is run in the subsequent phase - https://dotnet.github.io/docfx/tutorial/intro_template.html.
        /// </summary>
        public void Build(FileModel model, IHostService host)
        {
            if (model.Content is IDictionary<string, object> content)
            {
                // Display pages, e.g landing pages, index pages, 404 etc. Everything is disabled.
                if (content.TryGetValue("mimo_isDisplay", out object mimo_isDisplay) && (mimo_isDisplay as bool?) == true)
                {
                    content["mimo_includeInSal"] = false;
                    content["mimo_includeInSearchIndex"] = false;
                    content["mimo_disableEditArticle"] = true;
                    content["mimo_disableMetadata"] = true;
                    content["mimo_disableCategoryMenu"] = true;
                    content["mimo_disableArticleMenu"] = true;
                    content["mimo_unneededFontPreloads"] = new string[] { "/resources/ibm-plex-mono-v3-latin-regular.woff2" };
                }

                // Searcheable display pages. Everything is disabled but page is indexed for search.
                if (content.TryGetValue("mimo_isSearchableDisplay", out object mimo_isSearchableDisplay) && (mimo_isSearchableDisplay as bool?) == true)
                {
                    content["mimo_includeInSal"] = false;
                    content["mimo_disableEditArticle"] = true;
                    content["mimo_disableMetadata"] = true;
                    content["mimo_disableCategoryMenu"] = true;
                    content["mimo_disableArticleMenu"] = true;
                    content["mimo_unneededFontPreloads"] = new string[] { "/resources/ibm-plex-mono-v3-latin-regular.woff2" };
                }

                // Page with just text and article menu. Everything is disabled other than article menu.
                if (content.TryGetValue("mimo_isTextAndArticleMenu", out object mimo_isTextAndArticleMenu) && (mimo_isTextAndArticleMenu as bool?) == true)
                {
                    content["mimo_includeInSal"] = false;
                    content["mimo_includeInSearchIndex"] = false;
                    content["mimo_disableEditArticle"] = true;
                    content["mimo_disableMetadata"] = true;
                    content["mimo_disableCategoryMenu"] = true;
                    content["mimo_unneededFontPreloads"] = new string[] { "/resources/ibm-plex-mono-v3-latin-regular.woff2" };
                }

                // Default sal page settings
                if (content.TryGetValue("mimo_isSalPage", out object mimo_isSalPage) && (mimo_isSalPage as bool?) == true)
                {
                    content["mimo_includeInSal"] = false;
                    content["mimo_includeInSearchIndex"] = false;
                    content["mimo_disableEditArticle"] = true;
                    content["mimo_disableMetadata"] = true;
                    content["mimo_disableArticleMenu"] = true;
                    content["mimo_enableSal"] = true;
                }
            }
        }

        public void Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
            // Do nothing
        }

        public IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            // Do nothing
            return models;
        }
    }
}
