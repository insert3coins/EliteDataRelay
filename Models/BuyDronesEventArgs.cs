using System;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Provides data for the BuyDrones event.
    /// </summary>
    public class BuyDronesEventArgs : EventArgs
    {
        public int Count { get; }
        public long TotalCost { get; }

        public BuyDronesEventArgs(int count, long totalCost) => TotalCost = totalCost;
    }
}