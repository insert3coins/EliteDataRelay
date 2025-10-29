using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Represents an individual cargo item in Elite Dangerous
    /// </summary>
    public record CargoItem(
        [property: JsonPropertyName("Name")] string Name,
        [property: JsonPropertyName("Count")] int Count,
        [property: JsonPropertyName("Name_Localised")] string Localised,
        [property: JsonPropertyName("Stolen")] int Stolen);
}
