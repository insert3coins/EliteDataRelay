using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Reflection;
using EliteCargoMonitor.Models;

namespace EliteCargoMonitor.Configuration
{
    /// <summary>
    /// Static class to hold application configuration values.
    /// Manages loading and saving user-configurable settings from/to settings.json.
    /// </summary>
    public static class AppConfiguration
    {
        private const string SettingsFileName = "settings.json";
        private static readonly string UserSavedGames = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games", "Frontier Developments", "Elite Dangerous");

        /// <summary>
        /// Gets the current application settings.
        /// </summary>
        public static AppSettings Settings { get; private set; } = new AppSettings();

        /// <summary>
        /// Loads settings from settings.json or creates a default file.
        /// </summary>
        public static void Load()
        {
            AppSettings? loadedSettings = null;
            if (File.Exists(SettingsFileName))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFileName);
                    loadedSettings = JsonSerializer.Deserialize<AppSettings>(json);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AppConfiguration] Error loading {SettingsFileName}: {ex.Message}. Using defaults.");
                }
            }

            Settings = loadedSettings ?? new AppSettings();

            // Apply defaults for any missing properties and mark for saving
            bool needsSave = false;
            if (string.IsNullOrEmpty(Settings.OutputFileFormat))
            {
                Settings.OutputFileFormat = "{count_slash_capacity} | {items}";
                needsSave = true;
            }

            if (string.IsNullOrEmpty(Settings.OutputFileName))
            {
                Settings.OutputFileName = "cargo.txt";
                needsSave = true;
            }

            // Save to create the file on first run or to add new default properties to an existing file
            if (needsSave || !File.Exists(SettingsFileName))
            {
                Save();
            }
        }

        /// <summary>
        /// Saves the current settings to settings.json.
        /// </summary>
        public static void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Settings, options);
                File.WriteAllText(SettingsFileName, json);
                Debug.WriteLine($"[AppConfiguration] Settings saved to {SettingsFileName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppConfiguration] Error saving {SettingsFileName}: {ex.Message}");
            }
        }

        /// <summary>
        /// The path to the Cargo.json file.
        /// </summary>
        public static string CargoPath { get; } = Path.Combine(UserSavedGames, "Cargo.json");

        /// <summary>
        /// The path to the Elite Dangerous Player Journal directory.
        /// This defaults to the standard journal location within the user's "Saved Games" folder.
        /// </summary>
        public static string JournalPath { get; } = UserSavedGames;

        /// <summary>
        /// Polling interval in milliseconds for checking file changes.
        /// </summary>
        public static int PollingIntervalMs { get; } = 1000;

        /// <summary>
        /// Debounce delay in milliseconds to wait for file write to complete.
        /// </summary>
        public static int DebounceDelayMs { get; } = 250;

        /// <summary>
        /// Extra delay to give the file system before reading a changed file.
        /// </summary>
        public static int FileSystemDelayMs { get; } = 50;

        /// <summary>
        /// Maximum number of retries for background thread operations.
        /// </summary>
        public static int ThreadMaxRetries { get; } = 5;

        /// <summary>
        /// Delay in milliseconds between thread retries.
        /// </summary>
        public static int ThreadRetryDelayMs { get; } = 100;

        /// <summary>
        /// Maximum number of attempts to read a file.
        /// </summary>
        public static int FileReadMaxAttempts { get; } = 5;

        /// <summary>
        /// Delay in milliseconds between file read retries.
        /// </summary>
        public static int FileReadRetryDelayMs { get; } = 100;

        /// <summary>
        /// Gets the application version from the assembly.
        /// </summary>
        public static string AppVersion { get; } = GetAppVersion();

        /// <summary>
        /// About text for the application.
        /// </summary>
        public static string AboutInfo { get; } = $"Elite Cargo Monitor v{AppVersion}" + Environment.NewLine + "Created by insert3coins";

        private static string GetAppVersion()
        {
            // Use AssemblyInformationalVersion for semantic versioning (e.g., 1.0.0-beta), which is set by the <Version> tag in the .csproj
            var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(version))
            {
                return version;
            }

            // Fallback to AssemblyVersion (e.g., 1.0.0.0) if the informational version isn't available for some reason.
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        }

        /// <summary>
        /// The URL for the project's GitHub page.
        /// </summary>
        public static string AboutUrl { get; } = "https://github.com/insert3coins/EliteCargoMonitor";

        /// <summary>
        /// Welcome message displayed on startup.
        /// </summary>
        public static string WelcomeMessage { get; } = "Welcome to Elite Cargo Monitor!";

        /// <summary>
        /// Output directory for the cargo text file.
        /// </summary>
        public static string OutputDirectory { get; } = "out";

        /// <summary>
        /// Output file name for the cargo text file.
        /// </summary>
        public static string OutputFileName
        {
            get => Settings.OutputFileName ?? "cargo.txt";
            set => Settings.OutputFileName = value;
        }

        /// <summary>
        /// The format string for the cargo.txt output file.
        /// </summary>
        public static string OutputFileFormat
        {
            get => Settings.OutputFileFormat ?? "{count_slash_capacity} | {items}";
            set => Settings.OutputFileFormat = value;
        }

        // UI Configuration
        public static int FormWidth { get; } = 800;
        public static int FormHeight { get; } = 600;
        public static int ButtonHeight { get; } = 30;
        public static int ButtonPanelHeight { get; } = 40;
        public static float DefaultFontSize { get; } = 9.0f;
        public static string VerdanaFontName { get; } = "Verdana";
        public static string ConsolasFontName { get; } = "Consolas";
        public static int MaxTextBoxLines { get; } = 100;
    }
}