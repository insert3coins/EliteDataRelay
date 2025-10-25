﻿using EliteDataRelay.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EliteDataRelay.Services

{
    public static class MaterialDataService
    {
        private static readonly List<MaterialDefinition> AllMaterials = new List<MaterialDefinition>();
        private static readonly Dictionary<string, List<MaterialDefinition>> MaterialsByCategory = new Dictionary<string, List<MaterialDefinition>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, MaterialDefinition> MaterialsByName = new Dictionary<string, MaterialDefinition>(StringComparer.OrdinalIgnoreCase);

        static MaterialDataService()
        {
            LoadMaterialData();
        }

        private static void LoadMaterialData()
        {
            var assembly = Assembly.GetExecutingAssembly();
            LoadCsv(assembly, "EliteDataRelay.Resources.material.csv");
            LoadCsv(assembly, "EliteDataRelay.Resources.microresource.csv");

            // Group materials by category
            foreach (var group in AllMaterials.GroupBy(m => m.Category))
            {
                MaterialsByCategory[group.Key] = group.OrderBy(m => m.FriendlyName).ToList();
            }
        }

        private static void LoadCsv(Assembly assembly, string resourceName)
        {
            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[MaterialDataService] Error: Embedded resource '{resourceName}' not found.");
                    return;
                }
                using (StreamReader reader = new StreamReader(stream))
                {
                    reader.ReadLine(); // Skip header
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split(',');
                        if (parts.Length >= 6)
                        {
                            // id,symbol,rarity,type,category,name
                            var definition = new MaterialDefinition(parts[1], parts[5], parts[3], int.TryParse(parts[2], out int g) ? g : 0);
                            AllMaterials.Add(definition);
                            MaterialsByName[definition.Name] = definition;
                        }
                    }
                }
            }
        }

        public static List<MaterialDefinition> GetAllRawMaterials() =>
            AllMaterials.Where(m => m.Category == "Raw").OrderBy(m => m.FriendlyName).ToList();

        public static List<MaterialDefinition> GetAllManufacturedMaterials() =>
            AllMaterials.Where(m => m.Category == "Manufactured").OrderBy(m => m.FriendlyName).ToList();

        public static List<MaterialDefinition> GetAllEncodedMaterials() =>
            AllMaterials.Where(m => m.Category == "Encoded").OrderBy(m => m.FriendlyName).ToList();

        public static List<MaterialDefinition> GetAllMaterials() =>
            AllMaterials.OrderBy(m => m.FriendlyName).ToList();

        public static string GetLocalisedName(string name) =>
            AllMaterials.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.FriendlyName ??
            (name.Length > 1 ? char.ToUpperInvariant(name[0]) + name.Substring(1) : name);

        public static bool TryGetMaterialDefinition(string name, out MaterialDefinition definition)
            => MaterialsByName.TryGetValue(name, out definition!);
    }
}