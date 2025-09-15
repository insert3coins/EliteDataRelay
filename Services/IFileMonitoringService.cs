using System;

namespace EliteCargoMonitor.Services
{
    /// <summary>
    /// Service interface for monitoring file system changes to the cargo file
    /// </summary>
    public interface IFileMonitoringService
    {
        /// <summary>
        /// Event raised when the cargo file has changed and debounce period has elapsed
        /// </summary>
        event EventHandler? FileChanged;

        /// <summary>
        /// Start monitoring the cargo file for changes
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// Stop monitoring the cargo file
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// Gets whether the monitoring service is currently active
        /// </summary>
        bool IsMonitoring { get; }
    }
}