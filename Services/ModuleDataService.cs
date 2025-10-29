using EliteDataRelay.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Provides static data and helper methods related to ship modules.
    /// </summary>
    public static class ModuleDataService
    {
        private static readonly Dictionary<string, ModuleInfo> ModulesBySymbol = new Dictionary<string, ModuleInfo>(StringComparer.OrdinalIgnoreCase);

        static ModuleDataService()
        {
            LoadModulesData();
        }

        private static void LoadModulesData()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var resourceName = "EliteDataRelay.Resources.outfitting.csv";

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Logger.Info($"[ModuleDataService] Error: Embedded resource '{resourceName}' not found.");
                    return;
                }
                using (StreamReader reader = new StreamReader(stream))
                {
                    // Skip header
                    reader.ReadLine();

                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var values = ParseCsvLine(line);
                        if (values.Length >= 8)
                        {
                            var moduleInfo = new ModuleInfo
                            {
                                Symbol = values[1].ToLowerInvariant(),
                                Category = values[2],
                                Name = values[3],
                                Mount = values[4],
                                Class = int.TryParse(values[6], out int c) ? c : 0,
                                Rating = values[7]
                            };
                            ModulesBySymbol[moduleInfo.Symbol] = moduleInfo;
                        }
                    }
                }
            }
        }

        private static string[] ParseCsvLine(string line)
        {
            // Simple CSV parser that handles quoted fields.
            var matches = Regex.Matches(line, "(\"[^\"]*\"|[^,]*),?");
            return matches.Cast<Match>().Select(m => m.Value.Trim('"', ',')).ToArray();
        }

        // A dictionary to map internal power plant names to their power capacity in MW.
        private static readonly Dictionary<string, double> PowerPlantCapacities = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
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
        /// Gets the base power capacity of a stock power plant module.
        /// </summary>
        /// <param name="internalName">The internal name of the power plant (e.g., "int_powerplant_size7_a").</param>
        /// <returns>The power capacity in MW, or 0 if not found.</returns>
        public static double GetPowerPlantCapacity(string internalName)
        {
            if (string.IsNullOrEmpty(internalName))
            {
                return 0;
            }

            return PowerPlantCapacities.TryGetValue(internalName, out double capacity) ? capacity : 0;
        }

        /// <summary>
        /// Gets a user-friendly display name for a module.
        /// </summary>
        /// <param name="module">The ship module.</param>
        /// <param name="shipInternalName">The internal name of the ship the module is on.</param>
        /// <returns>A formatted display name.</returns>
        public static string GetModuleDisplayName(ShipModule module, string shipInternalName)
        {
            // Prioritize the in-game localised name if it exists.
            if (!string.IsNullOrEmpty(module.ItemLocalised))
            {
                return module.ItemLocalised;
            }
            
            if (string.IsNullOrEmpty(module.Item) || !ModulesBySymbol.TryGetValue(module.Item.ToLowerInvariant(), out var moduleInfo))
            {
                // Fallback to the internal name if not found in our file.
                return module.Item ?? string.Empty;
            }

            // For categories like Armour, the name is sufficient.
            if (moduleInfo.Category == "Armour")
            {
                return moduleInfo.Name;
            }

            string mount = !string.IsNullOrEmpty(moduleInfo.Mount) ? $"{moduleInfo.Mount} " : "";
            string name = moduleInfo.Name;

            return moduleInfo.Class > 0 ? $"{moduleInfo.Class}{moduleInfo.Rating} {mount}{name}" : $"{mount}{name}";
        }
    }
}
