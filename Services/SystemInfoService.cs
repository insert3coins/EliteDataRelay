using EliteDataRelay.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// A service to track information about the current star system, including stars and stations, based on journal events.
    /// </summary>
    public class SystemInfoService
    {
        public string SystemName { get; private set; } = "Unknown";
        public List<string> Stars { get; } = new List<string>();
        public List<string> Stations { get; } = new List<string>();
        public List<string> Bodies { get; } = new List<string>();
        public long? SystemAddress { get; private set; }
        
        private readonly HashSet<string> _discoveredStars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _discoveredStations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _discoveredBodies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Handles a location change event, clearing data if it's a new system.
        /// </summary>
        public void HandleLocationChange(LocationChangedEventArgs args)
        {
            if (args.IsNewSystem)
            {
                ClearSystemData();
            }

            // Always update the name and address to stay in sync, even if it's not a "new" system.
            SystemName = args.StarSystem;
            if (args.SystemAddress.HasValue)
            {
                SystemAddress = args.SystemAddress;
            }

            if (args.Stations != null)
            {
                foreach (var station in args.Stations)
                {
                    if (!_discoveredStations.Contains(station.Name.Trim()))
                    {
                        _discoveredStations.Add(station.Name.Trim());
                        string prettyType = PrettifyStationType(station.Type);
                        Stations.Add($"{station.Name.Trim()} ({prettyType})");
                    }
                }
                Stations.Sort();
            }
        }

        /// <summary>
        /// Handles a body scan event, adding stars to the list.
        /// </summary>
        public void HandleScan(ScanEventArgs args)
        {
            // We only care about scans in the current system.
            if (args.StarSystem != SystemName) return;

            var scanData = args.ScanData;
            if (scanData.StarType != null && scanData.BodyName != null && !_discoveredStars.Contains(scanData.BodyName))
            {
                _discoveredStars.Add(scanData.BodyName);
                // e.g., "Procyon (G-Type Star)"
                string starInfo = $"{scanData.BodyName} ({scanData.StarType})";
                Stars.Add(starInfo);
                Stars.Sort();
            }
            else if (scanData.PlanetClass != null && scanData.BodyName != null && !_discoveredBodies.Contains(scanData.BodyName))
            {
                _discoveredBodies.Add(scanData.BodyName);
                // e.g., "Earth (Earth-like world)"
                string bodyInfo = $"{scanData.BodyName} ({scanData.PlanetClass})";
                Bodies.Add(bodyInfo);
                Bodies.Sort();
            }
        }

        /// <summary>
        /// Handles a dockable body event, adding stations to the list.
        /// </summary>
        public void HandleDockableBody(DockableBodyEventArgs args)
        {
            // Only add the station if its SystemAddress matches our current known SystemAddress.
            // This is crucial for filtering out FSS signals from neighboring systems.
            if (this.SystemAddress.HasValue && args.SystemAddress == this.SystemAddress &&
                !_discoveredStations.Contains(args.StationName.Trim()))
            {
                _discoveredStations.Add(args.StationName.Trim());
                // e.g., "Jameson Memorial (Orbis Starport)"
                string prettyType = PrettifyStationType(args.StationType);
                string stationInfo = $"{args.StationName.Trim()} ({prettyType})";
                Stations.Add(stationInfo);
                Stations.Sort();
            }
        }

        /// <summary>
        /// Clears all stored system data.
        /// </summary>
        private void ClearSystemData()
        {
            SystemName = "Unknown";
            SystemAddress = null;
            Stars.Clear();
            Stations.Clear();
            Bodies.Clear();
            _discoveredStars.Clear();
            _discoveredStations.Clear();
            _discoveredBodies.Clear();
        }

        private static string PrettifyStationType(string type)
        {
            // The journal uses a mix of internal and friendly names. This method normalizes them.
            return type.ToLowerInvariant() switch
            {
                "coriolis" => "Coriolis Starport",
                "orbis" => "Orbis Starport",
                "stationcoriolis" => "Coriolis Starport",
                "outpost" => "Outpost",
                "stationoutpost" => "Outpost",
                "asteroidbase" => "Asteroid Base",
                "megaship" => "Megaship",
                "fleetcarrier" => "Fleet Carrier",
                // A generic fallback for other station types that might appear
                "station" => "Station",
                // Default to returning the original type, but capitalized, if it's not in our list.
                _ => type.Length > 1 ? char.ToUpperInvariant(type[0]) + type.Substring(1) : type
            };
        }
    }
}