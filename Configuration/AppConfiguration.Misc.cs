using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.Configuration
{
    public static partial class AppConfiguration
    {
        public static int PollingIntervalMs { get => _settings.PollingIntervalMs; set => _settings.PollingIntervalMs = value; }
        public static bool EnableSessionTracking { get => _settings.EnableSessionTracking; set => _settings.EnableSessionTracking = value; }
        public static string WelcomeMessage => _settings.WelcomeMessage;

        // Window Settings
        public static Point WindowLocation { get => _settings.WindowLocation; set => _settings.WindowLocation = value; }
        public static FormWindowState WindowState { get => _settings.WindowState; set => _settings.WindowState = value; }

        // Misc Technical Settings
        public static int DebounceDelayMs { get => _settings.DebounceDelayMs; set => _settings.DebounceDelayMs = value; }
        public static int FileReadMaxAttempts { get => _settings.FileReadMaxAttempts; set => _settings.FileReadMaxAttempts = value; }
        public static int FileReadRetryDelayMs { get => _settings.FileReadRetryDelayMs; set => _settings.FileReadRetryDelayMs = value; }
        public static int FileSystemDelayMs { get => _settings.FileSystemDelayMs; set => _settings.FileSystemDelayMs = value; }
        public static int ThreadMaxRetries { get => _settings.ThreadMaxRetries; set => _settings.ThreadMaxRetries = value; }
        public static int ThreadRetryDelayMs { get => _settings.ThreadRetryDelayMs; set => _settings.ThreadRetryDelayMs = value; }

        // Font Settings
        public static string ConsolasFontName => _settings.ConsolasFontName;
        public static float DefaultFontSize => _settings.DefaultFontSize;

        private partial class AppSettings
        {
            public int PollingIntervalMs { get; set; } = 1000;
            public bool EnableSessionTracking { get; set; } = true;
            public string WelcomeMessage { get; set; } = "Click 'Start' to begin monitoring.";
            public Point WindowLocation { get; set; } = Point.Empty;
            public FormWindowState WindowState { get; set; } = FormWindowState.Normal;
            public int DebounceDelayMs { get; set; } = 250;
            public int FileReadMaxAttempts { get; set; } = 5;
            public int FileReadRetryDelayMs { get; set; } = 100;
            public int FileSystemDelayMs { get; set; } = 500;
            public int ThreadMaxRetries { get; set; } = 5;
            public int ThreadRetryDelayMs { get; set; } = 100;
            public string ConsolasFontName { get; set; } = "Consolas";
            public float DefaultFontSize { get; set; } = 10f;
        }
    }
}