using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models.Market
{
    // --- Inara API Request Models ---

    public class InaraRequest
    {
        [JsonPropertyName("header")]
        public InaraHeader Header { get; set; } = new InaraHeader();

        [JsonPropertyName("events")]
        public List<InaraEvent> Events { get; set; } = new List<InaraEvent>();
    }

    public class InaraHeader
    {
        [JsonPropertyName("appName")]
        public string AppName { get; set; } = string.Empty;

        [JsonPropertyName("appVersion")]
        public string AppVersion { get; set; } = "1.0.0";

        [JsonPropertyName("isDeveloped")]
        public bool IsDeveloped { get; set; } = false; // Use false for production/released apps

        [JsonPropertyName("APIkey")]
        public string ApiKey { get; set; } = string.Empty;
    }

    public class InaraEvent
    {
        [JsonPropertyName("eventName")]
        public string EventName { get; set; } = string.Empty;

        [JsonPropertyName("eventData")]
        public object EventData { get; set; } = new { };
    }

    public class GetCommodityMarketEventData
    {
        [JsonPropertyName("searchName")]
        public string SearchName { get; set; } = string.Empty;
    }

    // --- Inara API Response Models ---

    public class InaraResponse
    {
        [JsonPropertyName("events")]
        public List<InaraEventResponse> Events { get; set; } = new List<InaraEventResponse>();
    }

    public class InaraEventResponse
    {
        [JsonPropertyName("eventData")]
        public InaraEventData EventData { get; set; } = new InaraEventData();
    }

    public class InaraEventData
    {
        [JsonPropertyName("marketStations")]
        public List<InaraMarketStation> MarketStations { get; set; } = new List<InaraMarketStation>();
    }

    public class InaraMarketStation
    {
        [JsonPropertyName("stationName")]
        public string StationName { get; set; } = string.Empty;

        [JsonPropertyName("systemName")]
        public string SystemName { get; set; } = string.Empty;

        [JsonPropertyName("buyPrice")]
        public int BuyPrice { get; set; }

        [JsonPropertyName("sellPrice")]
        public int SellPrice { get; set; }

        [JsonPropertyName("stationStock")]
        public int StationStock { get; set; }

        [JsonPropertyName("stationDemand")]
        public int StationDemand { get; set; }
    }
}