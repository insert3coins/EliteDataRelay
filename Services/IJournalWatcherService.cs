using System;

namespace EliteCargoMonitor.Services
{
    public interface IJournalWatcherService : IDisposable
    {
        event EventHandler<CargoCapacityEventArgs>? CargoCapacityChanged;
        void StartMonitoring();
        void StopMonitoring();
        bool IsMonitoring { get; }
        string JournalDirectoryPath { get; }
        event EventHandler<LocationChangedEventArgs>? LocationChanged;
    }
}