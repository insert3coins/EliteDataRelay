using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models.Market
{
    public class SystemMarketResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("stations")]
        public List<MarketInfo> Stations { get; set; } = new List<MarketInfo>();
    }
}