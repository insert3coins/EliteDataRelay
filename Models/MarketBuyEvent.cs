using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Represents a "MarketBuy" event from the Elite Dangerous journal.
    /// </summary>
    public class MarketBuyEvent
    {
        [JsonPropertyName("Type")]
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
        public long TotalCost { get; set; }
    }
}