using System.Collections.Generic;
using System.Linq;

namespace EliteDataRelay.Services
{
    public class MaterialDefinition
    {
        public string Name { get; }
        public string LocalisedName { get; }
        public string Category { get; }
        public int Grade { get; }
        public int MaxCount { get; }

        public MaterialDefinition(string name, string localisedName, string category, int grade, int maxCount)
        {
            Name = name;
            LocalisedName = localisedName;
            Category = category;
            Grade = grade;
            MaxCount = maxCount;
        }
    }

    public static class MaterialDataService
    {
        private static readonly List<MaterialDefinition> AllMaterials = new()
        {
            // Raw Materials
            new("carbon", "Carbon", "Raw", 1, 300),
            new("vanadium", "Vanadium", "Raw", 2, 250),
            new("niobium", "Niobium", "Raw", 3, 200),
            new("yttrium", "Yttrium", "Raw", 4, 150),
            new("polonium", "Polonium", "Raw", 5, 100),
            new("iron", "Iron", "Raw", 1, 300),
            new("germanium", "Germanium", "Raw", 2, 250),
            new("cadmium", "Cadmium", "Raw", 3, 200),
            new("tellurium", "Tellurium", "Raw", 4, 150),
            new("ruthenium", "Ruthenium", "Raw", 5, 100),

            // Manufactured Materials
            new("chemicalstorageunits", "Chemical Storage Units", "Manufactured", 1, 300),
            new("compactcomposites", "Compact Composites", "Manufactured", 2, 250),
            new("configurablecomponents", "Configurable Components", "Manufactured", 3, 200),
            new("fedproprietarycomposites", "Fed. Proprietary Composites", "Manufactured", 4, 150),
            new("imperialshielding", "Imperial Shielding", "Manufactured", 5, 100),
            new("pharmaceuticalisolators", "Pharmaceutical Isolators", "Manufactured", 5, 100),
            new("militarysupercapacitors", "Military Supercapacitors", "Manufactured", 5, 100),

            // Encoded Materials
            new("legacyfirmware", "Legacy Firmware", "Encoded", 1, 300),
            new("consumerfirmware", "Consumer Firmware", "Encoded", 2, 250),
            new("industrialfirmware", "Industrial Firmware", "Encoded", 3, 200),
            new("securityfirmware", "Security Firmware", "Encoded", 4, 150),
            new("dataminedwakeexceptions", "Datamined Wake Exceptions", "Encoded", 5, 100),
            new("adaptiveencryptorscapture", "Adaptive Encryptors Capture", "Encoded", 5, 100)
        };

        private static readonly Dictionary<string, MaterialDefinition> _materialMap =
            new(AllMaterials.ToDictionary(m => m.Name, m => m), System.StringComparer.InvariantCultureIgnoreCase);

        public static int GetMaxCount(string materialName) =>
            _materialMap.TryGetValue(materialName, out var def) ? def.MaxCount : 0;

        public static IEnumerable<MaterialDefinition> GetAll() => AllMaterials.OrderBy(m => m.Category).ThenBy(m => m.Grade).ThenBy(m => m.LocalisedName);
    }
}