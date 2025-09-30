using System.Drawing;

namespace EliteDataRelay.Configuration
{
    public static partial class AppConfiguration
    {
        // Overlay Settings
        public static bool EnableInfoOverlay { get => _settings.EnableInfoOverlay; set => _settings.EnableInfoOverlay = value; }
        public static bool EnableCargoOverlay { get => _settings.EnableCargoOverlay; set => _settings.EnableCargoOverlay = value; }
        public static bool EnableShipIconOverlay { get => _settings.EnableShipIconOverlay; set => _settings.EnableShipIconOverlay = value; }
        public static bool ShowSessionOnOverlay { get => _settings.ShowSessionOnOverlay; set => _settings.ShowSessionOnOverlay = value; }
        public static bool AllowOverlayDrag { get => _settings.AllowOverlayDrag; set => _settings.AllowOverlayDrag = value; }
        public static Color OverlayTextColor { get => Color.FromArgb(_settings.OverlayTextColorArgb); set => _settings.OverlayTextColorArgb = value.ToArgb(); }
        public static Color OverlayBackgroundColor { get => Color.FromArgb(_settings.OverlayBackgroundColorArgb); set => _settings.OverlayBackgroundColorArgb = value.ToArgb(); }
        public static string OverlayFontName { get => _settings.OverlayFontName; set => _settings.OverlayFontName = value; }
        public static float OverlayFontSize { get => _settings.OverlayFontSize; set => _settings.OverlayFontSize = value; }
        public static int OverlayOpacity { get => _settings.OverlayOpacity; set => _settings.OverlayOpacity = value; }
        public static Point InfoOverlayLocation { get => _settings.InfoOverlayLocation; set => _settings.InfoOverlayLocation = value; }
        public static Point CargoOverlayLocation { get => _settings.CargoOverlayLocation; set => _settings.CargoOverlayLocation = value; }
        public static Point ShipIconOverlayLocation { get => _settings.ShipIconOverlayLocation; set => _settings.ShipIconOverlayLocation = value; }

        private partial class AppSettings
        {
            public bool EnableInfoOverlay { get; set; } = false;
            public bool EnableCargoOverlay { get; set; } = false;
            public bool EnableShipIconOverlay { get; set; } = true;
            public bool ShowSessionOnOverlay { get; set; } = true;
            public bool AllowOverlayDrag { get; set; } = false;
            public int OverlayTextColorArgb { get; set; } = Color.Orange.ToArgb();
            public int OverlayBackgroundColorArgb { get; set; } = Color.FromArgb(150, 0, 0, 0).ToArgb();
            public string OverlayFontName { get; set; } = "Consolas";
            public float OverlayFontSize { get; set; } = 12f;
            public int OverlayOpacity { get; set; } = 70;
            public Point InfoOverlayLocation { get; set; } = Point.Empty;
            public Point CargoOverlayLocation { get; set; } = Point.Empty;
            public Point ShipIconOverlayLocation { get; set; } = Point.Empty;
        }
    }
}