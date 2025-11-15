using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models.Journal
{
    public static class CarrierStatsEvent
    {
        public sealed class CarrierStatsEventArgs
        {
            [JsonPropertyName("CarrierID")] public ulong CarrierID { get; set; }
            [JsonPropertyName("CarrierType")] public string? CarrierType { get; set; }
            [JsonPropertyName("Callsign")] public string? Callsign { get; set; }
            [JsonPropertyName("Name")] public string? Name { get; set; }
            [JsonPropertyName("DockingAccess")] public string? DockingAccess { get; set; }
            [JsonPropertyName("AllowNotorious")] public bool AllowNotorious { get; set; }
            [JsonPropertyName("FuelLevel")] public long FuelLevel { get; set; }
            [JsonPropertyName("Finance")] public CarrierFinance Finance { get; set; } = new();
            [JsonPropertyName("Crew")] public List<CarrierCrewMember> Crew { get; set; } = new();
        }

        public sealed class CarrierFinance
        {
            [JsonPropertyName("CarrierBalance")] public long CarrierBalance { get; set; }
        }

        public sealed class CarrierCrewMember
        {
            [JsonPropertyName("CrewRole")] public string? CrewRole { get; set; }
            [JsonPropertyName("Activated")] public bool Activated { get; set; }
            [JsonPropertyName("Enabled")] public bool Enabled { get; set; }
        }
    }

    public static class CarrierLocationEvent
    {
        public sealed class CarrierLocationEventArgs
        {
            [JsonPropertyName("CarrierID")] public ulong CarrierID { get; set; }
            [JsonPropertyName("CarrierType")] public string? CarrierType { get; set; }
            [JsonPropertyName("StarSystem")] public string? StarSystem { get; set; }
            [JsonPropertyName("SystemAddress")] public ulong SystemAddress { get; set; }
            [JsonPropertyName("BodyID")] public int BodyID { get; set; }
        }
    }

    public static class CarrierJumpRequestEvent
    {
        public sealed class CarrierJumpRequestEventArgs
        {
            [JsonPropertyName("CarrierID")] public ulong CarrierID { get; set; }
            [JsonPropertyName("CarrierType")] public string? CarrierType { get; set; }
            [JsonPropertyName("SystemName")] public string? SystemName { get; set; }
            [JsonPropertyName("SystemAddress")] public ulong? SystemAddress { get; set; }
            [JsonPropertyName("SystemID")] public ulong? SystemID { get; set; }
            [JsonPropertyName("Body")] public string? Body { get; set; }
            [JsonPropertyName("DepartureTime")] public DateTime? DepartureTime { get; set; }
        }
    }

    public static class CarrierJumpCancelledEvent
    {
        public sealed class CarrierJumpCancelledEventArgs
        {
            [JsonPropertyName("CarrierID")] public ulong CarrierID { get; set; }
            [JsonPropertyName("CarrierType")] public string? CarrierType { get; set; }
        }
    }

    public static class CarrierTradeOrderEvent
    {
        public sealed class CarrierTradeOrderEventArgs
        {
            [JsonPropertyName("CarrierID")] public ulong CarrierID { get; set; }
            [JsonPropertyName("BlackMarket")] public bool BlackMarket { get; set; }
            [JsonPropertyName("Commodity")] public string Commodity { get; set; } = string.Empty;
            [JsonPropertyName("Commodity_Localised")] public string? Commodity_Localised { get; set; }
            [JsonPropertyName("PurchaseOrder")] public long PurchaseOrder { get; set; }
            [JsonPropertyName("SaleOrder")] public long SaleOrder { get; set; }
            [JsonPropertyName("CancelTrade")] public bool CancelTrade { get; set; }
            [JsonPropertyName("Price")] public long Price { get; set; }
        }
    }

    public static class CarrierCrewServicesEvent
    {
        public sealed class CarrierCrewServicesEventArgs
        {
            [JsonPropertyName("CarrierID")] public ulong CarrierID { get; set; }
            [JsonPropertyName("CrewRole")] public string CrewRole { get; set; } = string.Empty;
            [JsonPropertyName("Operation")] public string Operation { get; set; } = string.Empty;
        }
    }

    public static class CarrierBankTransferEvent
    {
        public sealed class CarrierBankTransferEventArgs
        {
            [JsonPropertyName("CarrierID")] public ulong CarrierID { get; set; }
            [JsonPropertyName("CarrierBalance")] public long CarrierBalance { get; set; }
        }
    }

    public static class CarrierDepositFuelEvent
    {
        public sealed class CarrierDepositFuelEventArgs
        {
            [JsonPropertyName("CarrierID")] public ulong CarrierID { get; set; }
            [JsonPropertyName("Total")] public long Total { get; set; }
        }
    }

    public static class CargoTransferEvent
    {
        public sealed class CargoTransferEventArgs
        {
            [JsonPropertyName("Transfers")] public List<CargoTransferEntry> Transfers { get; set; } = new();
        }

        public sealed class CargoTransferEntry
        {
            [JsonPropertyName("Type")] public string Type { get; set; } = string.Empty;
            [JsonPropertyName("Type_Localised")] public string? Type_Localised { get; set; }
            [JsonPropertyName("Count")] public long Count { get; set; }
            [JsonPropertyName("Direction")] public string Direction { get; set; } = string.Empty;
            [JsonPropertyName("Stolen")] public bool? Stolen { get; set; }
        }
    }

    public static class MarketSellEvent
    {
        public sealed class MarketSellEventArgs
        {
            [JsonPropertyName("MarketID")] public long MarketID { get; set; }
            [JsonPropertyName("Type")] public string Type { get; set; } = string.Empty;
            [JsonPropertyName("Type_Localised")] public string? Type_Localised { get; set; }
            [JsonPropertyName("Count")] public int Count { get; set; }
            [JsonPropertyName("BlackMarket")] public bool BlackMarket { get; set; }
            [JsonPropertyName("StolenGoods")] public bool StolenGoods { get; set; }
        }
    }

    public static class MarketSnapshot
    {
        public sealed class Market
        {
            [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; }
            [JsonPropertyName("MarketID")] public ulong MarketID { get; set; }
            [JsonPropertyName("StationType")] public string? StationType { get; set; }
            [JsonPropertyName("StationName")] public string? StationName { get; set; }
            [JsonPropertyName("StarSystem")] public string? StarSystem { get; set; }
            [JsonPropertyName("Items")] public List<MarketItem> Items { get; set; } = new();
        }

        public sealed class MarketItem
        {
            [JsonPropertyName("Name")] public string Name { get; set; } = string.Empty;
            [JsonPropertyName("Name_Localised")] public string? NameLocalised { get; set; }
            [JsonPropertyName("Category")] public string? Category { get; set; }
            [JsonPropertyName("Category_Localised")] public string? CategoryLocalised { get; set; }
            [JsonPropertyName("BuyPrice")] public long BuyPrice { get; set; }
            [JsonPropertyName("SellPrice")] public long SellPrice { get; set; }
            [JsonPropertyName("Stock")] public long Stock { get; set; }
            [JsonPropertyName("Demand")] public long Demand { get; set; }
            [JsonPropertyName("Rare")] public bool Rare { get; set; }
        }
    }
}
