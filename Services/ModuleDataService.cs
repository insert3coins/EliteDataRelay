using EliteDataRelay.Models;
using System;
using System.Collections.Generic;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Provides static data and helper methods for ship modules.
    /// </summary>
    public static class ModuleDataService
    {
        // Data sourced from https://github.com/EDCD/coriolis-data/blob/master/modules/standard/power_plant.json
        private static readonly Dictionary<string, double> _powerPlantCapacity = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            { "int_powerplant_size2_a", 12.00 },
            { "int_powerplant_size2_b", 10.40 },
            { "int_powerplant_size2_c", 8.80 },
            { "int_powerplant_size2_d", 7.20 },
            { "int_powerplant_size2_e", 5.60 },

            { "int_powerplant_size3_a", 15.60 },
            { "int_powerplant_size3_b", 13.60 },
            { "int_powerplant_size3_c", 11.60 },
            { "int_powerplant_size3_d", 9.60 },
            { "int_powerplant_size3_e", 7.60 },

            { "int_powerplant_size4_a", 20.40 },
            { "int_powerplant_size4_b", 17.60 },
            { "int_powerplant_size4_c", 14.80 },
            { "int_powerplant_size4_d", 12.00 },
            { "int_powerplant_size4_e", 9.20 },

            { "int_powerplant_size5_a", 26.40 },
            { "int_powerplant_size5_b", 22.80 },
            { "int_powerplant_size5_c", 19.20 },
            { "int_powerplant_size5_d", 15.60 },
            { "int_powerplant_size5_e", 12.00 },

            { "int_powerplant_size6_a", 33.60 },
            { "int_powerplant_size6_b", 29.20 },
            { "int_powerplant_size6_c", 24.80 },
            { "int_powerplant_size6_d", 20.40 },
            { "int_powerplant_size6_e", 16.00 },

            { "int_powerplant_size7_a", 42.00 },
            { "int_powerplant_size7_b", 36.80 },
            { "int_powerplant_size7_c", 31.60 },
            { "int_powerplant_size7_d", 26.40 },
            { "int_powerplant_size7_e", 21.20 },

            { "int_powerplant_size8_a", 51.60 },
            { "int_powerplant_size8_b", 45.20 },
            { "int_powerplant_size8_c", 38.80 },
            { "int_powerplant_size8_d", 32.40 },
            { "int_powerplant_size8_e", 26.00 },
        };

        /// <summary>
        /// Gets the base power capacity of a power plant module.
        /// </summary>
        /// <param name="item">The internal name of the power plant (e.g., "int_powerplant_size7_a").</param>
        /// <returns>The power capacity in MW, or 0 if not found.</returns>
        public static double GetPowerPlantCapacity(string item)
        {
            if (string.IsNullOrEmpty(item))
            {
                return 0;
            }

            return _powerPlantCapacity.TryGetValue(item, out double capacity) ? capacity : 0;
        }

        /// <summary>
        /// Gets a user-friendly display name for a module, including engineering info.
        /// </summary>
        /// <param name="module">The ship module.</param>
        /// <returns>A formatted display name.</returns>
        public static string GetModuleDisplayName(ShipModule module)
        {
            string name = ItemNameService.TranslateModuleName(module.Item) ?? "Unknown Module";

            if (module.Engineering != null)
            {
                name += $" [G{module.Engineering.Level}]";
            }

            return name;
        }
    }
}