using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Represents the "ShipyardNew" event from the journal.
    /// Written when a new ship is purchased.
    /// </summary>
    public class ShipyardNewEvent
    {
        [JsonPropertyName("ShipType")]
        public string ShipType { get; set; } = string.Empty;

        [JsonPropertyName("ShipType_Localised")]
        public string? ShipTypeLocalised { get; set; }
    }

    /// <summary>
    /// Represents the "ShipyardSwap" event from the journal.
    /// Written when switching to another stored ship.
    /// </summary>
    public class ShipyardSwapEvent : ShipyardNewEvent { }

    /// <summary>
    /// Represents the "Docked" event from the journal.
    /// Written when the player docks at a station or carrier.
    /// </summary>
    public class DockedEvent
    {
        [JsonPropertyName("StationName")]
        public string StationName { get; set; } = string.Empty;

        [JsonPropertyName("Name")] // Used for Fleet Carrier custom name
        public string? Name { get; set; }

        [JsonPropertyName("StationType")]
        public string StationType { get; set; } = string.Empty;

        [JsonPropertyName("StationFaction")]
        public Faction? StationFaction { get; set; }

        [JsonPropertyName("StationGovernment_Localised")]
        public string? StationGovernmentLocalised { get; set; }

        [JsonPropertyName("StationGovernment")]
        public string? StationGovernment { get; set; }

        [JsonPropertyName("StationAllegiance")]
        public string? StationAllegiance { get; set; }

        [JsonPropertyName("StationEconomy_Localised")]
        public string? StationEconomyLocalised { get; set; }

        [JsonPropertyName("StationEconomy")]
        public string? StationEconomy { get; set; }

        [JsonPropertyName("StationServices")]
        public List<string> StationServices { get; set; } = new List<string>();
    }
}