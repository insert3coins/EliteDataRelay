using System;

namespace EliteDataRelay.Models.FleetCarrier
{
    /// <summary>
    /// Represents a single commodity record tracked on a Fleet Carrier.
    /// </summary>
    public sealed class FleetCarrierCommodity
    {
        public FleetCarrierCommodity(string commodityName, string? localizedName, bool stolen)
        {
            CommodityName = string.IsNullOrWhiteSpace(commodityName) ? "Unknown" : commodityName;
            LocalizedName = localizedName;
            Stolen = stolen;
        }

        public string CommodityName { get; }
        public string? LocalizedName { get; set; }
        public bool Stolen { get; }
        public bool Rare { get; set; }
        public bool BlackMarket { get; set; }
        public long StockCount { get; set; }
        public long OutstandingPurchaseOrders { get; set; }
        public long SalePrice { get; set; }

        public string DisplayName => string.IsNullOrWhiteSpace(LocalizedName) ? CommodityName : LocalizedName!;

        private string CommodityKey => CommodityName.Trim().ToLowerInvariant();

        public bool Matches(string commodityName, bool stolen)
        {
            if (CommodityName.Equals(commodityName ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                return Stolen == stolen;
            }
            return CommodityKey.Equals((commodityName ?? string.Empty).Trim().ToLowerInvariant(), StringComparison.Ordinal);
        }

        public FleetCarrierCommodity Clone()
        {
            return new FleetCarrierCommodity(CommodityName, LocalizedName, Stolen)
            {
                Rare = Rare,
                BlackMarket = BlackMarket,
                StockCount = StockCount,
                OutstandingPurchaseOrders = OutstandingPurchaseOrders,
                SalePrice = SalePrice
            };
        }
    }
}
