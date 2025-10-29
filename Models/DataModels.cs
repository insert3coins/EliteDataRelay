using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Represents a faction in the game.
    /// </summary>
    public class Faction
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the contents of the Status.json file.
    /// </summary>
    public class Status
    {
        [JsonPropertyName("Flags")]
        public long Flags { get; set; }

        [JsonPropertyName("Pips")]
        public List<int> Pips { get; set; } = new List<int>();

        [JsonPropertyName("FireGroup")]
        public int FireGroup { get; set; }

        [JsonPropertyName("GuiFocus")]
        public int GuiFocus { get; set; }

        [JsonPropertyName("Fuel")]
        public FuelInfo? Fuel { get; set; }

        [JsonPropertyName("Cargo")]
        public double Cargo { get; set; }

        [JsonPropertyName("Balance")]
        public long? Balance { get; set; }

        [JsonPropertyName("LegalState")]
        public string LegalState { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the fuel information within the Status.json file.
    /// </summary>
    public class FuelInfo
    {
        [JsonPropertyName("FuelMain")]
        public double FuelMain { get; set; }

        [JsonPropertyName("FuelReservoir")]
        public double FuelReservoir { get; set; }
    }
}
