using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using EliteDataRelay.Models;

namespace EliteDataRelay.Services
{
    public partial class JournalWatcherService
    {
        public event EventHandler<MaterialsEvent>? MaterialsChanged;

        private void HandleMaterialsEvent(JsonElement json)
        {
            try
            {
                var materialsEvent = JsonSerializer.Deserialize<MaterialsEvent>(json.GetRawText(), _jsonOptions);
                if (materialsEvent != null)
                {
                    // The journal event is sparse. We need to create a complete list including zero-count materials.
                    var allKnownMaterials = MaterialDataService.GetAllMaterials();

                    // Create dictionaries for quick lookups of counts from the event data.
                    var rawCounts = materialsEvent.Raw.ToDictionary(m => m.Name, m => m.Count, StringComparer.OrdinalIgnoreCase);
                    var manufacturedCounts = materialsEvent.Manufactured.ToDictionary(m => m.Name, m => m.Count, StringComparer.OrdinalIgnoreCase);
                    var encodedCounts = materialsEvent.Encoded.ToDictionary(m => m.Name, m => m.Count, StringComparer.OrdinalIgnoreCase);

                    // Populate the lists with ALL known materials, using the count from the event or defaulting to 0.
                    var allRaw = allKnownMaterials.Where(m => m.Category == "Raw")
                        .Select(m => new MaterialItem { Name = m.Name, Count = rawCounts.GetValueOrDefault(m.Name, 0) }).ToList();

                    var allManufactured = allKnownMaterials.Where(m => m.Category == "Manufactured")
                        .Select(m => new MaterialItem { Name = m.Name, Count = manufacturedCounts.GetValueOrDefault(m.Name, 0) }).ToList();

                    var allEncoded = allKnownMaterials.Where(m => m.Category == "Encoded")
                        .Select(m => new MaterialItem { Name = m.Name, Count = encodedCounts.GetValueOrDefault(m.Name, 0) }).ToList();

                    var completeMaterialsEvent = new MaterialsEvent
                    {
                        Raw = allRaw,
                        Manufactured = allManufactured,
                        Encoded = allEncoded
                    };
                    MaterialsChanged?.Invoke(this, completeMaterialsEvent);
                }
            }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine($"[JournalWatcherService] Error processing Materials event: {ex.Message}"); }
        }
    }
}