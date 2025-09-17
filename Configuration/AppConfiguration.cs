using System;
using System.IO;
using System.Reflection;
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
        public static int DebounceDelayMs { get; } = 250;
        public static int PollingIntervalMs { get; } = 1000;
        public static int FileSystemDelayMs { get; } = 50;
        public static int ThreadMaxRetries { get; } = 5;
        public static int ThreadRetryDelayMs { get; } = 100;
        public static int FileReadMaxAttempts { get; } = 5;
        public static int FileReadRetryDelayMs { get; } = 100;
        public static string AboutInfo { get; } = $"Elite Cargo Monitor v{GetAppVersion()}";
        public static string AboutUrl { get; } = "https://github.com/insert3coins/EliteCargoMonitor";

        public static bool EnableFileOutput { get; set; } = false;

        private const string SettingsFileName = "settings.json";

        private static string GetAppVersion()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version; 
                // Use Major.Minor.Build to reflect the full version from the .csproj file. in this case if there no version in the .csproj then we set one
                return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.8.6";
            }
            catch
            {
                return "0.8.6"; // Fallback
            }
        }

        private class SettingsModel
        {
            public string OutputFileFormat { get; set; } = AppConfiguration.OutputFileFormat;
            public string OutputFileName { get; set; } = AppConfiguration.OutputFileName;
            public string OutputDirectory { get; set; } = AppConfiguration.OutputDirectory;
            public bool EnableFileOutput { get; set; } = AppConfiguration.EnableFileOutput;
        }

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
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppConfiguration] Error loading settings: {ex.Message}");
                // Fallback to defaults if loading fails
            }
        }

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