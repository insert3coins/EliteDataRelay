using System;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Provides data for the LaunchDrone event.
    /// </summary>
    public class LaunchDroneEventArgs : EventArgs
    {
        /// <summary>
        /// The type of drone launched (e.g., "Collector").
        /// </summary>
        public string Type { get; }

        public LaunchDroneEventArgs(string type) => Type = type;
    }
}