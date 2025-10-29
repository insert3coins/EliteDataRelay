using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    public class StationInfo
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }
    }
}
