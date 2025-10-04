using System.Drawing;

namespace EliteDataRelay.Configuration
{
    public static partial class AppConfiguration
    {
        // Overlays
        public static bool EnableInfoOverlay { get; set; } = false;
        public static bool EnableCargoOverlay { get; set; } = false;
        public static bool EnableShipIconOverlay { get; set; } = false;
        public static bool ShowSessionOnOverlay { get; set; } = false;
        public static bool AllowOverlayDrag { get; set; } = true;
        public static Point InfoOverlayLocation { get; set; } = Point.Empty;
        public static Point CargoOverlayLocation { get; set; } = Point.Empty;
        public static Point ShipIconOverlayLocation { get; set; } = Point.Empty;
        public static string OverlayFontName { get; set; } = "Consolas";
        public static float OverlayFontSize { get; set; } = 12f;
        public static Color OverlayTextColor { get; set; } = Color.Orange;
        public static Color OverlayBackgroundColor { get; set; } = Color.FromArgb(200, 0, 0, 0);
        public static int OverlayOpacity { get; set; } = 85;
    }
}