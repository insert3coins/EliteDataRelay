using System;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Provides data for the CargoProcessed event.
    /// </summary>
    public class CargoProcessedEventArgs : EventArgs
    {
        /// <summary>
        /// The snapshot of the cargo inventory that was processed.
        /// </summary>
        public CargoSnapshot Snapshot { get; }

        /// <summary>
        /// The hash of the processed cargo data, used for duplicate detection.
        /// </summary>
        public string Hash { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CargoProcessedEventArgs"/> class.
        /// </summary>
        /// <param name="snapshot">The cargo snapshot.</param>
        /// <param name="hash">The hash of the cargo data.</param>
        public CargoProcessedEventArgs(CargoSnapshot snapshot, string hash)
        {
            Snapshot = snapshot;
            Hash = hash;
        }
    }
}