using System;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Provides data for the CargoCollected event.
    /// </summary>
    public class CargoCollectedEventArgs : EventArgs
    {
        /// <summary>
        /// The quantity of cargo units collected.
        /// </summary>
        public int Quantity { get; }

        public CargoCollectedEventArgs(int quantity) => Quantity = quantity;
    }
}