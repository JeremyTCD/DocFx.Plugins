using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace JeremyTCD.DocFx.Plugins.MimoMarkdown
{
    public class ExplicitParagraphsExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            // Do nothing
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            var explicitParagraphRenderer = new ExplicitParagraphRenderer();

            if(!renderer.ObjectRenderers.InsertBefore<ParagraphRenderer>(explicitParagraphRenderer)) // Attempt to insert after default paragraph renderer to avoid changing order
            {
                renderer.ObjectRenderers.Add(explicitParagraphRenderer);
            }

            ParagraphRenderer defaultParagraphRenderer = renderer.ObjectRenderers.Find<ParagraphRenderer>();
            if (defaultParagraphRenderer != null)
            {
                renderer.ObjectRenderers.Remove(defaultParagraphRenderer);
            }
        }
    }
}
