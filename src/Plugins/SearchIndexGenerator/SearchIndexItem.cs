namespace JeremyTCD.DocFx.Plugins.SearchIndexGenerator
{
    using Newtonsoft.Json;

    public class SearchIndexItem
    {
        [JsonProperty("relPath", Order = 1)]
        public string RelPath { get; set; }

        [JsonProperty("snippetHtml", Order = 2)]
        public string SnippetHtml { get; set; }

        [JsonProperty("title", Order = 3)]
        public string Title { get; set; }

        [JsonProperty("text", Order = 4)]
        public string Text { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as SearchIndexItem);
        }

        public bool Equals(SearchIndexItem other)
        {
            if (other == null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return string.Equals(SnippetHtml, other.SnippetHtml)
                && string.Equals(RelPath, other.RelPath)
                && string.Equals(Text, other.Text);
        }

        public override int GetHashCode()
        {
            return SnippetHtml.GetHashCode() ^ RelPath.GetHashCode() ^ Text.GetHashCode();
        }
    }
}