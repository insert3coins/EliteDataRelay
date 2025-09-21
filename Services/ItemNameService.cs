using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace EliteDataRelay.Services
    {
    /// <summary>
    /// A static service to translate internal game item names (e.g., for modules) into human-readable strings.
    /// </summary>
    public static class ItemNameService
    {
        private static readonly Dictionary<string, string> ModuleNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> BlueprintNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static ItemNameService()
        {
            LoadNamesFromResource("EliteDataRelay.Modules.txt", ModuleNames);
            LoadNamesFromResource("EliteDataRelay.Blueprints.txt", BlueprintNames);
        }

        /// <summary>
        /// Translates an internal module name into a user-friendly display name.
        /// </summary>
        /// <param name="internalName">The internal name from the journal (e.g., "int_powerplant_size2_class1").</param>
        /// <returns>A human-readable name (e.g., "2A Power Plant") or a formatted fallback.</returns>
        public static string TranslateModuleName(string internalName)
        {
            if (string.IsNullOrEmpty(internalName))
            {
                return "Empty";
            }

            // The journal uses '_size0_' for utility mounts, but community data files often use '_tiny_'.
            // We'll create a variant to check for this common discrepancy before doing a lookup.
            string lookupName = internalName.Contains("_size0_") ? internalName.Replace("_size0_", "_tiny_") : internalName;

            // First, try a direct match. This is fastest and handles non-engineered items.
            if (ModuleNames.TryGetValue(lookupName, out string? friendlyName))
            {
                // If the lookup name was different, cache the result for the original name for faster access next time.
                if (lookupName != internalName)
                {
                    ModuleNames[internalName] = friendlyName;
                }
                return friendlyName;
            }

            // If direct match fails, it might be an engineered module (e.g., '..._sturdy').
            // We can try stripping suffixes until we find a base module name.
            // We use the lookupName here as well to handle engineered utility mounts correctly.
            string tempName = lookupName;
            while (true)
            {
                int lastUnderscore = tempName.LastIndexOf('_');
                if (lastUnderscore <= 0)
                {
                    break; // No more parts to strip or we've reached the start of the string.
                }

                tempName = tempName.Substring(0, lastUnderscore);
                if (ModuleNames.TryGetValue(tempName, out friendlyName))
                {
                    // Found a base name match. Cache the full engineered name for next time.
                    ModuleNames[internalName] = friendlyName;
                    return friendlyName;
                }
            }

            // Fallback for completely unknown modules: try to make the internal name more readable.
            string fallback = internalName.Replace("_", " ");
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fallback.ToLower());
        }

        /// <summary>
        /// Translates an internal blueprint name into a user-friendly display name.
        /// </summary>
        /// <param name="internalName">The internal name from the journal (e.g., "FSD_LongRange").</param>
        /// <returns>A human-readable name (e.g., "Increased FSD Range") or a formatted fallback.</returns>
        public static string TranslateBlueprintName(string internalName)
        {
            if (string.IsNullOrEmpty(internalName))
            {
                return "Unknown";
            }

            if (BlueprintNames.TryGetValue(internalName, out string? friendlyName))
            {
                return friendlyName;
            }

            // Fallback for unknown blueprints: try to make the internal name more readable.
            string fallback = internalName.Replace("Blueprint_", "").Replace("_", " ");
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fallback.ToLower());
        }

        private static void LoadNamesFromResource(string resourceName, Dictionary<string, string> dictionary)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ItemNameService] Could not find embedded resource: {resourceName}");
                    return;
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                        var parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length == 2) dictionary[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }
        }
    }
}