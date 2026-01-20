using System;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Represents a single journal event entry for the History tab.
    /// </summary>
    public sealed class JournalHistoryEntry
    {
        public DateTime TimestampUtc { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Category { get; set; } = "other";
        public string? StarSystem { get; set; }
        public string? Body { get; set; }
        public string? Station { get; set; }
        public string RawJson { get; set; } = string.Empty;
    }
}
