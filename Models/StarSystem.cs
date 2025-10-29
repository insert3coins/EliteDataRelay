using System.Collections.Generic;
namespace EliteDataRelay.Models
{
    public class StarSystem
    {
        public string Name { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public List<StarSystemBody> Bodies { get; set; } = new List<StarSystemBody>();
    }

    public class StarSystemBody
    {
        public string BodyName { get; set; } = string.Empty;

        public string? StarType { get; set; } // e.g., "G" (Main Sequence)

        public string? PlanetClass { get; set; } // e.g., "High metal content body"

        public bool WasDiscovered { get; set; }

        public bool WasMapped { get; set; }

        public string? TerraformState { get; set; } // "Terraformable", "", etc.

        public bool Landable { get; set; }
    }
}
