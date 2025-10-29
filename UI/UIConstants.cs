using System.Drawing;
using System.Collections.Generic;

namespace EliteDataRelay.UI
{
      // Contains static UI constants like colors and design elements.
    public static class UIConstants
    {
        // Sizing constants
        public static int ButtonPanelHeight { get; } = 35;

        // Colors for button states to provide better visual feedback.
        public static readonly Color DefaultButtonBackColor = Color.FromArgb(240, 240, 240);
        public static readonly Color StartButtonActiveColor = Color.FromArgb(232, 245, 233); // A subtle light green
        public static readonly Color StopButtonActiveColor = Color.FromArgb(252, 232, 232); // A subtle light red

        // A map of ship internal names to their ASCII art representation.
        // Reimagined designs, grouped by ship size.
        public static readonly Dictionary<string, string> ShipArtMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // --- Small Ships (Reimagined) ---
            { "SideWinder", ">-[O]-<" },
            { "Eagle", ">--=>>" },
            { "Hauler", "[O| >" },
            { "Adder", "[<O>]" },
            { "Imperial_Eagle", ">--{E}>" },
            { "Viper", ">-(V)-<" },
            { "Viper_MkIV", ">-(W)-<" },
            { "CobraMkIII", "<(O)>" },
            { "CobraMkIV", "<(=O=)>" },
            { "DiamondbackScout", "<¤>" },
            { "Dolphin", "~(_o_)~" },
            { "Imperial_Courier", "~>i<~" },
            { "Vulture", "(vVv)" },

            // --- Medium Ships (Reimagined) ---
            { "DiamondbackXL", "<¤===>" },
            { "Keelback", "[<H>]" },
            { "Type6", "[■]" },
            { "Asp", "<( O )>" },
            { "Asp_Scout", "<( o )>" },
            { "Federal_Dropship", "[|-|]" },
            { "Federal_Assault_Ship", "[|^|]" },
            { "Federal_Gunship", "[|T|]" },
            { "Imperial_Clipper", "~<==()=>~" },
            { "Krait_MkII", "<|o|>" },
            { "Krait_Phantom", "<|·|>" },
            { "Mamba", ">->X<-<" },
            { "FerDeLance", ">-(~)-<" },
            { "Python", "([O])" },
            { "Orca", "~<OOO>~" },
            { "Chieftain", "</_^_\\>" },
            { "Crusader", "</_v_\\>" },
            { "Challenger", "</#=#\\>" },

            // --- Large Ships (Reimagined) ---
            { "Type7", "[[■]]" },
            { "Type9", "([OO])" },
            { "Type10", "([##])" },
            { "PantherClipper", "<<<[OO]>>>" },
            { "BelugaLiner", "~<OOOOO>~" },
            { "Anaconda", "<===(O)====>" },
            { "Federal_Corvette", "<<==(V)==>>" },
            { "Imperial_Cutter", "~<===(O)===~>" },
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



