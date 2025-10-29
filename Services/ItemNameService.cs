using System.Collections.Generic;
using System.Linq;

namespace EliteDataRelay.Services
{
    public static class ItemNameService
    {
        private static readonly Dictionary<string, string> CommodityNames = new Dictionary<string, string>
        {
            // Add commodity internal names and their friendly names here
            // Example:
            { "lowtemperaturediamonds", "Low Temperature Diamonds" },
            { "painite", "Painite" },
            { "tritium", "Tritium" },
            { "gold", "Gold" },
            { "silver", "Silver" },
            { "platinum", "Platinum" },
            { "palladium", "Palladium" },
            { "monazite", "Monazite" },
            { "musgravite", "Musgravite" },
            { "grandidierite", "Grandidierite" },
            { "alexandrite", "Alexandrite" },
            { "voidopals", "Void Opals" },
            { "benitoite", "Benitoite" },
            { "serendibite", "Serendibite" },
            { "rhodplumsite", "Rhodplumsite" },
            { "osmium", "Osmium" },
            { "bromellite", "Bromellite" },
            { "hydrogenperoxide", "Hydrogen Peroxide" },
            { "liquidoxygen", "Liquid Oxygen" },
            { "water", "Water" },
            { "mineraloil", "Mineral Oil" },
            { "coltan", "Coltan" },
            { "uraninite", "Uraninite" },
            { "bauxite", "Bauxite" },
            { "lepidolite", "Lepidolite" },
            { "rutile", "Rutile" },
            { "gallite", "Gallite" },
            { "bertrandite", "Bertrandite" },
            { "indite", "Indite" },
            { "methanolmonohydratecrystals", "Methanol Monohydrate Crystals" },
            // This list is not exhaustive. You can add more as needed.
        };

        private static readonly Dictionary<string, string> ModuleNames = new Dictionary<string, string>
        {
            // Example:
            { "hpt_pulselaser_fixed_medium", "Medium Fixed Pulse Laser" },
            { "int_cargorack_size5_class1", "Cargo Rack (16T)" },
            // This would be populated with many more module names
        };

        public static string? TranslateCommodityName(string internalName)
        {
            if (string.IsNullOrEmpty(internalName))
                return null;

            if (CommodityNames.TryGetValue(internalName.ToLower(), out var friendlyName))
            {
                return friendlyName;
            }

            // Fallback for items not in our dictionary: capitalize the first letter.
            return Capitalize(internalName);
        }

        public static string? TranslateModuleName(string internalName)
        {
            if (string.IsNullOrEmpty(internalName))
                return null;

            if (ModuleNames.TryGetValue(internalName.ToLower(), out var friendlyName))
            {
                return friendlyName;
            }

            // A more complex fallback for module names could be implemented here.
            // For now, just return a capitalized version.
            return FormatInternalName(internalName);
        }

        public static IEnumerable<string> GetAllCommodityNames()
        {
            return CommodityNames.Values.Distinct();
        }

        private static string FormatInternalName(string internalName)
        {
            if (string.IsNullOrEmpty(internalName))
            {
                return "Unknown";
            }

            // Simple formatter: "int_powerplant_size2_class5" -> "Int Powerplant Size2 Class5"
            var parts = internalName.Split('_');
            var formattedParts = parts.Select(p =>
            {
                if (string.IsNullOrEmpty(p)) return "";
                return char.ToUpper(p[0]) + p.Substring(1);
            });

            return string.Join(" ", formattedParts);
        }

        private static string? Capitalize(string? s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
