using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Text.Json;

namespace EliteDataRelay.Configuration
{
    public static class AppConfiguration
    {
        // --- Default values ---

        // --- User-configurable settings (saved to settings.json) ---
        public static string OutputFileFormat { get; set; } = "{count_slash_capacity} | {items}";
        public static string OutputFileName { get; set; } = "cargo.txt";
        public static string OutputDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "out");
        public static bool EnableFileOutput { get; set; } = false;
        public static bool EnableLeftOverlay { get; set; } = false;
        public static bool EnableRightOverlay { get; set; } = false;
        public static bool ShowSessionOnOverlay { get; set; } = false;
        public static bool EnableSessionTracking { get; set; } = false;
        public static bool EnableHotkeys { get; set; } = false;

        // --- Hotkey settings ---
        public static Keys StartMonitoringHotkey { get; set; } = Keys.Control | Keys.Alt | Keys.F9;
        public static Keys StopMonitoringHotkey { get; set; } = Keys.Control | Keys.Alt | Keys.F10;
        public static Keys ShowOverlayHotkey { get; set; } = Keys.Control | Keys.Alt | Keys.F11;
        public static Keys HideOverlayHotkey { get; set; } = Keys.Control | Keys.Alt | Keys.F12;

        // --- Application constants and paths ---
        public static string CargoPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games", "Frontier Developments", "Elite Dangerous", "Cargo.json");
        public static string JournalPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games", "Frontier Developments", "Elite Dangerous");
        public static string StatusJsonPath { get; } = Path.Combine(JournalPath, "Status.json");
        public static int ButtonHeight { get; } = 23;
        public static float DefaultFontSize { get; } = 9f;
        public static string ConsolasFontName { get; } = "Consolas";
        public static string WelcomeMessage { get; } = "Welcome to Elite Data Relay. Click Start to begin.";
        public static int DebounceDelayMs { get; } = 250;
        public static int PollingIntervalMs { get; } = 1000;
        public static int FileSystemDelayMs { get; } = 50;
        public static int ThreadMaxRetries { get; } = 5;
        public static int ThreadRetryDelayMs { get; } = 100;
        public static int FileReadMaxAttempts { get; } = 5;
        public static int FileReadRetryDelayMs { get; } = 100;
        public static string AboutInfo { get; } = $"Elite Data Relay v{GetAppVersion()}";
        public static string AboutUrl { get; } = "https://github.com/insert3coins/EliteDataRelay";
        public static string LicenseUrl { get; } = "https://github.com/insert3coins/EliteDataRelay/blob/main/LICENSE.txt";

        // --- Window and Sizing Constants ---
        public static int FormWidth { get; } = 670;
        public static int FormHeight { get; } = 400;
        public static Size WindowSize { get; set; } = new Size(FormWidth, FormHeight);
        public static Point WindowLocation { get; set; } = Point.Empty;
        public static FormWindowState WindowState { get; set; } = FormWindowState.Normal;

        private const string SettingsFileName = "settings.json";

        /// Gets the application version from the assembly.
        private static string GetAppVersion()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version; 
                // Use Major.Minor.Build to reflect the full version from the .csproj file.
                return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.8.6";
            }
            catch
            {
                return "0.8.6"; // Fallback
            }
        }

        /// A private model for serializing/deserializing settings.
        private class SettingsModel
        {
            public string OutputFileFormat { get; set; } = AppConfiguration.OutputFileFormat;
            public string OutputFileName { get; set; } = AppConfiguration.OutputFileName;
            public string OutputDirectory { get; set; } = AppConfiguration.OutputDirectory;
            public bool EnableFileOutput { get; set; } = AppConfiguration.EnableFileOutput;
            public bool EnableLeftOverlay { get; set; } = AppConfiguration.EnableLeftOverlay;
            public bool EnableRightOverlay { get; set; } = AppConfiguration.EnableRightOverlay;
            public bool ShowSessionOnOverlay { get; set; } = AppConfiguration.ShowSessionOnOverlay;
            public bool EnableSessionTracking { get; set; } = AppConfiguration.EnableSessionTracking;
            public bool EnableHotkeys { get; set; } = AppConfiguration.EnableHotkeys;
            public Keys StartMonitoringHotkey { get; set; } = AppConfiguration.StartMonitoringHotkey;
            public Keys StopMonitoringHotkey { get; set; } = AppConfiguration.StopMonitoringHotkey;
            public Keys ShowOverlayHotkey { get; set; } = AppConfiguration.ShowOverlayHotkey;
            public Keys HideOverlayHotkey { get; set; } = AppConfiguration.HideOverlayHotkey;
            public Size WindowSize { get; set; }
            public Point WindowLocation { get; set; }
            public FormWindowState WindowState { get; set; }
        }

        /// Loads settings from settings.json. If the file doesn't exist, it is created with default values.
        public static void Load()
        {
            try
            {
                if (!File.Exists(SettingsFileName))
                {
                    Save(); // Create file with default values
                    return;
                }

                string json = File.ReadAllText(SettingsFileName);
                var model = JsonSerializer.Deserialize<SettingsModel>(json);

                if (model != null)
                {
                    OutputFileFormat = model.OutputFileFormat;
                    OutputFileName = model.OutputFileName;
                    OutputDirectory = model.OutputDirectory;
                    EnableFileOutput = model.EnableFileOutput;
                    EnableLeftOverlay = model.EnableLeftOverlay;
                    EnableRightOverlay = model.EnableRightOverlay;
                    ShowSessionOnOverlay = model.ShowSessionOnOverlay;
                    EnableSessionTracking = model.EnableSessionTracking;
                    EnableHotkeys = model.EnableHotkeys;
                    StartMonitoringHotkey = model.StartMonitoringHotkey;
                    StopMonitoringHotkey = model.StopMonitoringHotkey;
                    ShowOverlayHotkey = model.ShowOverlayHotkey;
                    HideOverlayHotkey = model.HideOverlayHotkey;

                    // Load window settings, with validation
                    if (model.WindowSize.Width >= 300 && model.WindowSize.Height >= 200) // Basic sanity check
                    {
                        WindowSize = model.WindowSize;
                    }
                    WindowLocation = model.WindowLocation;
                    WindowState = model.WindowState;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppConfiguration] Error loading settings: {ex.Message}");
                // Fallback to defaults if loading fails
            }
        }

        /// Saves the current settings to settings.json.
        public static void Save()
        {
            try
            {
                var model = new SettingsModel
                {
                    OutputFileFormat = OutputFileFormat,
                    OutputFileName = OutputFileName,
                    OutputDirectory = OutputDirectory,
                    EnableFileOutput = EnableFileOutput,
                    EnableLeftOverlay = EnableLeftOverlay,
                    EnableRightOverlay = EnableRightOverlay,
                    ShowSessionOnOverlay = ShowSessionOnOverlay,
                    EnableSessionTracking = EnableSessionTracking,
                    EnableHotkeys = EnableHotkeys,
                    StartMonitoringHotkey = StartMonitoringHotkey,
                    StopMonitoringHotkey = StopMonitoringHotkey,
                    ShowOverlayHotkey = ShowOverlayHotkey,
                    HideOverlayHotkey = HideOverlayHotkey,
                    WindowSize = WindowSize,
                    WindowLocation = WindowLocation,
                    WindowState = WindowState
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(model, options);
                File.WriteAllText(SettingsFileName, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppConfiguration] Error saving settings: {ex.Message}");
            }
        }
    }
}