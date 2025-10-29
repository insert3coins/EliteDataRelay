using System;
using EliteDataRelay.Models;

namespace EliteDataRelay.Services
{
    public interface IShipLoadoutService : IDisposable
    {
        event EventHandler? ShipLoadoutUpdated;
        ShipLoadout? CurrentLoadout { get; }
        void Start();
        void Stop();
    }
}
