using EliteDataRelay.Models;
using System;
using System.Collections.Generic;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Provides data for the <see cref="IJournalWatcherService.LocationChanged"/> event.
    /// </summary>
    public class LocationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the current star system.
        /// </summary>
        public string StarSystem { get; }
        public bool IsNewSystem { get; }
        public double[] StarPos { get; }
        public long? SystemAddress { get; }
        public DateTime Timestamp { get; }

        public LocationChangedEventArgs(string starSystem, double[] starPos, bool isNewSystem, long? systemAddress, DateTime timestamp)
        {
            StarSystem = starSystem;
            IsNewSystem = isNewSystem;
            StarPos = starPos;
            SystemAddress = systemAddress;
            Timestamp = timestamp;
        }
    }
}