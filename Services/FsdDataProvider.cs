using EliteDataRelay.Models;
using System.Collections.Generic;

namespace EliteDataRelay.Services

{
    public static class FsdDataProvider
    {
        private static readonly Dictionary<string, FsdStats> FsdStatsMap = new Dictionary<string, FsdStats>(System.StringComparer.OrdinalIgnoreCase)
        {
            // Data from various community sources (e.g., https://elite-dangerous.fandom.com/wiki/Frame_Shift_Drive)
            // Format: { "internal_name", new FsdStats(OptimalMass, MaxFuelPerJump, PowerConstant, FuelMultiplier) }
            
            // Class 2
            { "int_hyperdrive_size2_class5", new FsdStats(90, 0.9, 2.00, 0.012) }, // 2A
            { "int_hyperdrive_size2_class4", new FsdStats(75, 0.8, 2.05, 0.012) }, // 2B
            { "int_hyperdrive_size2_class3", new FsdStats(60, 0.8, 2.10, 0.012) }, // 2C
            { "int_hyperdrive_size2_class2", new FsdStats(60, 0.7, 2.15, 0.012) }, // 2D
            { "int_hyperdrive_size2_class1", new FsdStats(50, 0.6, 2.20, 0.012) }, // 2E

            // Class 3
            { "int_hyperdrive_size3_class5", new FsdStats(240, 1.8, 2.15, 0.015) }, // 3A
            { "int_hyperdrive_size3_class4", new FsdStats(200, 1.6, 2.20, 0.015) }, // 3B
            { "int_hyperdrive_size3_class3", new FsdStats(160, 1.6, 2.25, 0.015) }, // 3C
            { "int_hyperdrive_size3_class2", new FsdStats(160, 1.4, 2.30, 0.015) }, // 3D
            { "int_hyperdrive_size3_class1", new FsdStats(130, 1.2, 2.35, 0.015) }, // 3E

            // Class 4
            { "int_hyperdrive_size4_class5", new FsdStats(540, 3.0, 2.30, 0.020) }, // 4A
            { "int_hyperdrive_size4_class4", new FsdStats(450, 2.7, 2.35, 0.020) }, // 4B
            { "int_hyperdrive_size4_class3", new FsdStats(360, 2.7, 2.40, 0.020) }, // 4C
            { "int_hyperdrive_size4_class2", new FsdStats(360, 2.4, 2.45, 0.020) }, // 4D
            { "int_hyperdrive_size4_class1", new FsdStats(300, 2.1, 2.50, 0.020) }, // 4E

            // Class 5
            { "int_hyperdrive_size5_class5", new FsdStats(1050, 5.0, 2.45, 0.035) }, // 5A
            { "int_hyperdrive_size5_class4", new FsdStats(875, 4.5, 2.50, 0.035) }, // 5B
            { "int_hyperdrive_size5_class3", new FsdStats(700, 4.5, 2.55, 0.035) }, // 5C
            { "int_hyperdrive_size5_class2", new FsdStats(700, 4.0, 2.60, 0.035) }, // 5D
            { "int_hyperdrive_size5_class1", new FsdStats(580, 3.5, 2.65, 0.035) }, // 5E

            // Class 6
            { "int_hyperdrive_size6_class5", new FsdStats(1800, 8.0, 2.60, 0.050) }, // 6A
            { "int_hyperdrive_size6_class4", new FsdStats(1500, 7.2, 2.65, 0.050) }, // 6B
            { "int_hyperdrive_size6_class3", new FsdStats(1200, 7.2, 2.70, 0.050) }, // 6C
            { "int_hyperdrive_size6_class2", new FsdStats(1200, 6.4, 2.75, 0.050) }, // 6D
            { "int_hyperdrive_size6_class1", new FsdStats(1000, 5.6, 2.80, 0.050) }, // 6E

            // Class 7
            { "int_hyperdrive_size7_class5", new FsdStats(2700, 12.8, 2.75, 0.080) }, // 7A
            { "int_hyperdrive_size7_class4", new FsdStats(2250, 11.5, 2.80, 0.080) }, // 7B
            { "int_hyperdrive_size7_class3", new FsdStats(1800, 11.5, 2.85, 0.080) }, // 7C
            { "int_hyperdrive_size7_class2", new FsdStats(1800, 10.2, 2.90, 0.080) }, // 7D
            { "int_hyperdrive_size7_class1", new FsdStats(1500, 9.0, 2.95, 0.080) }, // 7E
        };

        public static FsdStats? GetFsdStats(string internalName)
        {
            FsdStatsMap.TryGetValue(internalName, out var stats);
            return stats;
        }
    }
}
