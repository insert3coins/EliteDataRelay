using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    public class JournalEvent
    {
        [JsonPropertyName("event")]
        public string? Event { get; set; }

        [JsonPropertyName("StarSystem")]
        public string? StarSystem { get; set; }

        [JsonPropertyName("Stations")]
        public List<StationInfo>? Stations { get; set; }

        [JsonPropertyName("StarType")]
        public string? StarType { get; set; }

        [JsonPropertyName("BodyName")]
        public string? BodyName { get; set; }

        [JsonPropertyName("StationName")]
        public string? StationName { get; set; }
    }

    public class LoadoutEvent : JournalEvent
    {
        [JsonPropertyName("CargoCapacity")]
        public int CargoCapacity { get; set; }

        [JsonPropertyName("ShipName")]
        public string? ShipName { get; set; }

        [JsonPropertyName("Ship_Localised")]
        public string? ShipLocalised { get; set; }

        [JsonPropertyName("ShipIdent")]
        public string? ShipIdent { get; set; }
    }

    public class LoadGameEvent : JournalEvent
    {
        [JsonPropertyName("Commander")]
        public string? Commander { get; set; }

        [JsonPropertyName("ShipName")]
        public string? ShipName { get; set; }

        [JsonPropertyName("Ship_Localised")]
        public string? ShipLocalised { get; set; }

        [JsonPropertyName("ShipIdent")]
        public string? ShipIdent { get; set; }
    }

    public class ShipyardSwapEvent : JournalEvent
    {
        [JsonPropertyName("ShipType")]
        public string? ShipType { get; set; }

        [JsonPropertyName("ShipType_Localised")]
        public string? ShipTypeLocalised { get; set; }
    }
}