using EliteDataRelay.Models;
using System.Collections.Generic;
using System.Linq;

namespace EliteDataRelay.Services

{
    public static class MaterialDataService
    {
        private static readonly List<MaterialDefinition> RawMaterials = new List<MaterialDefinition>
        {
            // Grade 1
            new MaterialDefinition("carbon", 1), new MaterialDefinition("phosphorus", 1), new MaterialDefinition("sulphur", 1),
            new MaterialDefinition("iron", 1), new MaterialDefinition("nickel", 1), new MaterialDefinition("vanadium", 1),
            new MaterialDefinition("rhenium", 1),
            // Grade 2
            new MaterialDefinition("chromium", 2), new MaterialDefinition("manganese", 2), new MaterialDefinition("germanium", 2),
            // Grade 3
            new MaterialDefinition("zinc", 3), new MaterialDefinition("zirconium", 3), new MaterialDefinition("niobium", 3),
            // Grade 4
            new MaterialDefinition("cadmium", 4), new MaterialDefinition("mercury", 4), new MaterialDefinition("molybdenum", 4),
            new MaterialDefinition("tungsten", 4), new MaterialDefinition("tin", 4),
            // Grade 5
            new MaterialDefinition("arsenic", 5), new MaterialDefinition("polonium", 5), new MaterialDefinition("ruthenium", 5),
            new MaterialDefinition("selenium", 5), new MaterialDefinition("technetium", 5), new MaterialDefinition("tellurium", 5),
            new MaterialDefinition("antimony", 5), new MaterialDefinition("yttrium", 5),
        };

        private static readonly List<MaterialDefinition> ManufacturedMaterials = new List<MaterialDefinition>
        {
            // Grade 1
            new MaterialDefinition("basicconductors", 1), new MaterialDefinition("chemicalstorageunits", 1), new MaterialDefinition("compactcomposites", 1),
            new MaterialDefinition("crystalshards", 1), new MaterialDefinition("gridresistors", 1), new MaterialDefinition("heatconductionwiring", 1),
            new MaterialDefinition("mechanicalscrap", 1), new MaterialDefinition("salvagedalloys", 1), new MaterialDefinition("wornshieldemitters", 1),
            // Grade 2
            new MaterialDefinition("chemicalprocessors", 2), new MaterialDefinition("conductivecomponents", 2), new MaterialDefinition("heatdispersingplate", 2),
            new MaterialDefinition("mechanicalequipment", 2), new MaterialDefinition("shieldemitters", 2),
            // Grade 3
            new MaterialDefinition("chemicaldistillery", 3), new MaterialDefinition("conductiveceramics", 3), new MaterialDefinition("electrochemicalarrays", 3),
            new MaterialDefinition("focuscrystals", 3), new MaterialDefinition("heat-exchangers", 3), new MaterialDefinition("mechanicalcomponents", 3),
            new MaterialDefinition("phasealloys", 3), new MaterialDefinition("precipitatedalloys", 3), new MaterialDefinition("shieldingsensors", 3),
            // Grade 4
            new MaterialDefinition("chemicalmanipulators", 4), new MaterialDefinition("compoundshielding", 4), new MaterialDefinition("conductivepolymers", 4),
            new MaterialDefinition("configurablecomponents", 4), new MaterialDefinition("fedproprietarycomposites", 4), new MaterialDefinition("filamentcomposites", 4),
            new MaterialDefinition("heatvanes", 4), new MaterialDefinition("highdensitycomposites", 4), new MaterialDefinition("hybridcapacitors", 4),
            new MaterialDefinition("imperialshielding", 4), new MaterialDefinition("manipulators", 4), new MaterialDefinition("polymercapacitors", 4),
            new MaterialDefinition("refinedfocuscrystals", 4), new MaterialDefinition("thermicalloys", 4),
            // Grade 5
            new MaterialDefinition("antiquatedfocuscrystals", 5), new MaterialDefinition("biotechconductors", 5), new MaterialDefinition("coredynamicscomposites", 5),
            new MaterialDefinition("exquisitefocuscrystals", 5), new MaterialDefinition("fedcorecomposites", 5), new MaterialDefinition("improvisedcomponents", 5),
            new MaterialDefinition("militarygradealloys", 5), new MaterialDefinition("militarysupercapacitors", 5), new MaterialDefinition("pharmaceuticalisolators", 5),
            new MaterialDefinition("protoheatradiators", 5), new MaterialDefinition("protolightalloys", 5), new MaterialDefinition("protoradiolicalloys", 5),
            new MaterialDefinition("tg_assaultshipcores", 5), new MaterialDefinition("tg_assaultshipfragments", 5), new MaterialDefinition("tg_assaultshipplating", 5),
            new MaterialDefinition("tg_causticgeneratorparts", 5), new MaterialDefinition("tg_causticshard", 5), new MaterialDefinition("tg_energycell", 5),
            new MaterialDefinition("tg_interdictorcore", 5), new MaterialDefinition("tg_interdictorfragments", 5), new MaterialDefinition("tg_interdictorplating", 5),
            new MaterialDefinition("tg_mothercores", 5), new MaterialDefinition("tg_motherfragments", 5), new MaterialDefinition("tg_motherplating", 5),
            new MaterialDefinition("tg_shipflightdata", 5), new MaterialDefinition("tg_shipwreckagedata", 5), new MaterialDefinition("tg_shipsystemsdata", 5),
            new MaterialDefinition("tg_weaponparts", 5), new MaterialDefinition("unknowncarapace", 5), new MaterialDefinition("unknownenergycell", 5),
            new MaterialDefinition("unknownorganiccircuitry", 5), new MaterialDefinition("unknowntechnologycomponents", 5),
        };

        private static readonly List<MaterialDefinition> EncodedMaterials = new List<MaterialDefinition>
        {
            // Grade 1
            new MaterialDefinition("aberrantshieldpatternanalysis", 1), new MaterialDefinition("anomalousbulkscandata", 1), new MaterialDefinition("disruptedwakeechoes", 1),
            new MaterialDefinition("exceptionalscrambledemissiondata", 1), new MaterialDefinition("fsdwakedata_high", 1), new MaterialDefinition("scrambledemissiondata", 1),
            new MaterialDefinition("shieldcyclerecordings", 1), new MaterialDefinition("shielddensityreports", 1), new MaterialDefinition("specialisedlegacyfirmware", 1),
            new MaterialDefinition("tg_structuraldata", 1), new MaterialDefinition("unidentifiedscan", 1), new MaterialDefinition("unusualencryptedfiles", 1),
            // Grade 2
            new MaterialDefinition("anomalousfsdtelemetry", 2), new MaterialDefinition("bulkscandata", 2), new MaterialDefinition("classifiedscandata", 2),
            new MaterialDefinition("consumerfirmware", 2), new MaterialDefinition("fsdwakedata_low", 2), new MaterialDefinition("industrialfirmware", 2),
            new MaterialDefinition("inconsistentshieldsoakanalysis", 2), new MaterialDefinition("legacyfirmware", 2), new MaterialDefinition("shieldsoakanalysis", 2),
            new MaterialDefinition("tg_compositiondata", 2),
            // Grade 3
            new MaterialDefinition("archivedemissiondata", 3), new MaterialDefinition("compactemissionsdata", 3), new MaterialDefinition("decodedemissiondata", 3),
            new MaterialDefinition("divergentscandata", 3), new MaterialDefinition("fsdwakedata_medium", 3), new MaterialDefinition("fsdtelemetry", 3),
            new MaterialDefinition("securityfirmware", 3), new MaterialDefinition("shieldfrequencydata", 3), new MaterialDefinition("symmetrickeys", 3),
            new MaterialDefinition("tg_residuedata", 3),
            // Grade 4
            new MaterialDefinition("abnormalcompactemissionsdata", 4), new MaterialDefinition("adaptiveencryptorscapture", 4), new MaterialDefinition("classifiedscandatabackup", 4),
            new MaterialDefinition("dataminedwake", 4), new MaterialDefinition("decodedscandatabackup", 4), new MaterialDefinition("embeddedfirmware", 4),
            new MaterialDefinition("encryptioncodes", 4), new MaterialDefinition("fsdwakedata_veryhigh", 4), new MaterialDefinition("hyperspacewakedata", 4),
            new MaterialDefinition("scandatabackup", 4), new MaterialDefinition("tg_shipsystemsdata", 4),
            // Grade 5
            new MaterialDefinition("ancientbiologicaldata", 5), new MaterialDefinition("ancientculturaldata", 5), new MaterialDefinition("ancientguardianobeliskdata", 5),
            new MaterialDefinition("ancienthistoricaldata", 5), new MaterialDefinition("ancientlanguagedata", 5), new MaterialDefinition("ancienttechnologicaldata", 5),
            new MaterialDefinition("classifiedscandatafragment", 5), new MaterialDefinition("dataminedwakeexceptions", 5), new MaterialDefinition("guardian_moduleblueprint", 5),
            new MaterialDefinition("guardian_vesselblueprint", 5), new MaterialDefinition("guardian_weaponblueprint", 5), new MaterialDefinition("modifiedconsumerfirmware", 5),
            new MaterialDefinition("modifiedembeddedfirmware", 5), new MaterialDefinition("opentradedata", 5), new MaterialDefinition("tg_flightdata", 5),
            new MaterialDefinition("tg_systemdata", 5), new MaterialDefinition("unknownenergysourcedata", 5), new MaterialDefinition("unknownmaterialdata", 5),
        };

        public static List<MaterialDefinition> GetAllRawMaterials() => RawMaterials.OrderBy(m => m.Grade).ThenBy(m => m.Name).ToList();
        public static List<MaterialDefinition> GetAllManufacturedMaterials() => ManufacturedMaterials.OrderBy(m => m.Grade).ThenBy(m => m.Name).ToList();
        public static List<MaterialDefinition> GetAllEncodedMaterials() => EncodedMaterials.OrderBy(m => m.Grade).ThenBy(m => m.Name).ToList();

        private static readonly Dictionary<string, string> NameToLocalisedName = new Dictionary<string, string>
        {
            {"adaptiveencryptorscapture", "Adaptive Encryptors Capture"}, {"aberrantshieldpatternanalysis", "Aberrant Shield Pattern Analysis"},
            {"abnormalcompactemissionsdata", "Abnormal Compact Emissions Data"}, {"ancientbiologicaldata", "Ancient Biological Data"},
            {"ancientculturaldata", "Ancient Cultural Data"}, {"ancientguardianobeliskdata", "Ancient Guardian Obelisk Data"},
            {"ancienthistoricaldata", "Ancient Historical Data"}, {"ancientlanguagedata", "Ancient Language Data"},
            {"ancienttechnologicaldata", "Ancient Technological Data"}, {"anomalousbulkscandata", "Anomalous Bulk Scan Data"},
            {"anomalousfsdtelemetry", "Anomalous FSD Telemetry"}, {"antiquatedfocuscrystals", "Antiquated Focus Crystals"},
            {"antimony", "Antimony"}, {"archivedemissiondata", "Archived Emission Data"}, {"arsenic", "Arsenic"},
            {"basicconductors", "Basic Conductors"}, {"biotechconductors", "Biotech Conductors"}, {"bulkscandata", "Bulk Scan Data"},
            {"cadmium", "Cadmium"}, {"carbon", "Carbon"}, {"chemicaldistillery", "Chemical Distillery"},
            {"chemicalmanipulators", "Chemical Manipulators"}, {"chemicalprocessors", "Chemical Processors"},
            {"chemicalstorageunits", "Chemical Storage Units"}, {"chromium", "Chromium"}, {"classifiedscandata", "Classified Scan Data"},
            {"classifiedscandatabackup", "Classified Scan Data Backup"}, {"classifiedscandatafragment", "Classified Scan Data Fragment"},
            {"compactcomposites", "Compact Composites"}, {"compactemissionsdata", "Compact Emissions Data"},
            {"compoundshielding", "Compound Shielding"}, {"conductiveceramics", "Conductive Ceramics"},
            {"conductivecomponents", "Conductive Components"}, {"conductivepolymers", "Conductive Polymers"},
            {"configurablecomponents", "Configurable Components"}, {"consumerfirmware", "Consumer Firmware"},
            {"coredynamicscomposites", "Core Dynamics Composites"}, {"crystalshards", "Crystal Shards"},
            {"dataminedwake", "Datamined Wake Exceptions"}, {"dataminedwakeexceptions", "Datamined Wake Exceptions"},
            {"decodedemissiondata", "Decoded Emission Data"}, {"decodedscandatabackup", "Decoded Scan Data Backup"},
            {"disruptedwakeechoes", "Disrupted Wake Echoes"}, {"divergentscandata", "Divergent Scan Data"},
            {"electrochemicalarrays", "Electrochemical Arrays"}, {"embeddedfirmware", "Embedded Firmware"},
            {"encryptioncodes", "Encryption Codes"}, {"exceptionalscrambledemissiondata", "Exceptional Scrambled Emission Data"},
            {"exquisitefocuscrystals", "Exquisite Focus Crystals"}, {"fedcorecomposites", "Fed Core Composites"},
            {"fedproprietarycomposites", "Fed Proprietary Composites"}, {"filamentcomposites", "Filament Composites"},
            {"focuscrystals", "Focus Crystals"}, {"fsdtelemetry", "FSD Telemetry"}, {"fsdwakedata_high", "Atypical FSD Wake Data"},
            {"fsdwakedata_low", "Strange Wake Solutions"}, {"fsdwakedata_medium", "Anomalous FSD Wake Data"},
            {"fsdwakedata_veryhigh", "Exotic FSD Wake Data"}, {"germanium", "Germanium"}, {"gridresistors", "Grid Resistors"},
            {"guardian_moduleblueprint", "Guardian Module Blueprint"}, {"guardian_vesselblueprint", "Guardian Vessel Blueprint"},
            {"guardian_weaponblueprint", "Guardian Weapon Blueprint"}, {"heatconductionwiring", "Heat Conduction Wiring"},
            {"heatdispersingplate", "Heat Dispersing Plate"}, {"heat-exchangers", "Heat Exchangers"}, {"heatvanes", "Heat Vanes"},
            {"highdensitycomposites", "High Density Composites"}, {"hybridcapacitors", "Hybrid Capacitors"},
            {"hyperspacewakedata", "Hyperspace Trajectories"}, {"imperialshielding", "Imperial Shielding"},
            {"improvisedcomponents", "Improvised Components"}, {"inconsistentshieldsoakanalysis", "Inconsistent Shield Soak Analysis"},
            {"industrialfirmware", "Industrial Firmware"}, {"iron", "Iron"}, {"legacyfirmware", "Legacy Firmware"},
            {"manipulators", "Mechanical Manipulators"}, {"manganese", "Manganese"}, {"mechanicalcomponents", "Mechanical Components"},
            {"mechanicalequipment", "Mechanical Equipment"}, {"mechanicalscrap", "Mechanical Scrap"}, {"mercury", "Mercury"},
            {"militarygradealloys", "Military Grade Alloys"}, {"militarysupercapacitors", "Military Supercapacitors"},
            {"modifiedconsumerfirmware", "Modified Consumer Firmware"}, {"modifiedembeddedfirmware", "Modified Embedded Firmware"},
            {"molybdenum", "Molybdenum"}, {"nickel", "Nickel"}, {"niobium", "Niobium"}, {"opentradedata", "Open Symmetric Keys"},
            {"phasealloys", "Phase Alloys"}, {"pharmaceuticalisolators", "Pharmaceutical Isolators"}, {"phosphorus", "Phosphorus"},
            {"polonium", "Polonium"}, {"polymercapacitors", "Polymer Capacitors"}, {"precipitatedalloys", "Precipitated Alloys"},
            {"protoheatradiators", "Proto Heat Radiators"}, {"protolightalloys", "Proto Light Alloys"},
            {"protoradiolicalloys", "Proto Radiolic Alloys"}, {"refinedfocuscrystals", "Refined Focus Crystals"},
            {"rhenium", "Rhenium"}, {"ruthenium", "Ruthenium"}, {"salvagedalloys", "Salvaged Alloys"},
            {"scandatabackup", "Scan Data Backup"}, {"scrambledemissiondata", "Scrambled Emission Data"},
            {"securityfirmware", "Security Firmware"}, {"selenium", "Selenium"}, {"shieldcyclerecordings", "Shield Cycle Recordings"},
            {"shielddensityreports", "Shield Density Reports"}, {"shieldemitters", "Shield Emitters"},
            {"shieldfrequencydata", "Shield Frequency Data"}, {"shieldingsensors", "Shielding Sensors"},
            {"shieldsoakanalysis", "Shield Soak Analysis"}, {"specialisedlegacyfirmware", "Specialised Legacy Firmware"},
            {"sulphur", "Sulphur"}, {"symmetrickeys", "Symmetric Keys"}, {"technetium", "Technetium"},
            {"tellurium", "Tellurium"}, {"tg_assaultshipcores", "Thargoid Assault Ship Core"},
            {"tg_assaultshipfragments", "Thargoid Assault Ship Fragment"}, {"tg_assaultshipplating", "Thargoid Assault Ship Plating"},
            {"tg_causticgeneratorparts", "Thargoid Caustic Generator Part"}, {"tg_causticshard", "Thargoid Caustic Shard"},
            {"tg_compositiondata", "Thargoid Composition Data"}, {"tg_energycell", "Thargoid Energy Cell"},
            {"tg_flightdata", "Thargoid Flight Data"}, {"tg_interdictorcore", "Thargoid Interdictor Core"},
            {"tg_interdictorfragments", "Thargoid Interdictor Fragment"}, {"tg_interdictorplating", "Thargoid Interdictor Plating"},
            {"tg_mothercores", "Thargoid Mother Core"}, {"tg_motherfragments", "Thargoid Mother Fragment"},
            {"tg_motherplating", "Thargoid Mother Plating"}, {"tg_residuedata", "Thargoid Residue Data"},
            {"tg_shipflightdata", "Thargoid Ship Flight Data"}, {"tg_shipsystemsdata", "Thargoid Ship Systems Data"},
            {"tg_shipwreckagedata", "Thargoid Ship Wreckage Data"}, {"tg_structuraldata", "Thargoid Structural Data"},
            {"tg_systemdata", "Thargoid System Data"}, {"tg_weaponparts", "Thargoid Weapon Parts"},
            {"thermicalloys", "Thermic Alloys"}, {"tin", "Tin"}, {"tungsten", "Tungsten"},
            {"unidentifiedscan", "Unidentified Scan Data"}, {"unusualencryptedfiles", "Unusual Encrypted Files"},
            {"unknowncarapace", "Unknown Carapace"}, {"unknownenergycell", "Unknown Energy Cell"},
            {"unknownenergysourcedata", "Unknown Energy Source Data"}, {"unknownmaterialdata", "Unknown Material Data"},
            {"unknownorganiccircuitry", "Unknown Organic Circuitry"}, {"unknowntechnologycomponents", "Unknown Technology Components"},
            {"vanadium", "Vanadium"}, {"wornshieldemitters", "Worn Shield Emitters"}, {"yttrium", "Yttrium"},
            {"zinc", "Zinc"}, {"zirconium", "Zirconium"}
        };

        public static string GetLocalisedName(string name)
        {
            if (NameToLocalisedName.TryGetValue(name.ToLowerInvariant(), out var localisedName))
            {
                return localisedName;
            }
            // Fallback to a capitalized version of the internal name
            return name.Length > 1 ? char.ToUpperInvariant(name[0]) + name.Substring(1) : name;
        }
    }
}