using System;
using EliteCargoMonitor.Models;

namespace EliteCargoMonitor.Services
{
    /// <summary>
    /// Service interface for processing cargo data from the Elite Dangerous cargo file
    /// </summary>
    public interface ICargoProcessorService
    {
        /// <summary>
        /// Event raised when new cargo data has been successfully processed
        /// </summary>
        event EventHandler<CargoProcessedEventArgs>? CargoProcessed;

        /// <summary>
        /// Process the cargo file and extract cargo snapshot data
        /// </summary>
        void ProcessCargoFile();
    }

    /// <summary>
    /// Event arguments for cargo processing completion
    /// </summary>
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