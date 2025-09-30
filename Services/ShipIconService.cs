using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Provides a service to retrieve ship icons based on the ship's internal name.
    /// Icons are loaded from the file system and cached in memory.
    /// </summary>
    public static class ShipIconService
    {
        private static readonly Dictionary<string, Image> _iconCache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private static Image? _defaultIcon;
        // Maps the journal's internal ship name (usually lowercase) to a specific icon file name.
        // This provides a single source of truth and handles any inconsistencies in journal data.
        private static readonly Dictionary<string, string> _shipToFileNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // --- Small Ships ---
            { "sidewinder", "SideWinder" },
            { "eagle", "Eagle" },
            { "hauler", "Hauler" },
            { "adder", "Adder" },
            { "empire_eagle", "Imperial Eagle" },
            { "viper", "Viper" },
            { "viper_mkiv", "Viper Mk IV" },
            { "cobramkiii", "Cobra Mk III" },
            { "cobramkiv", "Cobra Mk IV" },
            { "diamondback", "Diamondback Scout" }, // Journal: "diamondback" -> Scout
            { "dolphin", "Dolphin" },
            { "empire_courier", "Imperial Courier" },
            { "vulture", "Vulture" },

            // --- Medium Ships ---
            { "diamondbackxl", "Diamondback XL" }, // Journal: "diamondbackxl" -> Explorer
            { "keelback", "Keelback" },
            { "type6", "Type-6 Transporter" },
            { "lakonminer", "Type-11 Prospector" }, // Lakon Type-11 Prospector
            { "asp", "Asp Explorer" }, // Journal: "asp" -> Asp Explorer
            { "asp_scout", "Asp Scout" },
            { "federation_dropship", "Federal Dropship" },
            { "federation_dropship_mkii", "Federal Assault Ship" },
            { "federation_gunship", "Federal Gunship" },
            { "empire_trader", "Imperial Clipper" },
            { "krait_mkii", "Krait Mk II" },
            { "krait_phantom", "Krait Phantom" },
            { "mamba", "Mamba" },
            { "ferdelance", "FerDeLance" },
            { "python", "Python" },
            { "orca", "Orca" },
            { "typex", "Chieftain" }, // Alliance Chieftain
            { "typex_2", "Crusader" }, // Alliance Crusader
            { "typex_3", "Challenger" }, // Alliance Challenger

            // --- Large Ships ---
            { "type7", "Type-7 Transporter" },
            { "type9", "Type-9 Heavy" },
            { "type9_military", "Type-10 Defender" },
            { "belugaliner", "Beluga Liner" },
            { "anaconda", "Anaconda" },
            { "federation_corvette", "Federal Corvette" },
            { "cutter", "Imperial Cutter" },
            { "panthermkii", "panthermkii" },
        };
        private static readonly string _iconDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Ships");

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
                Debug.WriteLine($"[ShipIconService] Returning cached icon for '{internalShipName}'.");
                return cachedIcon;
            }

            // Use the mapping to find the correct file name for the given internal ship name.
            if (_shipToFileNameMap.TryGetValue(internalShipName, out var fileName))
            {
                string fullPath = Path.Combine(_iconDirectory, $"{fileName}.png");
                Debug.WriteLine($"[ShipIconService] Mapped '{internalShipName}' to file '{fileName}.png'. Attempting to load from: {fullPath}");
                if (File.Exists(fullPath))
                {
                    try
                    {
                        var icon = Image.FromFile(fullPath);
                        _iconCache[internalShipName] = icon; // Cache under the original name for performance
                        Debug.WriteLine($"[ShipIconService] Successfully loaded and cached icon for '{internalShipName}'.");
                        return icon;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ShipIconService] Failed to load icon '{fullPath}': {ex.Message}");
                        _iconCache[internalShipName] = null!; // Cache null to prevent repeated failed attempts
                    }
                }
                else
                {
                    Debug.WriteLine($"[ShipIconService] Icon file not found: '{fullPath}'.");
                }
            }
            else
            {
                Debug.WriteLine($"[ShipIconService] No mapping found for ship '{internalShipName}'.");
            }
            return GetDefaultIcon();
        }

        /// <summary>
        /// Gets the default "unknown" ship icon, loading it from the file system if necessary.
        /// </summary>
        /// <returns>The default ship icon, or null if it also cannot be loaded.</returns>
        private static Image? GetDefaultIcon()
        {
            if (_defaultIcon != null)
            {
                Debug.WriteLine("[ShipIconService] Returning cached default icon.");
                return _defaultIcon;
            }

            string defaultIconPath = Path.Combine(_iconDirectory, "unknown.png");
            if (File.Exists(defaultIconPath))
            {
                try
                {
                    _defaultIcon = Image.FromFile(defaultIconPath);
                    Debug.WriteLine($"[ShipIconService] Successfully loaded and cached default icon from '{defaultIconPath}'.");
                    return _defaultIcon;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ShipIconService] Failed to load default icon '{defaultIconPath}': {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine($"[ShipIconService] Default icon file 'unknown.png' not found in '{_iconDirectory}'.");
            }
            return null; // Ultimate fallback if even the default is missing.
        }
    }
}