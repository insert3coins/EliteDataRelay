using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Represents the "Scan" event from the journal.
    /// Written when scanning a celestial body (star, planet, etc.).
    /// </summary>
    public class ScanEvent
    {
        [JsonPropertyName("ScanType")]
        public string? ScanType { get; set; }

        [JsonPropertyName("BodyName")]
        public string BodyName { get; set; } = string.Empty;

        [JsonPropertyName("BodyID")]
        public int? BodyID { get; set; }

        [JsonPropertyName("StarSystem")]
        public string? StarSystem { get; set; }

        [JsonPropertyName("SystemAddress")]
        public long? SystemAddress { get; set; }

        [JsonPropertyName("DistanceFromArrivalLS")]
        public double? DistanceFromArrivalLS { get; set; }

        [JsonPropertyName("StarType")]
        public string? StarType { get; set; }

        [JsonPropertyName("PlanetClass")]
        public string? PlanetClass { get; set; }

        [JsonPropertyName("WasDiscovered")]
        public bool WasDiscovered { get; set; }

        [JsonPropertyName("WasMapped")]
        public bool WasMapped { get; set; }

        [JsonPropertyName("TerraformState")]
        public string? TerraformState { get; set; }

        [JsonPropertyName("Landable")]
        public bool? Landable { get; set; }
    }
}
