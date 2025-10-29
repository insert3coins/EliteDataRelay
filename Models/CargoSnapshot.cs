using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    public record CargoSnapshot([property: JsonPropertyName("Inventory")] IReadOnlyList<CargoItem> Items, int Count)
    {
        public CargoSnapshot() : this(new List<CargoItem>(), 0) { }
    }
}
