using HtmlAgilityPack;
using System;

namespace JeremyTCD.DocFx.Plugins.SortedArticleList
{
    public class SortedArticleListItem : IComparable<SortedArticleListItem>
    {
        public string RelPath { get; set; }

        public DateTime Date { get; set; }

        public HtmlNode SnippetNode { get; set; }

        public string Title { get; set; }

        public int CompareTo(SortedArticleListItem other)
        {
            int result = DateTime.Compare(other.Date, Date); // Most recent first

            if(result == 0)
            {
                result = string.CompareOrdinal(Title, other.Title); // Lexicographical order
            }

            return result;
        }
    }
}