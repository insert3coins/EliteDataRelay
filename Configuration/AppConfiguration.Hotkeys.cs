using System.Windows.Forms;

namespace EliteDataRelay.Configuration
{
    public static partial class AppConfiguration
    {
        public static bool EnableHotkeys { get; set; } = true;
        public static Keys StartMonitoringHotkey { get; set; } = Keys.Control | Keys.Alt | Keys.F9;
        public static Keys StopMonitoringHotkey { get; set; } = Keys.Control | Keys.Alt | Keys.F10;
        public static Keys ShowOverlayHotkey { get; set; } = Keys.Control | Keys.Alt | Keys.F11;
        public static Keys HideOverlayHotkey { get; set; } = Keys.Control | Keys.Alt | Keys.F12;
    }
}