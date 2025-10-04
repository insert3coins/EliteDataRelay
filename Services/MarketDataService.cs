using EliteDataRelay.Models.Market;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace EliteDataRelay.Services
{
    public class MarketDataService : IMarketDataService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string EdsmSystemsApiUrl = "https://www.edsm.net/api-v1/systems";
        private const string EdsmSystemStationsApiUrl = "https://www.edsm.net/api-system-v1/stations";
        private const string EdsmMarketApiUrl = "https://www.edsm.net/api-system-v1/stations/market";

        public MarketDataService()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "EliteDataRelay/1.0");
        }

        public async Task<List<MarketInfo>> FindBestSellLocationsAsync(string? systemName, string commodityName)
        {
            // Player wants to SELL → Find stations that BUY the commodity
            var marketData = await GetMarketDataInRadiusAsync(systemName, commodityName);
            
            return marketData.Where(m => m.HaveMarket && 
                                   m.Commodity != null && 
                                   m.Commodity.Demand > 0)         // Station has actual demand
                             .OrderByDescending(m => m.Commodity!.BuyPrice) // Highest price first
                                .ToList();
        }

        public async Task<List<MarketInfo>> FindBestBuyLocationsAsync(string? systemName, string commodityName)
        {
            // Player wants to BUY → Find stations that SELL the commodity
            var marketData = await GetMarketDataInRadiusAsync(systemName, commodityName);
            
            return marketData.Where(m => m.HaveMarket && 
                                   m.Commodity != null && 
                                   m.Commodity.SellPrice > 0 &&    // Station sells to players
                                   m.Commodity.Stock > 0)          // Station has stock available
                             .OrderBy(m => m.Commodity!.SellPrice)  // Lowest price first
                                .ToList();
        }

        private async Task<List<MarketInfo>> GetMarketDataInRadiusAsync(string? systemName, string commodityName)
        {
            var referenceSystem = string.IsNullOrWhiteSpace(systemName) ? "Sol" : systemName;
            const int radius = 50; // Search within a 50 LY radius

            // Step 1: Find all systems within the radius
            var nearbySystems = await FindSystemsInSphereAsync(referenceSystem, radius, commodityName);
            if (!nearbySystems.Any())
            {
                Trace.WriteLine($"[MarketDataService] No systems found within {radius} LY of {referenceSystem}.");
                return new List<MarketInfo>();
            }

            Trace.WriteLine($"[MarketDataService] Found {nearbySystems.Count} systems. Now searching for '{commodityName}' markets...");

            // Step 2: For each system, find its stations, then query the market for the specific commodity.
            var tasks = nearbySystems.Select(sys => FindAndProcessStationsInSystem(sys, commodityName));
            var results = await Task.WhenAll(tasks);

            // Flatten the list of lists into a single list and filter out empty results.
            var allMarketData = results.SelectMany(list => list).ToList();

            Trace.WriteLine($"[MarketDataService] Total market results found: {allMarketData.Count}");
            return allMarketData;
        }

        private async Task<List<NearbySystem>> FindSystemsInSphereAsync(string systemName, int radius, string commodityName)
        {
            var url = $"{EdsmSystemsApiUrl}?systemName={Uri.EscapeDataString(systemName)}&radius={radius}&showCoordinates=0";
            Trace.WriteLine($"[MarketDataService] Finding systems with URL: {url}");

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var systems = JsonSerializer.Deserialize<List<NearbySystem>>(json, options);
                return systems ?? new List<NearbySystem>();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[MarketDataService] Error finding nearby systems for {systemName}: {ex}");
                return new List<NearbySystem>();
            }
        }

        private async Task<List<MarketInfo>> FindAndProcessStationsInSystem(NearbySystem system, string commodityName)
        {
            var url = $"{EdsmSystemStationsApiUrl}?systemName={Uri.EscapeDataString(system.Name)}";
            Trace.WriteLine($"[MarketDataService]   -> Finding stations in '{system.Name}' with URL: {url}");

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var stationResponse = JsonSerializer.Deserialize<SystemStationsResponse>(json, options);

                if (stationResponse?.Stations == null || !stationResponse.Stations.Any())
                {
                    Trace.WriteLine($"[MarketDataService]   -> No stations found in '{system.Name}'.");
                    return new List<MarketInfo>();
                }

                Trace.WriteLine($"[MarketDataService]   -> Found {stationResponse.Stations.Count} station(s) in '{system.Name}'. Querying markets...");

                // For each station with a market, get its detailed market data.
                var tasks = stationResponse.Stations
                                           .Where(s => s.HaveMarket)
                                           .Select(s => GetMarketDataForStationAsync(system.Name, s.Name, commodityName, system.Distance));

                var results = await Task.WhenAll(tasks);
                return results.Where(r => r != null).Select(r => r!).ToList();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[MarketDataService] Error finding stations in {system.Name}: {ex.Message}");
                return new List<MarketInfo>();
            }
        }

        private async Task<MarketInfo?> GetMarketDataForStationAsync(string systemName, string stationName, string commodityName, double distance)
        {
            var url = $"{EdsmMarketApiUrl}?systemName={Uri.EscapeDataString(systemName)}&stationName={Uri.EscapeDataString(stationName)}";

            Trace.WriteLine($"[MarketDataService]   -> Querying market at '{stationName}' in '{systemName}'");

            try
            {
                var response = await _httpClient.GetAsync(url);

                // Add status code check
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Trace.WriteLine($"[MarketDataService]   -> Market not found at {stationName} in {systemName} (404)");
                    return null;
                }

                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                // DEBUG: Log the raw JSON to see what we're getting
                Trace.WriteLine($"[MarketDataService]   -> Raw JSON response: {json.Substring(0, Math.Min(200, json.Length))}...");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var market = JsonSerializer.Deserialize<MarketInfo>(json, options);

                if (market == null)
                {
                    Trace.WriteLine($"[MarketDataService]   -> Failed to deserialize market data");
                    return null;
                }

                // Find the specific commodity
                var targetCommodity = market.Commodities?.FirstOrDefault(c =>
                    c.Name.Equals(commodityName, StringComparison.OrdinalIgnoreCase));

                if (targetCommodity == null)
                {
                    Trace.WriteLine($"[MarketDataService]   -> Commodity '{commodityName}' not found in market");
                    return null;
                }

                Trace.WriteLine($"[MarketDataService]   -> FOUND '{commodityName}': Buy={targetCommodity.BuyPrice}, Sell={targetCommodity.SellPrice}, Demand={targetCommodity.Demand}, Stock={targetCommodity.Stock}");

                // Return ALL found commodities and let the calling method filter
                return new MarketInfo
                {
                    Id = market.Id,
                    Name = market.Name,
                    StationName = stationName,
                    SystemName = systemName,
                    Type = market.Type,
                    DistanceToArrival = distance,
                    HaveMarket = true,
                    Commodities = new List<CommodityMarketData> { targetCommodity }
                };
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[MarketDataService] Error fetching market data for {stationName} in {systemName}: {ex.Message}");
                return null;
            }
        }

        private class NearbySystem
        {
            public string Name { get; set; } = string.Empty;
            public double Distance { get; set; }
        }

        private class SystemStationsResponse
        {
            public List<Station> Stations { get; set; } = new List<Station>();
        }

        private class Station
        {
            public string Name { get; set; } = string.Empty;
            public bool HaveMarket { get; set; }
        }
    }
}