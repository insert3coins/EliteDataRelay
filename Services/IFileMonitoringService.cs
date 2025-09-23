using System;

namespace EliteDataRelay.Services
{
    public interface IFileMonitoringService : IDisposable
    {
        event EventHandler? FileChanged;
        bool IsMonitoring { get; }
        void StartMonitoring();
        void StopMonitoring();
    }
}