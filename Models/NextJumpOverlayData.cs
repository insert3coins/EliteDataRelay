namespace EliteDataRelay.Models
{
    public class NextJumpOverlayData
    {
        public string? TargetSystemName { get; set; }
        public string? StarClass { get; set; }
        public double? JumpDistanceLy { get; set; }
        public int? RemainingJumps { get; set; }
        public SystemInfoData? SystemInfo { get; set; }

        // Enhanced route context (SrvSurvey-inspired)
        public double? NextDistanceLy { get; set; }
        public double? TotalRemainingLy { get; set; }
        public int? CurrentJumpIndex { get; set; } // 0-based current position in route
        public int? TotalJumps { get; set; }
        public System.Collections.Generic.List<JumpHop>? Hops { get; set; }
    }

    public class JumpHop
    {
        public string Name { get; set; } = string.Empty;
        public string? StarClass { get; set; }
        public double? DistanceLy { get; set; }
        public bool IsScoopable { get; set; }
    }
}
