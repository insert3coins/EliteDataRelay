using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Represents the data structure of the Status.json file from the game.
    /// </summary>
    public class StatusFile
    {
        public Fuel? Fuel { get; set; }
        public int Cargo { get; set; }
        public long? Balance { get; set; }
        public bool ShieldsUp { get; set; }

        [JsonPropertyName("Pips")]
        public int[]? PowerPips { get; set; }
        [JsonPropertyName("HullHealth")]
        public double HullHealth { get; set; }
    }
}