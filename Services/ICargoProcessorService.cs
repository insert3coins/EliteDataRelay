using System;
using EliteCargoMonitor.Models;

namespace EliteCargoMonitor.Services
{
    public interface ICargoProcessorService
    {
        event EventHandler<CargoProcessedEventArgs>? CargoProcessed;

        void ProcessCargoFile();
    }

    public class CargoProcessedEventArgs : EventArgs
    {
        public CargoSnapshot Snapshot { get; }
        public string Hash { get; }

        public CargoProcessedEventArgs(CargoSnapshot snapshot, string hash)
        {
            Snapshot = snapshot;
            Hash = hash;
        }
    }
}