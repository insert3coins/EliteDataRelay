using System;
using System.Text.Json.Serialization;

namespace EliteCargoMonitor.Models
{
    public class JournalEvent
    {
        [JsonPropertyName("event")]
        public string? Event { get; set; }
    }

    public class LoadoutEvent : JournalEvent
    {
        [JsonPropertyName("CargoCapacity")]
        public int CargoCapacity { get; set; }
    }
}