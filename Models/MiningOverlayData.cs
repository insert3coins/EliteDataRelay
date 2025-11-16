using System;
using System.Collections.Generic;
using EliteDataRelay.Models.Mining;

namespace EliteDataRelay.Models
{
    public class MiningOverlayData
    {
        public string Location { get; set; } = "Unknown";
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public double RefinedPerHour { get; set; }
        public int ProspectorsFired { get; set; }
        public int CollectorsDeployed { get; set; }
        public int AsteroidsProspected { get; set; }
        public int AsteroidsCracked { get; set; }
        public int TotalRefined { get; set; }
        public int MaterialsCollected { get; set; }
        public int LowContent { get; set; }
        public int MedContent { get; set; }
        public int HighContent { get; set; }
        public int? LimpetsRemaining { get; set; }
        public int? CargoFree { get; set; }
        public int? CargoCapacity { get; set; }
        public IReadOnlyList<MiningOverlayCommodity> Commodities { get; set; } = Array.Empty<MiningOverlayCommodity>();
    }

    public class MiningOverlayCommodity
    {
        public string Name { get; set; } = string.Empty;
        public MiningItemType Type { get; set; }
        public int RefinedCount { get; set; }
        public int CollectedCount { get; set; }
        public int Prospected { get; set; }
        public double HitRate { get; set; }
        public double MinPercentage { get; set; }
        public double MaxPercentage { get; set; }
        public int Motherlodes { get; set; }
        public int LowContent { get; set; }
        public int MedContent { get; set; }
        public int HighContent { get; set; }
    }
}
