using System.Drawing;

namespace EliteDataRelay.Configuration
{
    public static partial class AppConfiguration
    {
        // Overlays
        public static bool EnableInfoOverlay { get; set; } = false;
        public static bool EnableCargoOverlay { get; set; } = false;
        public static bool EnableSessionOverlay { get; set; } = false;
        public static bool EnableExplorationOverlay { get; set; } = false;
        public static bool EnableMiningOverlay { get; set; } = false;
        public static bool EnableProspectorOverlay { get; set; } = false;
        public static bool EnableJumpOverlay { get; set; } = false; // removed
        public static bool AllowOverlayDrag { get; set; } = true;
        public static Point InfoOverlayLocation { get; set; } = Point.Empty;
        public static Point CargoOverlayLocation { get; set; } = Point.Empty;
        public static Point SessionOverlayLocation { get; set; } = Point.Empty;
        public static Point ExplorationOverlayLocation { get; set; } = new Point(20, 20); // Default top-left
        public static Point MiningOverlayLocation { get; set; } = Point.Empty;
        public static Point ProspectorOverlayLocation { get; set; } = Point.Empty;
        public static Point JumpOverlayLocation { get; set; } = Point.Empty;
        public static string OverlayFontName { get; set; } = "Consolas";
        public static float OverlayFontSize { get; set; } = 12f;
        public static Color OverlayTextColor { get; set; } = Color.Orange;
        public static Color OverlayBackgroundColor { get; set; } = Color.FromArgb(200, 0, 0, 0);
        public static Color OverlayBorderColor { get; set; } = Color.FromArgb(255, 111, 0); // Elite Dangerous Orange
        public static int OverlayOpacity { get; set; } = 85;
        // Global toggle (legacy). Still persisted for migration.
        public static bool OverlayShowBorder { get; set; } = true;

        // Per-overlay border toggles
        public static bool OverlayShowBorderInfo { get; set; } = true;
        public static bool OverlayShowBorderCargo { get; set; } = true;
        public static bool OverlayShowBorderSession { get; set; } = true;
        public static bool OverlayShowBorderExploration { get; set; } = true;
        public static bool OverlayShowBorderMining { get; set; } = true;
        public static bool OverlayShowBorderProspector { get; set; } = true;
        public static bool OverlayShowBorderJump { get; set; } = false; // removed

        // Next Jump overlay options
        public static bool ShowNextJumpJumpsLeft { get; set; } = true;

        // Traffic display options
        public static bool ShowTrafficOnExplorationOverlay { get; set; } = true;
        public static bool ShowTrafficOnJumpOverlay { get; set; } = false; // removed
    }
}
