using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    public class ShipInfo
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("modules")]
        public Dictionary<string, string> Modules { get; set; } = new Dictionary<string, string>();
    }
}
