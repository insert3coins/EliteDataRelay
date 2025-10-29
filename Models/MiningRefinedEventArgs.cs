using System;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Provides data for the MiningRefined event.
    /// </summary>
    public class MiningRefinedEventArgs : EventArgs
    {
        /// <summary>
        /// The type of commodity that was refined (e.g., "platinum").
        /// </summary>
        public string CommodityType { get; }

        public MiningRefinedEventArgs(string commodityType) => CommodityType = commodityType;
    }
}
