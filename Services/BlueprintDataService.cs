using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Provides translation for blueprint and experimental effect names from an embedded resource file.
    /// </summary>
    public static class BlueprintDataService
    {
        private static readonly ConcurrentDictionary<string, string> BlueprintNames = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static BlueprintDataService()
        {
            LoadBlueprintData();
        }

        private static void LoadBlueprintData()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "EliteDataRelay.Resources.blueprints.txt";

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[BlueprintDataService] Error: Embedded resource '{resourceName}' not found.");
                    return;
                }
                using (StreamReader reader = new StreamReader(stream))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                        {
                            var parts = line.Split(new[] { '=' }, 2);
                            if (parts.Length == 2)
                            {
                                BlueprintNames[parts[0].Trim()] = parts[1].Trim();
                            }
                        }
                    }
                }
            }
        }

        public static string GetBlueprintName(string internalName) =>
            BlueprintNames.TryGetValue(internalName, out var friendlyName) ? friendlyName : internalName;
    }
}