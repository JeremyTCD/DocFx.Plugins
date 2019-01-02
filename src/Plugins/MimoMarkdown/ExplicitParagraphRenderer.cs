using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace JeremyTCD.DocFx.Plugins.MimoMarkdown
{
    /// <summary>
    /// Always renders &lt;p&gt; tags. Allows Mimo to limit the widths of text blocks for better readability.
    /// </summary>
    public class ExplicitParagraphRenderer : HtmlObjectRenderer<ParagraphBlock>
    {
        protected override void Write(HtmlRenderer renderer, ParagraphBlock obj)
        {
            if (renderer.EnableHtmlForBlock)
            {
                if (!renderer.IsFirstInContainer)
                {
                    renderer.EnsureLine();
                }

                renderer.Write("<p").WriteAttributes(obj).Write(">");
            }
            renderer.WriteLeafInline(obj);
            if (renderer.EnableHtmlForBlock)
            {
                renderer.WriteLine("</p>");
            }
        }
    }
}
