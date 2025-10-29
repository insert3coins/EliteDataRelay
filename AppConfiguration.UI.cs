using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.Configuration
{
    public static partial class AppConfiguration
    {
        // Main Window
        public static Point WindowLocation { get; set; } = Point.Empty;
        public static FormWindowState WindowState { get; set; } = FormWindowState.Normal;

        // Fonts
        public static float DefaultFontSize { get; set; } = 9f;
        public static string ConsolasFontName { get; set; } = "Consolas";
    }
}
