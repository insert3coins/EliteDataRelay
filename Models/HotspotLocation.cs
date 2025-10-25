using System;

namespace EliteDataRelay.Models
{
    public class HotspotLocation
    {
        public string StarSystem { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string RingType { get; set; } = string.Empty;
        public string Mineral { get; set; } = string.Empty;
        public double DistanceFromStar { get; set; }
            = double.NaN;
        public double OverlapQuality { get; set; }
            = double.NaN;
        public string Notes { get; set; } = string.Empty;

        public HotspotLocation Clone() => new()
        {
            StarSystem = StarSystem,
            Body = Body,
            RingType = RingType,
            Mineral = Mineral,
            DistanceFromStar = DistanceFromStar,
            OverlapQuality = OverlapQuality,
            Notes = Notes
        };
    }

    public class HotspotSearchCriteria
    {
        public string? Mineral { get; set; }
        public string? RingType { get; set; }
        public double? MaxDistance { get; set; }
        public string? SystemContains { get; set; }
    }
}
