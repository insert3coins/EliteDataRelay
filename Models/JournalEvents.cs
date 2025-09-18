using System;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    public class JournalEvent
    {
        [JsonPropertyName("event")]
        public string? Event { get; set; }
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
}