using System;
using System.Collections.Generic;
using System.Linq;
using EliteDataRelay.Models.Journal;

namespace EliteDataRelay.Models.FleetCarrier
{
    public enum CarrierCrewStatus
    {
        Inactive,
        Active,
        Suspended
    }

    /// <summary>
    /// Snapshot of a carrier's state (used by the Fleet Carrier tracker service and UI).
    /// </summary>
    public sealed class FleetCarrierState
    {
        public ulong CarrierId { get; set; }
        public string CarrierType { get; set; } = "FleetCarrier";
        public string Name { get; set; } = string.Empty;
        public string Callsign { get; set; } = string.Empty;
        public string DockingAccess { get; set; } = string.Empty;
        public bool AllowNotorious { get; set; }
        public long FuelLevel { get; set; }
        public long Balance { get; set; }
        public string StarSystem { get; set; } = "Unknown";
        public ulong SystemAddress { get; set; }
        public int BodyId { get; set; }
        public FleetCarrierDestination Destination { get; set; } = new();
        public DateTime? JumpDepartureUtc { get; set; }
        public DateTime? CooldownCompleteUtc { get; set; }
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

        public List<FleetCarrierCommodity> Stock { get; } = new();
        public Dictionary<string, CarrierCrewStatus> Crew { get; } = new(StringComparer.OrdinalIgnoreCase);

        public string DisplayName => string.IsNullOrWhiteSpace(Name)
            ? string.IsNullOrWhiteSpace(Callsign) ? "Unknown Carrier" : Callsign
            : string.IsNullOrWhiteSpace(Callsign) ? Name : $"{Name} ({Callsign})";

        public void ApplyStats(CarrierStatsEvent.CarrierStatsEventArgs stats)
        {
            CarrierId = stats.CarrierID;
            CarrierType = stats.CarrierType ?? CarrierType;
            Callsign = stats.Callsign ?? Callsign;
            Name = stats.Name ?? Name;
            DockingAccess = stats.DockingAccess ?? DockingAccess;
            AllowNotorious = stats.AllowNotorious;
            FuelLevel = stats.FuelLevel;
            Balance = stats.Finance?.CarrierBalance ?? Balance;
            AssignCrew(stats.Crew);
            LastUpdatedUtc = DateTime.UtcNow;
        }

        public void ApplyLocation(CarrierLocationEvent.CarrierLocationEventArgs location)
        {
            StarSystem = location.StarSystem ?? StarSystem;
            SystemAddress = location.SystemAddress;
            BodyId = location.BodyID;
            if (Destination.HasDestination)
            {
                Destination.Reset();
            }
            JumpDepartureUtc = null;
            CooldownCompleteUtc = null;
            LastUpdatedUtc = DateTime.UtcNow;
        }

        public void AssignCrew(IReadOnlyList<CarrierStatsEvent.CarrierCrewMember> crew)
        {
            Crew.Clear();
            foreach (var member in crew)
            {
                var role = string.IsNullOrWhiteSpace(member.CrewRole) ? "Unknown" : member.CrewRole;
                var status = member.Activated
                    ? (member.Enabled ? CarrierCrewStatus.Active : CarrierCrewStatus.Suspended)
                    : CarrierCrewStatus.Inactive;

                Crew[role] = status;
            }
        }

        public void UpdateCrewStatus(string crewRole, string operation)
        {
            var role = string.IsNullOrWhiteSpace(crewRole) ? "Unknown" : crewRole;
            CarrierCrewStatus status;
            switch ((operation ?? string.Empty).ToLowerInvariant())
            {
                case "activate":
                case "resume":
                    status = CarrierCrewStatus.Active;
                    break;
                case "pause":
                    status = CarrierCrewStatus.Suspended;
                    break;
                case "deactivate":
                    status = CarrierCrewStatus.Inactive;
                    break;
                default:
                    return;
            }

            Crew[role] = status;
            LastUpdatedUtc = DateTime.UtcNow;
        }

        public FleetCarrierCommodity GetOrAddCommodity(string commodityName, string? localizedName, bool stolen)
        {
            var existing = Stock.FirstOrDefault(c => c.Matches(commodityName, stolen));
            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(localizedName))
                {
                    existing.LocalizedName = localizedName;
                }
                return existing;
            }

            var commodity = new FleetCarrierCommodity(commodityName, localizedName, stolen);
            Stock.Add(commodity);
            return commodity;
        }

        public void RemoveIfEmpty(FleetCarrierCommodity commodity)
        {
            if (commodity.StockCount <= 0 && commodity.OutstandingPurchaseOrders <= 0 && commodity.SalePrice <= 0)
            {
                Stock.Remove(commodity);
            }
        }

        public FleetCarrierState Clone()
        {
            var clone = new FleetCarrierState
            {
                CarrierId = CarrierId,
                CarrierType = CarrierType,
                Name = Name,
                Callsign = Callsign,
                DockingAccess = DockingAccess,
                AllowNotorious = AllowNotorious,
                FuelLevel = FuelLevel,
                Balance = Balance,
                StarSystem = StarSystem,
                SystemAddress = SystemAddress,
                BodyId = BodyId,
                JumpDepartureUtc = JumpDepartureUtc,
                CooldownCompleteUtc = CooldownCompleteUtc,
                LastUpdatedUtc = LastUpdatedUtc,
                Destination = Destination.Clone()
            };

            foreach (var kvp in Crew)
            {
                clone.Crew[kvp.Key] = kvp.Value;
            }

            foreach (var commodity in Stock)
            {
                clone.Stock.Add(commodity.Clone());
            }

            return clone;
        }
    }
}
