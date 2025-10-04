using EliteDataRelay.Models.Market;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EliteDataRelay.Services
{
    public interface IMarketDataService
    {
        Task<List<MarketInfo>> FindBestSellLocationsAsync(string? systemName, string commodityName);
        Task<List<MarketInfo>> FindBestBuyLocationsAsync(string? systemName, string commodityName);
    }
}