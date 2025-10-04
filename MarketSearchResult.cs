using System.Text.Json.Serialization;

namespace EliteDataRelay.Models.Market
{
    /// <summary>
    /// Represents a single market search result from the EDSM api-v1/market/search endpoint.
    /// </summary>
    public class MarketSearchResult
    {
        [JsonPropertyName("systemName")]
        public string SystemName { get; set; } = string.Empty;

        [JsonPropertyName("stationName")]
        public string StationName { get; set; } = string.Empty;

        [JsonPropertyName("marketId")]
        public long MarketId { get; set; }

        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("buyPrice")]
        public int BuyPrice { get; set; }

        [JsonPropertyName("sellPrice")]
        public int SellPrice { get; set; }

        [JsonPropertyName("stock")]
        public int Stock { get; set; }

        [JsonPropertyName("demand")]
        public int Demand { get; set; }

        [JsonPropertyName("lastUpdate")]
        public string LastUpdate { get; set; } = string.Empty;
    }
}