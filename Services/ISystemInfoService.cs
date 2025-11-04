using EliteDataRelay.Models;
using System;

namespace EliteDataRelay.Services
{
    public interface ISystemInfoService : IDisposable
    {
        event EventHandler<SystemInfoData>? SystemInfoUpdated;
        void Start();
        void Stop();
        SystemInfoData? GetLastSystemInfo();
        void RequestFetch(string systemName);
    }
}
