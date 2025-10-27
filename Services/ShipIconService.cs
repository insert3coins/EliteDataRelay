using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using System.Reflection;
namespace EliteDataRelay.Services
{
    /// <summary>
    /// Provides a service to retrieve ship icons based on the ship's internal name.
    /// Icons are loaded from the file system and cached in memory.
    /// </summary>
    public static class ShipIconService
    {
        private static readonly Dictionary<string, Image> _iconCache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private static readonly string _shipIconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "ships");
        private static Image? _defaultIcon;

        /// <summary>
        /// Static constructor to log the ship icon mappings on startup.
        /// </summary>
        static ShipIconService()
        {
            Trace.WriteLine("[ShipIconService] Initializing ship icon mappings...");
            foreach (var mapping in _shipToFileNameMap)
            {
                Trace.WriteLine($"[ShipIconService] Mapping: '{mapping.Key}' -> '{mapping.Value}.png'");
            }
            Trace.WriteLine($"[ShipIconService] Total mappings loaded: {_shipToFileNameMap.Count}");
        }

        // Maps the journal's internal ship name (usually lowercase) to a specific icon file name.
        // This provides a single source of truth and handles any inconsistencies in journal data.
        private static readonly Dictionary<string, string> _shipToFileNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // --- Small Ships ---
            { "sidewinder", "SideWinder" },
            { "eagle", "Eagle Mk II" },
            { "hauler", "Hauler" },
            { "adder", "Adder" },
            { "empire_eagle", "Imperial Eagle" },
            { "viper", "Viper Mk III" },
            { "viper_mkiv", "Viper Mk IV" },
            { "cobramkiii", "Cobra Mk III" },
            { "cobramkiv", "Cobra Mk IV" },
            { "cobramkv", "Cobra Mk V" },
            { "diamondback", "Diamondback Scout" }, // Journal: "diamondback" -> Scout
            { "dolphin", "Dolphin" },
            { "empire_courier", "Imperial Courier" },
            { "vulture", "Vulture" },
            { "corsair", "Corsair" },

            // --- Medium Ships ---
            { "diamondbackxl", "Diamondback Explorer" }, // Journal: "diamondbackxl" -> Explorer
            { "keelback", "Keelback" },
            { "type6", "Type 6 Transporter" },
            { "lakonminer", "Lakon_Miner" }, // Lakon Type-11 Prospector
            { "asp", "Asp Explorer" }, // Journal: "asp" -> Asp Explorer
            { "asp_scout", "Asp Scout" },
            { "federation_dropship", "Federal Dropship" },
            { "federation_dropship_mkii", "Federal Assault Ship" },
            { "federation_gunship", "Federal_Gunship" },
            { "empire_trader", "Imperial Clipper" },
            { "krait_mkii", "Krait Mk II" },
            { "krait_phantom", "Krait Phantom" },
            { "mamba", "Mamba" },
            { "ferdelance", "Fer De Lance" },
            { "python", "Python" },
            { "python_mkii", "Python Mk II" },
            { "orca", "Orca" },
            { "typex_3" , "Alliance Challenger" },
            { "typex_2" , "Alliance Crusader" },
            { "typex", "Alliance Chieftain" }, // Alliance Chieftain
            { "type8", "Type-8 Transporter"}, // Type 8
            { "mandalay", "Mandalay" }, 

            // --- Large Ships ---
            { "type7", "Type 7 Transporter" },
            { "type9", "Type 9 Heavy" },
            { "type9_military", "Type9_Military" },
            { "belugaliner", "Beluga Liner" },
            { "anaconda", "Anaconda" },
            { "federation_corvette", "Federation_Corvette" },
            { "cutter", "Cutter" },
            { "cyclops", "Cyclops" },
            { "panthermkii" , "Panther Clipper Mk II" }
        };

        /// <summary>
        /// Gets the icon for a given ship type.
        /// </summary>
        /// <param name="internalShipName">The internal name of the ship (e.g., "CobraMkIII").</param>
        /// <returns>An <see cref="Image"/> for the ship, or null if not found.</returns>
        public static Image? GetShipIcon(string? internalShipName)
        {
            if (string.IsNullOrEmpty(internalShipName))
            {
                return GetDefaultIcon();
            }

            if (_iconCache.TryGetValue(internalShipName, out var cachedIcon))
            {
                Trace.WriteLine($"[ShipIconService] Returning cached icon for '{internalShipName}'.");
                return cachedIcon;
            }

            // Use the mapping to find the correct file name for the given internal ship name.
            if (_shipToFileNameMap.TryGetValue(internalShipName, out var fileName))
            {
                string filePath = Path.Combine(_shipIconPath, $"{fileName}.png");
                Trace.WriteLine($"[ShipIconService] Mapped '{internalShipName}' to file '{filePath}'.");

                try
                {
                    if (File.Exists(filePath))
                    {
                        var icon = Image.FromFile(filePath);
                        _iconCache[internalShipName] = icon; // Cache under the original name for performance
                        Trace.WriteLine($"[ShipIconService] Successfully loaded and cached icon for '{internalShipName}'.");
                        return icon;
                    }
                    Trace.WriteLine($"[ShipIconService] Icon file not found: '{filePath}'.");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"[ShipIconService] Failed to load icon from file '{filePath}': {ex.Message}");
                }
            }
            else
            {
                Trace.WriteLine($"[ShipIconService] No mapping found for ship '{internalShipName}'.");
            }

            return GetDefaultIcon();
        }

        /// <summary>
        /// Gets the display name for a given ship's internal name.
        /// </summary>
        /// <param name="internalShipName">The internal name of the ship (e.g., "CobraMkIII").</param>
        /// <returns>A user-friendly display name (e.g., "Cobra Mk III"), or the capitalized internal name as a fallback.</returns>
        public static string GetShipDisplayName(string? internalShipName)
        {
            if (string.IsNullOrEmpty(internalShipName))
            {
                return "Unknown";
            }

            // Use the mapping to find the correct file name, which is also the display name.
            return _shipToFileNameMap.TryGetValue(internalShipName, out var displayName) ? displayName : (char.ToUpperInvariant(internalShipName[0]) + internalShipName.Substring(1));
        }

        /// <summary>
        /// Gets the default "unknown" ship icon, loading it from the file system if necessary.
        /// </summary>
        /// <returns>The default ship icon, or null if it also cannot be loaded.</returns>
        private static Image? GetDefaultIcon()
        {
            if (_defaultIcon != null)
            {
                Trace.WriteLine("[ShipIconService] Returning cached default icon.");
                return _defaultIcon;
            }

            string filePath = Path.Combine(_shipIconPath, "unknown.png");
            Trace.WriteLine($"[ShipIconService] Attempting to load default icon from file '{filePath}'.");

            try
            {
                if (File.Exists(filePath))
                {
                    _defaultIcon = Image.FromFile(filePath);
                    Trace.WriteLine($"[ShipIconService] Successfully loaded and cached default icon.");
                    return _defaultIcon;
                }
                Trace.WriteLine($"[ShipIconService] Default icon file not found: '{filePath}'.");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[ShipIconService] Failed to load default icon from file: {ex.Message}");
            }
            return null; // Ultimate fallback if even the default is missing.
        }
    }
}
