using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Represents the structure of a BuyDrones journal event for deserialization.
    /// </summary>
    public class BuyDronesEvent
    {
        [JsonPropertyName("Count")]
        public int Count { get; set; }

        [JsonPropertyName("TotalCost")]
        public long TotalCost { get; set; }
    }
}