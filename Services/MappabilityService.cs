using System;
using System.Collections.Generic;
using EliteDataRelay.Models;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Determines whether a scanned body is DSS-mappable based on its planet class.
    /// Mirrors EDDiscovery's intent by whitelisting known mappable planet classes
    /// and excluding stars, belts, barycentres and non-bodies.
    /// </summary>
    public static class MappabilityService
    {
        private static readonly HashSet<string> PlanetClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Terran/Water/Ammonia
            "Earth-like world",
            "Water world",
            "Ammonia world",

            // Rocky/Metal/Icy
            "Metal-rich body",
            "High metal content world",
            "Rocky body",
            "Icy body",
            "Rocky ice world",

            // Gas giants
            "Class I gas giant",
            "Class II gas giant",
            "Class III gas giant",
            "Class IV gas giant",
            "Class V gas giant",
            "Helium-rich gas giant",
            "Gas giant with water-based life",
            "Gas giant with ammonia-based life",

            // Exotic water types
            "Water giant",
            "Supercritical water world",
        };

        /// <summary>
        /// Returns true if the body's type corresponds to a DSS-mappable planet class.
        /// </summary>
        public static bool IsMappable(ScannedBody body)
        {
            if (body == null) return false;
            var type = body.BodyType?.Trim();
            if (string.IsNullOrEmpty(type)) return false;

            // Fast whitelist: planet classes only
            return PlanetClasses.Contains(type);
        }
    }
}

