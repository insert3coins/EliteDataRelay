using System;
using System.Text.Json;
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
                if (materialsEvent != null) MaterialsChanged?.Invoke(this, materialsEvent);
            }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine($"[JournalWatcherService] Error processing Materials event: {ex.Message}"); }
        }
    }
}