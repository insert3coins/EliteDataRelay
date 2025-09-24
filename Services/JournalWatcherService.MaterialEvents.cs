using EliteDataRelay.Models;
using System.Diagnostics;
using System.Text.Json;

namespace EliteDataRelay.Services
{
    public partial class JournalWatcherService
    {
        private void ProcessMaterialsEvent(string journalLine, JsonSerializerOptions options)
        {
            var materialsEvent = JsonSerializer.Deserialize<MaterialsEvent>(journalLine, options);
            if (materialsEvent != null)
            {
                Debug.WriteLine($"[JournalWatcherService] Found Materials event.");
                MaterialsEvent?.Invoke(this, new MaterialsEventArgs(materialsEvent));
            }
        }

        private void ProcessMaterialCollectedEvent(string journalLine, JsonSerializerOptions options)
        {
            var collectedEvent = JsonSerializer.Deserialize<MaterialCollectedEvent>(journalLine, options);
            if (collectedEvent != null)
            {
                Debug.WriteLine($"[JournalWatcherService] Found MaterialCollected event for {collectedEvent.Name}.");
                MaterialCollectedEvent?.Invoke(this, new MaterialCollectedEventArgs(collectedEvent));
            }
        }

        private void ProcessMaterialDiscardedEvent(string journalLine, JsonSerializerOptions options)
        {
            var discardedEvent = JsonSerializer.Deserialize<MaterialCollectedEvent>(journalLine, options);
            if (discardedEvent != null)
            {
                MaterialDiscardedEvent?.Invoke(this, new MaterialCollectedEventArgs(discardedEvent));
            }
        }

        private void ProcessMaterialTradeEvent(string journalLine, JsonSerializerOptions options)
        {
            var tradeEvent = JsonSerializer.Deserialize<MaterialTradeEvent>(journalLine, options);
            if (tradeEvent != null)
            {
                MaterialTradeEvent?.Invoke(this, new MaterialTradeEventArgs(tradeEvent));
            }
        }

        private void ProcessEngineerCraftEvent(string journalLine, JsonSerializerOptions options)
        {
            var craftEvent = JsonSerializer.Deserialize<EngineerCraftEvent>(journalLine, options);
            if (craftEvent != null)
            {
                EngineerCraftEvent?.Invoke(this, new EngineerCraftEventArgs(craftEvent));
            }
        }
    }
}