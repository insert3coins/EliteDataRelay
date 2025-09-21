using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Drawing;

namespace EliteDataRelay.Configuration
{
    public static class AppConfiguration
    {
        private static readonly string _settingsPath;
        private static AppSettings _settings = new AppSettings();

        /// <summary>
        /// Gets the path to the application's data directory in AppData.
        /// </summary>
        public static string AppDataPath { get; }

        public static string StatusJsonPath => Path.Combine(JournalPath, "Status.json");

        // This path is usually found dynamically, but we provide a config setting for it.
        public static string CargoPath { get => _settings.CargoPath; set => _settings.CargoPath = value; }

        // Public properties to access settings from other parts of the application
        public static string JournalPath
        {
            get => _settings.JournalPath;
            set => _settings.JournalPath = value;
        }
        public static int PollingIntervalMs
        {
            get => _settings.PollingIntervalMs;
            set => _settings.PollingIntervalMs = value;
        }
        public static bool EnableSessionTracking
        {
            get => _settings.EnableSessionTracking;
            set => _settings.EnableSessionTracking = value;
        }
        public static bool ShowSessionOnOverlay
        {
            get => _settings.ShowSessionOnOverlay;
            set => _settings.ShowSessionOnOverlay = value;
        }
        public static Color OverlayBackgroundColor
        {
            get => Color.FromArgb(_settings.OverlayBackgroundColorArgb);
            set => _settings.OverlayBackgroundColorArgb = value.ToArgb();
        }
        public static string OverlayFontName
        {
            get => _settings.OverlayFontName;
            set => _settings.OverlayFontName = value;
        }
        public static float OverlayFontSize
        {
            get => _settings.OverlayFontSize;
            set => _settings.OverlayFontSize = value;
        }
        public static int OverlayOpacity
        {
            get => _settings.OverlayOpacity;
            set => _settings.OverlayOpacity = value;
        }
        public static bool PinMaterialsMode
        {
            get => _settings.PinMaterialsMode;
            set => _settings.PinMaterialsMode = value;
        }
        public static bool EnableHotkeys
        {
            get => _settings.EnableHotkeys;
            set => _settings.EnableHotkeys = value;
        }

        // File Output Settings
        public static bool EnableFileOutput { get => _settings.EnableFileOutput; set => _settings.EnableFileOutput = value; }
        public static string OutputFileFormat { get => _settings.OutputFileFormat; set => _settings.OutputFileFormat = value; }
        public static string OutputFileName { get => _settings.OutputFileName; set => _settings.OutputFileName = value; }
        public static string OutputDirectory { get => _settings.OutputDirectory; set => _settings.OutputDirectory = value; }

        // Overlay Settings
        public static bool EnableLeftOverlay { get => _settings.EnableLeftOverlay; set => _settings.EnableLeftOverlay = value; }
        public static bool EnableRightOverlay { get => _settings.EnableRightOverlay; set => _settings.EnableRightOverlay = value; }
        public static bool EnableMaterialsOverlay { get => _settings.EnableMaterialsOverlay; set => _settings.EnableMaterialsOverlay = value; }
        public static bool AllowOverlayDrag { get => _settings.AllowOverlayDrag; set => _settings.AllowOverlayDrag = value; }
        public static Color OverlayTextColor { get => Color.FromArgb(_settings.OverlayTextColorArgb); set => _settings.OverlayTextColorArgb = value.ToArgb(); }
        public static Point LeftOverlayLocation { get => _settings.LeftOverlayLocation; set => _settings.LeftOverlayLocation = value; }
        public static Point RightOverlayLocation { get => _settings.RightOverlayLocation; set => _settings.RightOverlayLocation = value; }
        public static Point MaterialsOverlayLocation { get => _settings.MaterialsOverlayLocation; set => _settings.MaterialsOverlayLocation = value; }

        // Hotkey Settings
        public static Keys StartMonitoringHotkey { get => _settings.StartMonitoringHotkey; set => _settings.StartMonitoringHotkey = value; }
        public static Keys StopMonitoringHotkey { get => _settings.StopMonitoringHotkey; set => _settings.StopMonitoringHotkey = value; }
        public static Keys ShowOverlayHotkey { get => _settings.ShowOverlayHotkey; set => _settings.ShowOverlayHotkey = value; }
        public static Keys HideOverlayHotkey { get => _settings.HideOverlayHotkey; set => _settings.HideOverlayHotkey = value; }

        // Window Settings
        public static Size WindowSize { get => _settings.WindowSize; set => _settings.WindowSize = value; }
        public static Point WindowLocation { get => _settings.WindowLocation; set => _settings.WindowLocation = value; }
        public static FormWindowState WindowState { get => _settings.WindowState; set => _settings.WindowState = value; }

        // Misc Technical Settings
        public static int DebounceDelayMs { get => _settings.DebounceDelayMs; set => _settings.DebounceDelayMs = value; }
        public static int FileReadMaxAttempts { get => _settings.FileReadMaxAttempts; set => _settings.FileReadMaxAttempts = value; }
        public static int FileReadRetryDelayMs { get => _settings.FileReadRetryDelayMs; set => _settings.FileReadRetryDelayMs = value; }
        public static int FileSystemDelayMs { get => _settings.FileSystemDelayMs; set => _settings.FileSystemDelayMs = value; }
        public static int ThreadMaxRetries { get => _settings.ThreadMaxRetries; set => _settings.ThreadMaxRetries = value; }
        public static int ThreadRetryDelayMs { get => _settings.ThreadRetryDelayMs; set => _settings.ThreadRetryDelayMs = value; }

        // About/Info Settings
        public static string AboutInfo => _settings.AboutInfo;
        public static string AboutUrl => _settings.AboutUrl;
        public static string LicenseUrl => _settings.LicenseUrl;

        // Font Settings
        public static string ConsolasFontName => _settings.ConsolasFontName;
        public static float DefaultFontSize => _settings.DefaultFontSize;

        public static string WelcomeMessage => _settings.WelcomeMessage;
        // This is a reference type, so callers can already modify the contents of the set.
        public static HashSet<string> PinnedMaterials
        {
            get => _settings.PinnedMaterials;
            set => _settings.PinnedMaterials = value;
        }

        /// <summary>
        /// Static constructor to set up paths and load settings on application start.
        /// </summary>
        static AppConfiguration()
        {
            // Define the application's data directory in AppData\Roaming
            AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EliteDataRelay");
            Directory.CreateDirectory(AppDataPath); // Ensure the directory exists

            // Define the full path for the settings file
            _settingsPath = Path.Combine(AppDataPath, "settings.json");

            Load();
        }

        public static void Load()
        {
            if (File.Exists(_settingsPath))
            {
                try
                {
                    var json = File.ReadAllText(_settingsPath);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    Debug.WriteLine($"[AppConfiguration] Settings loaded from {_settingsPath}");

                    // Self-healing: If the loaded journal path is invalid, re-detect it and save.
                    // This prevents issues if the settings file becomes corrupted or was saved with an empty path.
                    if (string.IsNullOrWhiteSpace(_settings.JournalPath))
                    {
                        Debug.WriteLine("[AppConfiguration] Loaded JournalPath is empty. Re-detecting default path.");
                        _settings.JournalPath = FindDefaultJournalPath();
                        _settings.CargoPath = Path.Combine(_settings.JournalPath, "Cargo.json");
                        Save(); // Save the corrected settings immediately.
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading settings: {ex.Message}", "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _settings = new AppSettings();
                }
            }
            else
            {
                // If settings file doesn't exist, create a new one with defaults
                _settings = new AppSettings();
                // Auto-detect journal path on first run
                _settings.JournalPath = FindDefaultJournalPath();
                _settings.CargoPath = Path.Combine(_settings.JournalPath, "Cargo.json"); // Default assumption
                Save();
                Debug.WriteLine($"[AppConfiguration] Default settings created at {_settingsPath}");
            }
        }

        public static void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Attempts to find the default Elite Dangerous journal path.
        /// </summary>
        private static string FindDefaultJournalPath()
        {
            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string savedGamesPath = Path.Combine(userProfile, "Saved Games", "Frontier Developments", "Elite Dangerous");
                // Always return the expected default path. The check for its existence will happen when monitoring starts.
                return savedGamesPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppConfiguration] Error finding default journal path: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// A private class to hold all settings for easy serialization.
        /// </summary>
        private class AppSettings
        {
            public string CargoPath { get; set; } = string.Empty;
            public string JournalPath { get; set; } = string.Empty;
            public int PollingIntervalMs { get; set; } = 1000;
            public bool EnableSessionTracking { get; set; } = true;
            public bool ShowSessionOnOverlay { get; set; } = true;
            public string WelcomeMessage { get; set; } = "Click 'Start' to begin monitoring.";
            public HashSet<string> PinnedMaterials { get; set; } = new HashSet<string>();
            public int OverlayBackgroundColorArgb { get; set; } = Color.FromArgb(200, 0, 0, 0).ToArgb();
            public string OverlayFontName { get; set; } = "Eurostile";
            public float OverlayFontSize { get; set; } = 10f;
            public int OverlayOpacity { get; set; } = 85;
            public bool PinMaterialsMode { get; set; } = false;
            public bool EnableHotkeys { get; set; } = true;
            public bool EnableFileOutput { get; set; } = false;
            public string OutputFileFormat { get; set; } = "{name} - {count}";
            public string OutputFileName { get; set; } = "cargo.txt";
            public string OutputDirectory { get; set; } = string.Empty;
            public bool EnableLeftOverlay { get; set; } = false;
            public bool EnableRightOverlay { get; set; } = false;
            public bool EnableMaterialsOverlay { get; set; } = false;
            public bool AllowOverlayDrag { get; set; } = false;
            public int OverlayTextColorArgb { get; set; } = Color.Orange.ToArgb();
            public Point LeftOverlayLocation { get; set; } = Point.Empty;
            public Point RightOverlayLocation { get; set; } = Point.Empty;
            public Point MaterialsOverlayLocation { get; set; } = Point.Empty;
            public Keys StartMonitoringHotkey { get; set; } = Keys.F1;
            public Keys StopMonitoringHotkey { get; set; } = Keys.F2;
            public Keys ShowOverlayHotkey { get; set; } = Keys.F3;
            public Keys HideOverlayHotkey { get; set; } = Keys.F4;
            public Size WindowSize { get; set; } = new Size(800, 600);
            public Point WindowLocation { get; set; } = Point.Empty;
            public FormWindowState WindowState { get; set; } = FormWindowState.Normal;
            public int DebounceDelayMs { get; set; } = 250;
            public int FileReadMaxAttempts { get; set; } = 5;
            public int FileReadRetryDelayMs { get; set; } = 100;
            public int FileSystemDelayMs { get; set; } = 500;
            public int ThreadMaxRetries { get; set; } = 5;
            public int ThreadRetryDelayMs { get; set; } = 100;
            public string AboutInfo { get; set; } = "Elite Data Relay provides real-time cargo and material tracking.";
            public string AboutUrl { get; set; } = "https://github.com/your-repo";
            public string LicenseUrl { get; set; } = "https://github.com/your-repo/blob/main/LICENSE.txt";
            public string ConsolasFontName { get; set; } = "Consolas";
            public float DefaultFontSize { get; set; } = 10f;
        }
    }
}