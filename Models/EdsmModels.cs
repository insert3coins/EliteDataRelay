using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Represents the top-level object returned by the EDSM system API.
    /// </summary>
    public class EdsmSystem
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("information")]
        public EdsmSystemInformation? Information { get; set; }
    }

    /// <summary>
    /// Represents the 'information' sub-object in the EDSM system API response.
    /// </summary>
    public class EdsmSystemInformation
    {
        public string? Allegiance { get; set; }
        public string? Government { get; set; }
        public string? Faction { get; set; }
        public string? FactionState { get; set; }
        public long? Population { get; set; }
        public string? Security { get; set; }
        public string? Economy { get; set; }
    }
}