using System.Text.Json.Serialization;

namespace EliteDataRelay.Models

{
    /// <summary>
    /// Represents the top-level response from the EDSM system API.
    /// It can be either a single object or an empty array if not found.
    /// </summary>
    public class EdsmSystem
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("information")]
        public EdsmSystemInformation? Information { get; set; }
    }

    /// <summary>
    /// Represents the detailed information block for a system from EDSM.
    /// </summary>
    public class EdsmSystemInformation
    {
        [JsonPropertyName("allegiance")]
        public string? Allegiance { get; set; }
        [JsonPropertyName("government")]
        public string? Government { get; set; }
        [JsonPropertyName("faction")]
        public string? Faction { get; set; }
        [JsonPropertyName("factionState")]
        public string? FactionState { get; set; }
        [JsonPropertyName("population")]
        public long? Population { get; set; }
        [JsonPropertyName("security")]
        public string? Security { get; set; }
        [JsonPropertyName("economy")]
        public string? Economy { get; set; }
    }
}