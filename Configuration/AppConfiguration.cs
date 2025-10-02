using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Drawing;

namespace EliteDataRelay.Configuration
{
    public static partial class AppConfiguration
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

        public static string JournalPath
        {
            get => _settings.JournalPath;
            set => _settings.JournalPath = value;
        }

        /// <summary>
        /// Static constructor to set up paths and load settings on application start.
        /// </summary>
        static AppConfiguration()
        {
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
        private partial class AppSettings
        {
            public string CargoPath { get; set; } = string.Empty;
            public string JournalPath { get; set; } = string.Empty;
        }
    }
}