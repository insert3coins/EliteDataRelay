using System;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    public class LoadoutChangedEventArgs : EventArgs
    {
        public ShipLoadout Loadout { get; }

        public LoadoutChangedEventArgs(ShipLoadout loadout)
        {
            Loadout = loadout;
        }
    }

    public class StatusFile
    {
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }
        [JsonPropertyName("event")]
        public string? Event { get; set; }
        [JsonPropertyName("fuel")]
        public StatusFuel? Fuel { get; set; }
        [JsonPropertyName("cargo")]
        public double Cargo { get; set; }
        [JsonPropertyName("balance")]
        public long? Balance { get; set; }
        [JsonPropertyName("hullhealth")]
        public double? HullHealth { get; set; }
    }

    public class StatusFuel
    {
        public double FuelMain { get; set; }
        public double FuelReservoir { get; set; }
    }

    public class StatusChangedEventArgs : EventArgs
    {
        public StatusFile Status { get; }
        public StatusChangedEventArgs(StatusFile status) { Status = status; }
    }

    public class DockedEvent
    {
        // For Fleet Carriers, this property holds the custom name of the carrier.
        // For stations, it is not present.
        public string? Name { get; set; }

        public string StationName { get; set; } = string.Empty;
        public string StationType { get; set; } = string.Empty;
        public string? StationAllegiance { get; set; }
        public string? StationEconomy { get; set; }
        [JsonPropertyName("StationEconomy_Localised")]
        public string? StationEconomyLocalised { get; set; }
        public string? StationGovernment { get; set; }
        [JsonPropertyName("StationGovernment_Localised")]
        public string? StationGovernmentLocalised { get; set; }
        [JsonPropertyName("StationFaction")]
        public StationFactionInfo? StationFaction { get; set; }
        public List<string> StationServices { get; set; } = new();
    }

    public class StationFactionInfo
    {
        public string Name { get; set; } = string.Empty;
    }

    public class DockedEventArgs : EventArgs
    {
        public DockedEvent DockedEvent { get; }
        public DockedEventArgs(DockedEvent dockedEvent)
        {
            DockedEvent = dockedEvent;
        }
    }

    public class UndockedEventArgs : EventArgs
    {
        public string StationName { get; }
        public UndockedEventArgs(string stationName)
        {
            StationName = stationName;
        }
    }
}