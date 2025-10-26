using System;
using System.Collections.Generic;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Represents a scanned body in the current system.
    /// </summary>
    public class ScannedBody
    {
        public string BodyName { get; set; } = string.Empty;
        public int? BodyID { get; set; }
        public string BodyType { get; set; } = string.Empty; // Star or Planet class
        public double? DistanceFromArrival { get; set; }
        public bool? Landable { get; set; }
        public bool WasDiscovered { get; set; }
        public bool WasMapped { get; set; }
        public bool IsMapped { get; set; } // SAA Scan completed
        public bool FirstFootfall { get; set; } // First commander to land on this body
        public string? TerraformState { get; set; }
        public List<Signal> Signals { get; set; } = new List<Signal>();
        public List<string> BiologicalSignals { get; set; } = new List<string>();
        public int? ProbesUsed { get; set; }
        public int? EfficiencyTarget { get; set; }
    }

    /// <summary>
    /// Represents the current exploration state for a system.
    /// </summary>
    public class SystemExplorationData
    {
        public string SystemName { get; set; } = string.Empty;
        public long? SystemAddress { get; set; }
        public int TotalBodies { get; set; }
        public int ScannedBodies { get; set; }
        public int MappedBodies { get; set; }
        public double FSSProgress { get; set; }
        public List<ScannedBody> Bodies { get; set; } = new List<ScannedBody>();
        public DateTime LastVisited { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Represents exploration session statistics.
    /// </summary>
    public class ExplorationSessionData
    {
        public int SystemsVisited { get; set; }
        public int TotalScans { get; set; }
        public int TotalMapped { get; set; }
        public int FirstDiscoveries { get; set; }
        public int FirstMappings { get; set; }
        public int FirstFootfalls { get; set; }
        public long EstimatedValue { get; set; }
        public long SoldValue { get; set; }
        public DateTime SessionStart { get; set; } = DateTime.Now;
    }
}
