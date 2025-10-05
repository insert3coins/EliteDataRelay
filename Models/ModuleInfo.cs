using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    public class ModuleInfo
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("mount")]
        public string? Mount { get; set; }
        [JsonPropertyName("class")]
        public int Class { get; set; }
        [JsonPropertyName("rating")]
        public string Rating { get; set; } = string.Empty;
    }
}