using System;
using EliteDataRelay.Models;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Defines the contract for a service that processes cargo data from Cargo.json.
    /// </summary>
    public interface ICargoProcessorService
    {
        event EventHandler<CargoProcessedEventArgs>? CargoProcessed;
        bool ProcessCargoFile(bool force = false);
        System.Threading.Tasks.Task<bool> ProcessCargoFileAsync(bool force = false);
        void Reset();
    }
}

