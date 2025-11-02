using System;

namespace EliteDataRelay.Models
{
    public class ImportProgress
    {
        public int TotalFiles { get; set; }
        public int CurrentFileIndex { get; set; } // 1-based
        public string CurrentFileName { get; set; } = string.Empty;
        public int CurrentFilePercent { get; set; }
        public int OverallPercent { get; set; }
        public string? Message { get; set; }
    }
}

