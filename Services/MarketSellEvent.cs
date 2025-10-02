using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Represents the structure of a MarketSell journal event for deserialization.
    /// </summary>
    public class MarketSellEvent
    {
        [JsonPropertyName("Type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("Count")]
        public int Count { get; set; }

        [JsonPropertyName("TotalSale")]
        public long TotalSale { get; set; }
    }
}