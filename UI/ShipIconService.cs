using System.Collections.Generic;
using System.Drawing;
using System;
using System.IO;
using System.Reflection;

namespace EliteDataRelay.UI
{
    public static class ShipIconService
    {
        /// <summary>
        /// A mapping from the journal's internal ship name (lowercase) to the expected resource name.
        /// This handles cases where the internal name is different from the common name or file name convention.
        /// For example, the journal might use 'cobramkiii' but the image file is named 'cobra_mkiii.png'.
        /// </summary>
        private static readonly Dictionary<string, string> ShipNameMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Journal Name -> File Name (without .png)
            { "adder", "Adder" },
            { "alliance_challenger", "Alliance Challenger" },
            { "alliance_chieftain", "Alliance Chieftain" },
            { "alliance_crusader", "Alliance Crusader" },
            { "anaconda", "Anaconda" },
            { "asp", "Asp Explorer" },
            { "asp_scout", "Asp Scout" },
            { "beluga", "Beluga Liner" },
            { "cobramkiii", "Cobra Mk III" },
            { "cobramkiv", "Cobra Mk IV" },
            { "cobramkv", "Cobra Mk V" },
            { "corsair", "Corsair" },
            { "cyclops", "Cyclops" },
            { "diamondback", "Diamondback Scout" },
            { "diamondbackxl", "DiamondBack Explorer" },
            { "dolphin", "Dolphin" },
            { "dropship_mkii", "Federal Assault Ship" },
            { "eagle", "Eagle Mk II" },
            { "federal_corvette", "Federal Corvette" },
            { "federal_dropship", "Federal Dropship" },
            { "federal_gunship", "Federal Gunship" },
            { "ferdelance", "Fer De Lance" },
            { "hauler", "Hauler" },
            { "imperial_courier", "Imperial Courier" },
            { "imperial_clipper", "Imperial Clipper" },
            { "imperial_cutter", "Imperial Cutter" },
            { "imperial_eagle", "Imperial Eagle" },
            { "keelback", "Keelback" },
            { "krait_mkii", "Krait Mk II" },
            { "krait_phantom", "Krait Phantom" },
            { "mamba", "Mamba" },
            { "mandalay", "Mandalay" },
            { "orca", "Orca" },
            { "panthermkii", "Panther Clipper Mk II" },
            { "python", "Python" },
            { "python_mkii", "Python Mk II" },
            { "sidewinder", "Sidewinder" },
            { "type6", "Type 6 Transporter" },
            { "type7", "Type 7 Transporter" },
            { "type_8_transporter", "Type-8 Transporter" },
            { "type9_heavy", "Type 9 Heavy" },
            { "type9_military", "Type 9 Heavy" },
            { "type_11_prospector", "Type-11 Prospector" },
            { "typex", "Type 10 Defender" },
            { "typex_2", "Alliance Crusader" },
            { "typex_3", "Alliance Challenger" },
            { "viper", "Viper Mk III" },
            { "viper_mkiv", "Viper Mk IV" },
            { "vulture", "Vulture" },
        };

        private static readonly string _shipIconPath;
        private static readonly Dictionary<string, Image> _iconCache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        static ShipIconService()
        {
            // Determine the path to the ship icons directory relative to the executable.
            var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _shipIconPath = Path.Combine(exePath ?? "", "Images", "Ships");
        }

        public static Image? GetShipIcon(string shipName)
        {
            if (string.IsNullOrEmpty(shipName) || shipName.Equals("unknown", StringComparison.OrdinalIgnoreCase)) return null;

            string internalName = shipName.ToLowerInvariant();
            string fileSystemName = internalName;

            // Check our explicit mapping first for known inconsistencies.
            if (ShipNameMappings.TryGetValue(internalName, out var mappedName))
            {
                fileSystemName = mappedName;
            }
            System.Diagnostics.Debug.WriteLine($"[ShipIconService] Looking for ship icon. Internal Name: '{internalName}', Mapped Name: '{fileSystemName}'");

            // The key for caching should be consistent. Let's use the fileSystemName as it's more unique
            // and represents the final intended image.
            // Check the cache first to avoid repeated file access.
            if (_iconCache.TryGetValue(fileSystemName, out var cachedIcon))
            {
                return cachedIcon;
            }

            // Attempt to load the icon from the file system first.
            string filePath = Path.Combine(_shipIconPath, fileSystemName + ".png");
            System.Diagnostics.Debug.WriteLine($"[ShipIconService] Attempting to load from file system: '{filePath}'");

            if (File.Exists(filePath))
            {
                try
                {
                    // Load the image into a memory stream to prevent locking the file on disk.
                    byte[] fileData = File.ReadAllBytes(filePath);
                    using (var ms = new MemoryStream(fileData))
                    {
                        var image = Image.FromStream(ms);
                        System.Diagnostics.Debug.WriteLine($"[ShipIconService] Success. Loaded from file system.");
                        _iconCache[fileSystemName] = image; // Cache the loaded image.
                        return image;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ShipIconService] Error loading ship icon from '{filePath}': {ex.Message}");
                    // Fall through to try embedded resources.
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ShipIconService] File not found. Falling back to embedded resources.");
            }

            // Fallback to embedded resources if the file is not found on disk.
            // Resource names are typically valid C# identifiers (e.g., Cobra_Mk_III, not "Cobra Mk III").
            // We need to convert the file system name to a resource-friendly name.
            string resourceName = fileSystemName.Replace(" ", "_").Replace("-", "_");
            System.Diagnostics.Debug.WriteLine($"[ShipIconService] Attempting to load from embedded resource: '{resourceName}'");

            var resourceImage = (Image?)Properties.Resources.ResourceManager.GetObject(resourceName);
            if (resourceImage != null)
            {
                System.Diagnostics.Debug.WriteLine($"[ShipIconService] Success. Loaded from embedded resource.");
                _iconCache[fileSystemName] = resourceImage; // Cache the resource image too.
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ShipIconService] Failed to load icon from all sources.");
            }
            return resourceImage;
        }
    }
}