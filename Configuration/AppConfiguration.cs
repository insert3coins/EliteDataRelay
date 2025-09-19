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
        // --- User-configurable settings (saved to settings.json) ---
        public static string OutputFileFormat { get; set; } = "{count_slash_capacity} | {items}";
        public static string OutputFileName { get; set; } = "cargo.txt";
        public static string OutputDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "out");
        public static bool EnableFileOutput { get; set; } = false;
        public static bool EnableOverlay { get; set; } = false;

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
        public static int FormWidth { get; } = 600;
        public static int FormHeight { get; } = 400;
        public static Size WindowSize { get; set; } = new Size(FormWidth, FormHeight);
        public static Point WindowLocation { get; set; } = Point.Empty;
        public static FormWindowState WindowState { get; set; } = FormWindowState.Normal;

        private const string SettingsFileName = "settings.json";

        /// <summary>
        /// Gets the application version from the assembly.
        /// </summary>
        /// <returns>The application version string (e.g., "0.8.6").</returns>
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

        /// <summary>
        /// A private model for serializing/deserializing settings.
        /// </summary>
        private class SettingsModel
        {
            public string OutputFileFormat { get; set; } = AppConfiguration.OutputFileFormat;
            public string OutputFileName { get; set; } = AppConfiguration.OutputFileName;
            public string OutputDirectory { get; set; } = AppConfiguration.OutputDirectory;
            public bool EnableFileOutput { get; set; } = AppConfiguration.EnableFileOutput;
            public bool EnableOverlay { get; set; } = AppConfiguration.EnableOverlay;
            public Size WindowSize { get; set; }
            public Point WindowLocation { get; set; }
            public FormWindowState WindowState { get; set; }
        }

        /// <summary>
        /// Loads settings from settings.json. If the file doesn't exist, it is created with default values.
        /// </summary>
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
                    EnableOverlay = model.EnableOverlay;

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

        /// <summary>
        /// Saves the current settings to settings.json.
        /// </summary>
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
                    EnableOverlay = EnableOverlay,
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