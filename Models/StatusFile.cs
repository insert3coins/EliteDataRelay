using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Represents the structure of the Status.json file written by the game.
    /// </summary>
    public class StatusFile
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;

        [JsonPropertyName("Flags")]
        public long Flags { get; set; }

        [JsonPropertyName("Pips")]
        public int[]? Pips { get; set; }

        [JsonPropertyName("FireGroup")]
        public int FireGroup { get; set; }

        [JsonPropertyName("GuiFocus")]
        public int GuiFocus { get; set; }

        [JsonPropertyName("Fuel")]
        public Fuel? Fuel { get; set; }

        [JsonPropertyName("Cargo")]
        public double Cargo { get; set; }

        [JsonPropertyName("LegalState")]
        public string LegalState { get; set; } = string.Empty;

        [JsonPropertyName("Balance")]
        public long? Balance { get; set; }

        [JsonPropertyName("HullHealth")]
        public double HullHealth { get; set; }
    }
}