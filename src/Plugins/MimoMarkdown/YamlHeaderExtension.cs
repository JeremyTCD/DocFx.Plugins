using Jering.Markdig.Extensions.FlexiBlocks.FlexiCodeBlocks;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Parsers;
using Markdig.Renderers;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace JeremyTCD.DocFx.Plugins.MimoMarkdown
{
    public class CustomYamlHeaderExtension : IMarkdownExtension
    {
        private readonly MarkdownContext _context;

        public CustomYamlHeaderExtension(MarkdownContext context)
        {
            _context = context;
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<YamlFrontMatterParser>())
            {
                // Insert the YAML parser before the thematic break parser, as it is also triggered on a --- dash
                pipeline.BlockParsers.InsertBefore<ThematicBreakParser>(new YamlFrontMatterParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (!renderer.ObjectRenderers.Contains<YamlHeaderRenderer>())
            {
                renderer.ObjectRenderers.InsertBefore<FlexiCodeBlockRenderer>(new YamlHeaderRenderer(_context)); // YamlFrontMatterBlock extends CodeBlock
            }
        }
    }
}
