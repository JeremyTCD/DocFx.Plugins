namespace JeremyTCD.DocFx.Plugins.MimoMarkdown
{
    using Jering.Markdig.Extensions.FlexiBlocks;
    using Jering.Markdig.Extensions.FlexiBlocks.FlexiAlertBlocks;
    using Jering.Markdig.Extensions.FlexiBlocks.FlexiCodeBlocks;
    using Jering.Markdig.Extensions.FlexiBlocks.FlexiIncludeBlocks;
    using Jering.Markdig.Extensions.FlexiBlocks.FlexiSectionBlocks;
    using Jering.Markdig.Extensions.FlexiBlocks.FlexiTableBlocks;
    using Markdig;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdigEngine.Extensions;
    using Microsoft.DocAsCode.Plugins;
    using System;
    using System.IO;

    public class MimoMarkdownService : IMarkdownService, IDisposable
    {
        public string Name => "mimo-markdown";

        private readonly MarkdownServiceParameters _parameters;

        public MimoMarkdownService(MarkdownServiceParameters parameters)
        {
            _parameters = parameters;
        }

        public MarkupResult Markup(string src, string path)
        {
            return Markup(src, path, false);
        }

        public MarkupResult Markup(string src, string path, bool enableValidation)
        {
            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(Strings.ArgumentException_ValueCannotBeNullWhitespaceOrAnEmptyString,
                    nameof(path));
            }

            if (string.IsNullOrWhiteSpace(src))
            {
                return new MarkupResult
                {
                    Html = src // Nothing to process
                };
            }

            MarkdownPipeline markdownPipeline = CreatePipeline(path);
            string html = null;
            try
            {
                html = Markdown.ToHtml(src, markdownPipeline);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception.ToString(), file: path);
                throw;
            }

            return new MarkupResult
            {
                Html = html
            };
        }

        private MarkdownPipeline CreatePipeline(string path)
        {
            var flexiSectionBlocksExtensionOptions = new FlexiSectionBlocksExtensionOptions
            {
                DefaultBlockOptions = new FlexiSectionBlockOptions(linkIconMarkup: @"<svg><use xlink:href=""#material-design-link"" /></svg>")
            };

            var flexiCodeBlocksExtensionOptions = new FlexiCodeBlocksExtensionOptions
            {
                DefaultBlockOptions = new FlexiCodeBlockOptions(
                    copyIconMarkup: @"<svg><use xlink:href=""#custom-file-copy"" /></svg>",
                    hiddenLinesIconMarkup: @"<svg><use xlink:href=""#custom-more-vert"" /></svg>")
            };

            var flexiAlertBlocksExtensionOptions = new FlexiAlertBlocksExtensionOptions();
            flexiAlertBlocksExtensionOptions.IconMarkups["info"] = @"<svg><use xlink:href=""#material-design-info"" /></svg>";
            flexiAlertBlocksExtensionOptions.IconMarkups["warning"] = @"<svg><use xlink:href=""#material-design-warning"" /></svg>";
            flexiAlertBlocksExtensionOptions.IconMarkups["critical-warning"] = @"<svg><use xlink:href=""#material-design-error"" /></svg>";

            var flexiIncludeBlocksExtensionOptions = new FlexiIncludeBlocksExtensionOptions
            {
                RootBaseUri = Path.Combine(_parameters.BasePath, path)
            };

            var flexiTableBlocksExtensionOptions = new FlexiTableBlocksExtensionOptions
            {
                DefaultBlockOptions = new FlexiTableBlockOptions(wrapperElement: "div")
            };

            var builder = new MarkdownPipelineBuilder().
                UseEmphasisExtras().
                UseDefinitionLists().
                UseFootnotes().
                UseAutoLinks().
                UseTaskLists().
                UseListExtras().
                UseMediaLinks().
                UseAbbreviations().
                UseFooters().
                UseFigures().
                UseCitations().
                UseCustomContainers().
                UseGenericAttributes().
                UseMathematics().
                UseSmartyPants().
                UseDiagrams().
                UseFlexiBlocks(
                    alertBlocksExtensionOptions: flexiAlertBlocksExtensionOptions,
                    codeBlocksExtensionOptions: flexiCodeBlocksExtensionOptions,
                    sectionBlocksExtensionOptions: flexiSectionBlocksExtensionOptions,
                    includeBlocksExtensionOptions: flexiIncludeBlocksExtensionOptions,
                    tableBlocksExtensionOptions: flexiTableBlocksExtensionOptions);

            builder.Extensions.Add(new CustomYamlHeaderExtension(new MarkdownContext()));
            builder.Extensions.Add(new ExplicitParagraphsExtension());

            return builder.Build();
        }

        public void Dispose()
        {
            FlexiBlocksMarkdownPipelineBuilderExtensions.DisposeServiceProvider();
        }
    }
}
