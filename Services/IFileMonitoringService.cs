using System;

namespace EliteDataRelay.Services
{
    public interface IFileMonitoringService : IDisposable
    {
        event Action? FileChanged;
        bool IsMonitoring { get; }
        void StartMonitoring();
        void StopMonitoring();
    }
}