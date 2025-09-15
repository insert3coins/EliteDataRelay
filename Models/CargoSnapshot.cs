using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EliteCargoMonitor.Models
{
    /// <summary>
    /// Represents a complete cargo inventory snapshot from Elite Dangerous
    /// </summary>
    public record CargoSnapshot(
        [property: JsonPropertyName("Inventory")] List<CargoItem> Inventory,
        [property: JsonPropertyName("Count")] int Count);
}