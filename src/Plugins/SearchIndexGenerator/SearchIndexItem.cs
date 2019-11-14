namespace JeremyTCD.DocFx.Plugins.SearchIndexGenerator
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    public class SearchIndexItem : IComparable<SearchIndexItem>
    {
        [JsonProperty("title", Order = 1)]
        public string Title { get; set; }

        [JsonProperty("relPath", Order = 2)]
        public string RelPath { get; set; }

        [JsonProperty("date", Order = 3)]
        public string Date { get; set; }

        [JsonProperty("text", Order = 4)]
        public List<string> Text { get; set; }

        [JsonProperty("description", Order = 5)]
        public string Description { get; set; }

        public int CompareTo(SearchIndexItem other)
        {
            return string.CompareOrdinal(Title, other.Title);
        }
    }
}