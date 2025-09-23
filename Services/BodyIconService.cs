using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace EliteDataRelay.Services
{
    public static class BodyIconService
    {
        private static readonly Dictionary<string, Image> _iconCache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private static readonly string _iconDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Bodies");

        // Maps journal body/station types to icon file names (without extension)
        private static readonly Dictionary<string, string> _typeToFileNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Star Types
            { "O-Type Star", "star_o" },
            { "B-Type Star", "star_b" },
            { "A-Type Star", "star_a" },
            { "F-Type Star", "star_f" },
            { "G-Type Star", "star_g" },
            { "K-Type Star", "star_k" },
            { "M-Type Star", "star_m" },
            { "L-Type Star", "star_l" },
            { "T-Type Star", "star_t" },
            { "Y-Type Star", "star_y" },
            { "Herbig Ae/Be Star", "star_herbig" },
            { "T Tauri Star", "star_ttauri" },
            { "White Dwarf", "star_d" },
            { "Neutron Star", "star_n" },
            { "Black Hole", "blackhole" },
            { "Supermassive Black Hole", "blackhole" },

            // Planet Types
            { "Metal rich body", "metalrich" },
            { "High metal content body", "highmetalcontent" },
            { "Rocky body", "rocky" },
            { "Icy body", "icy" },
            { "Rocky ice body", "rockyice" },
            { "Earthlike body", "earthlike" },
            { "Water world", "waterworld" },
            { "Ammonia world", "ammonia" },
            { "Water giant", "watergiant" },
            { "Water giant with life", "watergiant" },
            { "Gas giant with water-based life", "gasgiantwater" },
            { "Gas giant with ammonia-based life", "gasgiantammonia" },
            { "Sudarsky class I gas giant", "gasgiant1" },
            { "Sudarsky class II gas giant", "gasgiant2" },
            { "Sudarsky class III gas giant", "gasgiant3" },
            { "Sudarsky class IV gas giant", "gasgiant4" },
            { "Sudarsky class V gas giant", "gasgiant5" },
            { "Helium-rich gas giant", "gasgianthelium" },
            { "Helium gas giant", "gasgianthelium" },

            // Station Types from FSSSignalDiscovered
            { "Station", "station" },
            { "FleetCarrier", "carrier" },
            { "Installation", "installation" },

            // Station Types from DockableBody/FSDJump
            { "Orbis", "coriolis" }, // Orbis and Coriolis often use the same icon
            { "Coriolis", "coriolis" },
            { "Outpost", "outpost" },
            { "AsteroidBase", "asteroidbase" },
            { "MegaShip", "megaship" },
        };

        public static Image? GetBodyIcon(string bodyType)
        {
            if (string.IsNullOrEmpty(bodyType))
            {
                return null;
            }

            if (_iconCache.TryGetValue(bodyType, out var cachedIcon))
            {
                return cachedIcon;
            }

            // Handle special cases like "StationCoriolis"
            string lookupType = bodyType;
            if (bodyType.StartsWith("Station", StringComparison.OrdinalIgnoreCase))
            {
                lookupType = bodyType.Substring("Station".Length);
            }

            if (_typeToFileNameMap.TryGetValue(lookupType, out var fileName))
            {
                string fullPath = Path.Combine(_iconDirectory, $"{fileName}.png");
                if (File.Exists(fullPath))
                {
                    try
                    {
                        var icon = Image.FromFile(fullPath);
                        _iconCache[bodyType] = icon; // Cache under the original type name
                        return icon;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[BodyIconService] Failed to load icon '{fullPath}': {ex.Message}");
                        // Cache a null to prevent repeated file access attempts for a bad file
                        _iconCache[bodyType] = null!;
                        return null;
                    }
                }
            }

            return null;
        }
    }
}