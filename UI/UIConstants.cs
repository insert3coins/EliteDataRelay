using System.Drawing;
using System.Collections.Generic;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Contains static UI constants like colors and design elements.
    /// </summary>
    public static class UIConstants
    {
        // Sizing constants
        public static int ButtonPanelHeight { get; } = 35;

        // Colors for button states to provide better visual feedback.
        public static readonly Color DefaultButtonBackColor = Color.FromArgb(240, 240, 240);
        public static readonly Color StartButtonActiveColor = Color.FromArgb(232, 245, 233); // A subtle light green
        public static readonly Color StopButtonActiveColor = Color.FromArgb(252, 232, 232); // A subtle light red

        // A map of ship internal names to their ASCII art representation.
        public static readonly Dictionary<string, string> ShipArtMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Small Ships
            { "SideWinder", ">--o--<" },
            { "Eagle", ">--=--<" },
            { "Hauler", "[<=#=>]" },
            { "Adder", "[<=#=>]" },
            { "Imperial_Eagle", ">--^--<" },
            { "Viper", ">-(*)-<" },
            { "Viper_MkIV", ">-(*)-<" },
            { "CobraMkIII", "<(-O-)>" },
            { "CobraMkIV", "<(-O-)>" },
            { "DiamondbackScout", "<(-O-)>" },
            { "Dolphin", " ~<o>~ " },

            // Medium Ships
            { "DiamondbackXL", "<(-O-)>" },
            { "Type6", "(#####)" },
            { "Asp", "<(-O-)>" },
            { "Asp_Scout", "<(-O-)>" },
            { "Vulture", ">-(*)-<" },
            { "Federal_Dropship", "/_O_\\" },
            { "Federal_Assault_Ship", "/_O_\\" },
            { "Federal_Gunship", "/_O_\\" },
            { "Imperial_Courier", " ~<o>~ " },
            { "Imperial_Clipper", " ~<o>~ " },
            { "Krait_MkII", "<==*==>" },
            { "Krait_Phantom", "<==*==>" },
            { "Mamba", ">--^--<" },
            { "Python", "(#####)" },
            { "Orca", " ~<o>~ " },
            { "Chieftain", "/_O_\\" },
            { "Crusader", "/_O_\\" },
            { "Challenger", "/_O_\\" },

            // Large Ships
            { "Type7", "(#####)" },
            { "Type9", "(#####)" },
            { "Type10", "(#####)" },
            { "BelugaLiner", " ~<o>~ " },
            { "Anaconda", "<==*==>" },
            { "Federal_Corvette", "<==*==>" },
            { "Imperial_Cutter", "<==*==>" },
        };
        // Cargo storage sizes for bottom right of our ui
        public static readonly string[] CargoSize = new[]
        {
            "▱▱▱▱▱▱▱▱▱▱",
            "▰▱▱▱▱▱▱▱▱▱",
            "▰▰▱▱▱▱▱▱▱▱",
            "▰▰▰▱▱▱▱▱▱▱",
            "▰▰▰▰▱▱▱▱▱▱",
            "▰▰▰▰▰▱▱▱▱▱",
            "▰▰▰▰▰▰▱▱▱▱",
            "▰▰▰▰▰▰▰▱▱▱",
            "▰▰▰▰▰▰▰▰▱▱",
            "▰▰▰▰▰▰▰▰▰▱",
            "▰▰▰▰▰▰▰▰▰▰",
        };
    }
}