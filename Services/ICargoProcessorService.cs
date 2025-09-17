using System;
using System.Threading.Tasks;
using EliteCargoMonitor.Models;

namespace EliteCargoMonitor.Services
{
    /// <summary>
    /// Defines the contract for a service that processes the `Cargo.json` file.
    /// </summary>
    public interface ICargoProcessorService
    {
        /// <summary>
        /// Occurs when a new cargo snapshot has been successfully processed.
        /// </summary>
        event EventHandler<CargoProcessedEventArgs>? CargoProcessed;

        /// <summary>
        /// Asynchronously reads, parses, and processes the `Cargo.json` file.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ProcessCargoFileAsync();
    }

    /// <summary>
    /// Provides data for the <see cref="ICargoProcessorService.CargoProcessed"/> event.
    /// </summary>
    public class CargoProcessedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the processed cargo snapshot.
        /// </summary>
        public CargoSnapshot Snapshot { get; }
        /// <summary>
        /// Gets the SHA256 hash of the snapshot to identify unique cargo states.
        /// </summary>
        public string Hash { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CargoProcessedEventArgs"/> class.
        /// </summary>
        /// <param name="snapshot">The cargo snapshot.</param>
        /// <param name="hash">The hash of the snapshot.</param>
        public CargoProcessedEventArgs(CargoSnapshot snapshot, string hash)
        {
            Snapshot = snapshot;
            Hash = hash;
        }
    }
}