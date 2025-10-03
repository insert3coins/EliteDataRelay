using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Represents the geometric definition of a ship's wireframe.
    /// </summary>
    public class WireframeGeometry
    {
        public List<PointF[]> Polygons { get; } = new List<PointF[]>();
        public List<(PointF, PointF)> Lines { get; } = new List<(PointF, PointF)>();
    }

    /// <summary>
    /// Provides a static data store for ship wireframe geometries.
    /// </summary>
    public static partial class ShipWireframeData
    {
        private static readonly Dictionary<string, WireframeGeometry> Geometries;

        static ShipWireframeData()
        {
            Geometries = new Dictionary<string, WireframeGeometry>
            {
                ["cobramkiii"] = CreateCobraMkIII(),
                ["sidewinder"] = CreateSidewinder(),
                ["viper"] = CreateViper(),
                ["anaconda"] = CreateAnaconda(),
                ["eagle"] = CreateEagle(),
                ["hauler"] = CreateHauler(),
                ["adder"] = CreateAdder(),
                ["imperial_eagle"] = CreateImperialEagle(),
                ["viper_mkiv"] = CreateViperMkIV(),
                ["cobramkiv"] = CreateCobraMkIV(),
                ["diamondbackscout"] = CreateDiamondbackScout(),
                ["dolphin"] = CreateDolphin(),
                ["imperial_courier"] = CreateImperialCourier(),
                ["vulture"] = CreateVulture(),
                ["diamondbackxl"] = CreateDiamondbackXL(),
                ["keelback"] = CreateKeelback(),
                ["type6"] = CreateType6(),
                ["asp"] = CreateAspExplorer(),
                ["asp_scout"] = CreateAspScout(),
                ["federal_dropship"] = CreateFederalDropship(),
                ["federal_assault_ship"] = CreateFederalAssaultShip(),
                ["federal_gunship"] = CreateFederalGunship(),
                ["imperial_clipper"] = CreateImperialClipper(),
                ["krait_mkii"] = CreateKraitMkII(),
                ["krait_phantom"] = CreateKraitPhantom(),
                ["mamba"] = CreateMamba(),
                ["ferdelance"] = CreateFerDeLance(),
                ["python"] = CreatePython(),
                ["orca"] = CreateOrca(),
                ["chieftain"] = CreateChieftain(),
                ["crusader"] = CreateCrusader(),
                ["challenger"] = CreateChallenger(),
                ["type7"] = CreateType7(),
                ["type9"] = CreateType9(),
                ["type10"] = CreateType10(),
                ["lakonminer"] = CreateLakonMiner(),
                ["belugaliner"] = CreateBelugaLiner(),
                ["federal_corvette"] = CreateFederalCorvette(),
                ["imperial_cutter"] = CreateImperialCutter(),
            };
        }

        /// <summary>
        /// Gets the wireframe geometry for a given ship type.
        /// </summary>
        /// <param name="shipType">The internal name of the ship (e.g., "cobramkiii").</param>
        /// <returns>The ship's geometry, or the Cobra MkIII's geometry as a fallback.</returns>
        public static WireframeGeometry GetGeometry(string shipType)
        {
            return Geometries.TryGetValue(shipType.ToLowerInvariant(), out var geometry) ? geometry : Geometries["cobramkiii"];
        }
    }
}