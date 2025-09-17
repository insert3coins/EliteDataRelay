using System;
using EliteCargoMonitor.Models;

namespace EliteCargoMonitor.Services
{
    /// <summary>
    /// Defines the contract for a service that monitors Elite Dangerous journal files
    /// for events like cargo capacity, location changes, and cargo inventory snapshots.
    /// </summary>
    public interface IJournalWatcherService : IDisposable
    {
        /// <summary>
        /// Occurs when the cargo capacity is updated from a journal event.
        /// </summary>
        event EventHandler<CargoCapacityEventArgs>? CargoCapacityChanged;

        /// <summary>
        /// Starts monitoring the journal files for relevant events.
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// Stops monitoring the journal files.
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// Gets a value indicating whether the service is currently monitoring.
        /// </summary>
        bool IsMonitoring { get; }

        /// <summary>
        /// Gets the path to the journal directory being monitored.
        /// </summary>
        string JournalDirectoryPath { get; }

        /// <summary>
        /// Occurs when the player's location (StarSystem) changes.
        /// </summary>
        event EventHandler<LocationChangedEventArgs>? LocationChanged;

        /// <summary>
        /// Occurs when a 'Cargo' event is read from the journal, providing a full inventory snapshot.
        /// </summary>
        event EventHandler<CargoInventoryEventArgs>? CargoInventoryChanged;
    }
}