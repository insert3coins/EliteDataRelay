using System.Windows.Forms;

namespace EliteDataRelay.Configuration
{
    public static partial class AppConfiguration
    {
        public static bool EnableHotkeys { get => _settings.EnableHotkeys; set => _settings.EnableHotkeys = value; }
        public static Keys StartMonitoringHotkey { get => _settings.StartMonitoringHotkey; set => _settings.StartMonitoringHotkey = value; }
        public static Keys StopMonitoringHotkey { get => _settings.StopMonitoringHotkey; set => _settings.StopMonitoringHotkey = value; }
        public static Keys ShowOverlayHotkey { get => _settings.ShowOverlayHotkey; set => _settings.ShowOverlayHotkey = value; }
        public static Keys HideOverlayHotkey { get => _settings.HideOverlayHotkey; set => _settings.HideOverlayHotkey = value; }

        private partial class AppSettings
        {
            public bool EnableHotkeys { get; set; } = true; // Hotkeys are enabled by default for new installations.
            public Keys StartMonitoringHotkey { get; set; } = Keys.F1;
            public Keys StopMonitoringHotkey { get; set; } = Keys.F2;
            public Keys ShowOverlayHotkey { get; set; } = Keys.F3;
            public Keys HideOverlayHotkey { get; set; } = Keys.F4;
        }
    }
}