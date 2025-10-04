using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models.Market
{
    public class MarketInfo
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("sName")]
        public string StationName { get; set; } = string.Empty;

        [JsonPropertyName("systemName")]
        public string SystemName { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("distanceToArrival")]
        public double DistanceToArrival { get; set; }

        [JsonPropertyName("haveMarket")]
        public bool HaveMarket { get; set; }

        [JsonPropertyName("commodities")]
        public List<CommodityMarketData> Commodities { get; set; } = new List<CommodityMarketData>();

        [JsonIgnore]
        public CommodityMarketData? Commodity => Commodities.FirstOrDefault();
    }

    public class CommodityMarketData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("buyPrice")]
        public int BuyPrice { get; set; }

        [JsonPropertyName("stock")]
        public int Stock { get; set; }

        [JsonPropertyName("sellPrice")]
        public int SellPrice { get; set; }

        [JsonPropertyName("demand")]
        public int Demand { get; set; }

        [JsonPropertyName("stockBracket")]
        public int StockBracket { get; set; }

        [JsonPropertyName("demandBracket")]
        public int DemandBracket { get; set; }
    }
}