using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Provides data about commodities, such as their category, by reading from local data files.
    /// </summary>
    public static class CommodityDataService
    {
        private static readonly Dictionary<string, string> _commodityCategories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes the <see cref="CommodityDataService"/> class by loading commodity data from embedded resources.
        /// </summary>
        static CommodityDataService()
        {
            // User has added the CSVs as resources named 'commodity' and 'rare_commodity'
            LoadCommodityData(Properties.Resources.commodity, "commodity.csv");
            LoadCommodityData(Properties.Resources.rare_commodity, "rare_commodity.csv");
        }

        private static void LoadCommodityData(byte[] resourceBytes, string resourceName)
        {
            try
            {
                if (resourceBytes == null || resourceBytes.Length == 0)
                {
                    Debug.WriteLine($"[CommodityDataService] Resource content is empty or null: {resourceName}");
                    return;
                }

                var resourceContent = Encoding.UTF8.GetString(resourceBytes);

                // Read all lines from the resource string, skipping the header
                var lines = resourceContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Skip(1);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // The CSV structure is: id,symbol,category,name
                    var parts = line.Split(',');
                    if (parts.Length >= 3) // We need at least id, symbol, and category
                    {
                        // The key is the internal symbol/name (e.g., "gold"), which is the second column.
                        var symbol = parts[1].Trim();
                        var category = parts[2].Trim().Trim('"');
                        
                        if (!string.IsNullOrEmpty(symbol))
                        {
                            _commodityCategories[symbol] = category;
                        }
                    }
                }
                Debug.WriteLine($"[CommodityDataService] Loaded commodity data from resource '{resourceName}'");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CommodityDataService] Error loading from resource '{resourceName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the category for a given commodity name.
        /// </summary>
        /// <param name="commodityName">The internal (non-localized) name of the commodity.</param>
        /// <returns>The commodity's category, or "Unknown" if not found.</returns>
        public static string GetCategory(string commodityName) => 
            !string.IsNullOrEmpty(commodityName) && _commodityCategories.TryGetValue(commodityName, out var category) ? category : "Unknown";
    }
}