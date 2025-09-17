using System;
using System.IO;
using System.Text.Json;

namespace EliteCargoMonitor.Configuration
{
    public static class AppConfiguration
    {
        // --- User-configurable settings (saved to settings.json) ---
        public static string OutputFileFormat { get; set; } = "{count_slash_capacity} | {items}";
        public static string OutputFileName { get; set; } = "cargo.txt";
        public static string OutputDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "out");

        // --- Application constants and paths ---
        public static string CargoPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games", "Frontier Developments", "Elite Dangerous", "Cargo.json");
        public static string JournalPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games", "Frontier Developments", "Elite Dangerous");
        public static int FormWidth { get; } = 600;
        public static int FormHeight { get; } = 400;
        public static int ButtonHeight { get; } = 23;
        public static int ButtonPanelHeight { get; } = 35;
        public static float DefaultFontSize { get; } = 9f;
        public static string ConsolasFontName { get; } = "Consolas";
        public static string WelcomeMessage { get; } = "Welcome to Elite Cargo Monitor. Click Start to begin.";
        public static int MaxTextBoxLines { get; } = 100;
        public static int DebounceDelayMs { get; } = 250;
        public static int PollingIntervalMs { get; } = 1000;
        public static int FileSystemDelayMs { get; } = 50;
        public static int ThreadMaxRetries { get; } = 5;
        public static int ThreadRetryDelayMs { get; } = 100;
        public static int FileReadMaxAttempts { get; } = 5;
        public static int FileReadRetryDelayMs { get; } = 100;
        public static string AboutInfo { get; } = "Elite Cargo Monitor v1.0";
        public static string AboutUrl { get; } = "https://github.com/insert3coins/EliteCargoMonitor";

        private const string SettingsFileName = "settings.json";

        /// <summary>
        /// A private model for serializing/deserializing settings.
        /// </summary>
        private class SettingsModel
        {
            public string OutputFileFormat { get; set; } = AppConfiguration.OutputFileFormat;
            public string OutputFileName { get; set; } = AppConfiguration.OutputFileName;
            public string OutputDirectory { get; set; } = AppConfiguration.OutputDirectory;
        }

        /// <summary>
        /// Loads settings from settings.json. If the file doesn't exist or fails to load, defaults are used.
        /// </summary>
        public static void Load()
        {
            try
            {
                if (!File.Exists(SettingsFileName)) return;

                string json = File.ReadAllText(SettingsFileName);
                var model = JsonSerializer.Deserialize<SettingsModel>(json);

                if (model != null)
                {
                    OutputFileFormat = model.OutputFileFormat;
                    OutputFileName = model.OutputFileName;
                    OutputDirectory = model.OutputDirectory;
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