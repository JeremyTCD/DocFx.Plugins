using HtmlAgilityPack;
using Newtonsoft.Json;
using System;

namespace JeremyTCD.DocFx.Plugins.SortedArticleList
{
    public class SortedArticleListItem
    {
        [JsonProperty("relPath")]
        public string RelPath { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("snippetNode")]
        public HtmlNode SnippetNode { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as SortedArticleListItem);
        }

        public bool Equals(SortedArticleListItem other)
        {
            if (other == null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Equals(SnippetNode, other.SnippetNode)
                && string.Equals(RelPath, other.RelPath)
                && DateTime.Equals(Date, other.Date);
        }

        public override int GetHashCode()
        {
            return SnippetNode.GetHashCode() ^ RelPath.GetHashCode() ^ Date.GetHashCode();
        }
    }
}