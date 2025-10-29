using System;

namespace EliteDataRelay.Services
{
    public interface IFileMonitoringService : IDisposable
    {
        event Action<string>? FileChanged;
        bool IsMonitoring { get; }
        void StartMonitoring();
        void StopMonitoring();
    }
}
