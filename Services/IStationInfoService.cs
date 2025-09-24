using EliteDataRelay.Models;
using System;

namespace EliteDataRelay.Services
{
    public interface IStationInfoService : IDisposable
    {
        event EventHandler<StationInfoData>? StationInfoUpdated;
        void Start();
        void Stop();
    }
}