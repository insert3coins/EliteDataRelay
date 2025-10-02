using System;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Provides data for the MarketSell event.
    /// </summary>
    public class MarketSellEventArgs : EventArgs
    {
        public string Commodity { get; }
        public int Count { get; }
        public long TotalSale { get; }

        public MarketSellEventArgs(string commodity, int count, long totalSale)
        {
            Commodity = commodity;
            Count = count;
            TotalSale = totalSale;
        }
    }
}