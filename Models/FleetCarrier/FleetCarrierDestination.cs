using System;

namespace EliteDataRelay.Models.FleetCarrier
{
    /// <summary>
    /// Represents a pending Fleet Carrier jump destination.
    /// </summary>
    public sealed class FleetCarrierDestination
    {
        public string SystemName { get; set; } = "No Jump Set";
        public string BodyName { get; set; } = string.Empty;
        public ulong SystemAddress { get; set; }
        public DateTime DepartureTimeUtc { get; set; } = DateTime.MinValue;

        public bool HasDestination => !string.Equals(SystemName, "No Jump Set", StringComparison.Ordinal);

        public bool DepartureKnown => DepartureTimeUtc != DateTime.MinValue;

        public FleetCarrierDestination Clone()
        {
            return new FleetCarrierDestination
            {
                SystemName = SystemName,
                BodyName = BodyName,
                SystemAddress = SystemAddress,
                DepartureTimeUtc = DepartureTimeUtc
            };
        }

        public void Set(string systemName, string bodyName, ulong systemAddress, DateTime departureTimeUtc)
        {
            SystemName = string.IsNullOrWhiteSpace(systemName) ? "Unknown" : systemName;
            BodyName = bodyName ?? string.Empty;
            SystemAddress = systemAddress;
            DepartureTimeUtc = departureTimeUtc;
        }

        public void Reset()
        {
            SystemName = "No Jump Set";
            BodyName = string.Empty;
            SystemAddress = 0;
            DepartureTimeUtc = DateTime.MinValue;
        }
    }
}
