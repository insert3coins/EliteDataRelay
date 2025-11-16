using System.Collections.Generic;
using EliteDataRelay.Models.Mining;

namespace EliteDataRelay.Models
{
    public class ProspectorOverlayData
    {
        public MiningContent? Content { get; set; }
        public double RemainingPercent { get; set; }
        public string? Motherlode { get; set; }
        public bool IsDepleted { get; set; }
        public IReadOnlyList<ProspectorOverlayItem> Materials { get; set; } = System.Array.Empty<ProspectorOverlayItem>();
    }

    public class ProspectorOverlayItem
    {
        public string Name { get; set; } = string.Empty;
        public double Percentage { get; set; }
    }
}
