using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models

{
    public class MaterialItem
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("Name_Localised")]
        public string? Localised { get; set; }

        [JsonPropertyName("Count")]
        public int Count { get; set; }
    }

    public class MaterialsEvent
    {
        [JsonPropertyName("Raw")]
        public List<MaterialItem> Raw { get; set; } = new();

        [JsonPropertyName("Manufactured")]
        public List<MaterialItem> Manufactured { get; set; } = new();

        [JsonPropertyName("Encoded")]
        public List<MaterialItem> Encoded { get; set; } = new();
    }

    public class MaterialCollectedEvent
    {
        [JsonPropertyName("Category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("Count")]
        public int Count { get; set; }
    }

    public class MaterialTradeEvent
    {
        [JsonPropertyName("Paid")]
        public MaterialItem Paid { get; set; } = new();

        [JsonPropertyName("Received")]
        public MaterialItem Received { get; set; } = new();
    }

    public class EngineerCraftEvent
    {
        [JsonPropertyName("Materials")]
        public List<MaterialItem> Materials { get; set; } = new();
    }

    public class MaterialsProcessedEventArgs : EventArgs
    {
        public MaterialsEvent Materials { get; }
        public string Hash { get; }

        public MaterialsProcessedEventArgs(MaterialsEvent materials, string hash)
        {
            Materials = materials;
            Hash = hash;
        }
    }
}
