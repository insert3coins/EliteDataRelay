using System;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Provides data for the MarketBuy event.
    /// </summary>
    public class MarketBuyEventArgs : EventArgs
    {
        public string CommodityType { get; }
        public int Count { get; }
        public long TotalCost { get; }

        public MarketBuyEventArgs(string commodityType, int count, long totalCost)
        {
            CommodityType = commodityType ?? string.Empty;
            Count = count;
            TotalCost = totalCost;
        }
    }
}
