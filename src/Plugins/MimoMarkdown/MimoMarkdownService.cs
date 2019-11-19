namespace JeremyTCD.DocFx.Plugins.MimoMarkdown
{
    using Jering.Markdig.Extensions.FlexiBlocks;
    using Jering.Markdig.Extensions.FlexiBlocks.FlexiIncludeBlocks;
    using Jering.Markdig.Extensions.FlexiBlocks.FlexiPictureBlocks;
    using Jering.Markdig.Extensions.FlexiBlocks.FlexiVideoBlocks;
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
            string html;
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
            string basePath = _parameters.BasePath;
            string mediaDirectory = Path.Combine(basePath, "src/resources");

            var includeBlocksExtensionOptions = new FlexiIncludeBlocksExtensionOptions(baseUri: Path.Combine(basePath, path));
            var flexiPictureBlocksExtensionOptions = new FlexiPictureBlocksExtensionOptions(localMediaDirectory: mediaDirectory);
            var flexiVideoBlocksExtensionOptions = new FlexiVideoBlocksExtensionOptions(localMediaDirectory: mediaDirectory);

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
                      UseFlexiBlocks(includeBlocksExtensionOptions,
                          flexiPictureBlocksExtensionOptions: flexiPictureBlocksExtensionOptions,
                          flexiVideoBlocksExtensionOptions: flexiVideoBlocksExtensionOptions);

            builder.Extensions.Add(new CustomYamlHeaderExtension(new MarkdownContext()));

            return builder.Build();
        }

        public void Dispose()
        {
            MarkdownPipelineBuilderExtensions.DisposeServiceProvider();
        }
    }
}
