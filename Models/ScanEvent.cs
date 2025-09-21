using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    public class ScanEvent
    {
        [JsonPropertyName("BodyName")]
        public string BodyName { get; set; } = string.Empty;

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