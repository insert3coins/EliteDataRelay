using System;
using System.Collections.Generic;

namespace EliteDataRelay.Models
{
    public class BackupSnapshot
    {
        public DateTime CreatedOn { get; set; }
        public MiningSessionPreferences Preferences { get; set; } = new();
        public List<MiningSessionRecord>? SessionHistory { get; set; }
            = new();
        public List<string>? Reports { get; set; } = new();
        public Dictionary<string, HotspotLocation>? HotspotBookmarks { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);
    }
}
