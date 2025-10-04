using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace EliteDataRelay.Configuration

{
    public static partial class AppConfiguration
    {
        private static readonly string SettingsFilePath;

        #region Properties

        // General

        #endregion

        static AppConfiguration()
        {
            // The static constructor ensures that all dependent properties are initialized
            // in the correct order, resolving the CS8604 warning.
            SettingsFilePath = Path.Combine(AppDataPath, "settings.json");
            OutputDirectory = Path.Combine(AppDataPath, "output");
            StartSoundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds/start.wav");
            StopSoundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds/stop.wav");
            // Any other properties that depend on AppDataPath or other static properties can be initialized here.
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var config = JsonSerializer.Deserialize<ConfigData>(json);
                    if (config != null)
                    {
                        // Map all properties from the loaded config data
                        WelcomeMessage = config.WelcomeMessage;
                        OutputDirectory = config.OutputDirectory;
                        OutputFileName = config.OutputFileName;
                        CargoPath = config.CargoPath;
                        EnableFileOutput = config.EnableFileOutput;
                        OutputFileFormat = config.OutputFileFormat;
                        EnableInfoOverlay = config.EnableInfoOverlay;
                        EnableCargoOverlay = config.EnableCargoOverlay;
                        EnableShipIconOverlay = config.EnableShipIconOverlay;
                        AllowOverlayDrag = config.AllowOverlayDrag;
                        EnableSessionTracking = config.EnableSessionTracking;
                        ShowSessionOnOverlay = config.ShowSessionOnOverlay;
                        EnableHotkeys = config.EnableHotkeys;
                        StartMonitoringHotkey = config.StartMonitoringHotkey;
                        StopMonitoringHotkey = config.StopMonitoringHotkey;
                        ShowOverlayHotkey = config.ShowOverlayHotkey;
                        HideOverlayHotkey = config.HideOverlayHotkey;
                        FileReadMaxAttempts = config.FileReadMaxAttempts;
                        FileReadRetryDelayMs = config.FileReadRetryDelayMs;
                        WindowLocation = config.WindowLocation;
                        WindowState = config.WindowState;
                        DefaultFontSize = config.DefaultFontSize;
                        PollingIntervalMs = config.PollingIntervalMs;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[AppConfiguration] Error loading settings: {ex.Message}");
            }
        }

        public static void Save()
        {
            try
            {
                // Ensure the AppData directory exists before trying to save the file.
                if (!Directory.Exists(AppDataPath))
                {
                    Directory.CreateDirectory(AppDataPath);
                }

                var config = new ConfigData
                {
                    WelcomeMessage = AppConfiguration.WelcomeMessage,
                    OutputDirectory = AppConfiguration.OutputDirectory,
                    OutputFileName = AppConfiguration.OutputFileName,
                    CargoPath = AppConfiguration.CargoPath,
                    EnableFileOutput = AppConfiguration.EnableFileOutput,
                    OutputFileFormat = AppConfiguration.OutputFileFormat,
                    EnableInfoOverlay = AppConfiguration.EnableInfoOverlay,
                    EnableCargoOverlay = AppConfiguration.EnableCargoOverlay,
                    EnableShipIconOverlay = AppConfiguration.EnableShipIconOverlay,
                    AllowOverlayDrag = AppConfiguration.AllowOverlayDrag,
                    EnableSessionTracking = AppConfiguration.EnableSessionTracking,
                    ShowSessionOnOverlay = AppConfiguration.ShowSessionOnOverlay,
                    EnableHotkeys = AppConfiguration.EnableHotkeys,
                    StartMonitoringHotkey = AppConfiguration.StartMonitoringHotkey,
                    StopMonitoringHotkey = AppConfiguration.StopMonitoringHotkey,
                    ShowOverlayHotkey = AppConfiguration.ShowOverlayHotkey,
                    HideOverlayHotkey = AppConfiguration.HideOverlayHotkey,
                    FileReadMaxAttempts = AppConfiguration.FileReadMaxAttempts,
                    FileReadRetryDelayMs = AppConfiguration.FileReadRetryDelayMs,
                    WindowLocation = AppConfiguration.WindowLocation,
                    WindowState = AppConfiguration.WindowState,
                    DefaultFontSize = AppConfiguration.DefaultFontSize,
                    PollingIntervalMs = AppConfiguration.PollingIntervalMs,
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[AppConfiguration] Error saving settings: {ex.Message}");
            }
        }

        // A private class to hold the data for serialization
        private class ConfigData
        {
            public string WelcomeMessage { get; set; } = "Welcome, CMDR! Click 'Start' to begin monitoring.";
            public string OutputDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
            public string OutputFileName { get; set; } = "cargo.txt";
            public string CargoPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Saved Games\Frontier Developments\Elite Dangerous\Cargo.json");
            public bool EnableFileOutput { get; set; } = false;
            public string OutputFileFormat { get; set; } = "Cargo: {count_slash_capacity}\\n\\n{items_multiline}";
            public bool EnableInfoOverlay { get; set; } = false;
            public bool EnableCargoOverlay { get; set; } = false;
            public bool EnableShipIconOverlay { get; set; } = false;
            public bool AllowOverlayDrag { get; set; } = true;
            public bool EnableSessionTracking { get; set; } = true;
            public bool ShowSessionOnOverlay { get; set; } = false;
            public bool EnableHotkeys { get; set; } = true;
            public Keys StartMonitoringHotkey { get; set; } = Keys.Control | Keys.Alt | Keys.F9;
            public Keys StopMonitoringHotkey { get; set; } = Keys.Control | Keys.Alt | Keys.F10;
            public Keys ShowOverlayHotkey { get; set; } = Keys.Control | Keys.Alt | Keys.F11;
            public Keys HideOverlayHotkey { get; set; } = Keys.Control | Keys.Alt | Keys.F12;
            public int FileReadMaxAttempts { get; set; } = 5;
            public int FileReadRetryDelayMs { get; set; } = 50;
            public Point WindowLocation { get; set; } = Point.Empty;
            public FormWindowState WindowState { get; set; } = FormWindowState.Normal;
            public float DefaultFontSize { get; set; } = 9f;
            public int PollingIntervalMs { get; set; } = 1000;
        }
    }
}