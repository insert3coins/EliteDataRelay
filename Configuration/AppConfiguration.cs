using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace EliteDataRelay.Configuration

{
    public static partial class AppConfiguration
    {
        private static readonly string SettingsFilePath;
        // Opacity for browser-source overlays (0-100)
        public static int WebOverlayOpacity { get; set; } = 85;

        #region Properties

        // General

        #endregion

        static AppConfiguration()
        {
            // The static constructor ensures that all dependent properties are initialized
            // in the correct order, resolving the CS8604 warning.
            SettingsFilePath = Path.Combine(AppDataPath, "settings.json");
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
                    var options = new JsonSerializerOptions();
                    options.Converters.Add(new ColorJsonConverter());

                    var config = JsonSerializer.Deserialize<ConfigData>(json, options);

                    if (config != null)
                    {
                        // Map all properties from the loaded config data
                        WelcomeMessage = config.WelcomeMessage;
                        // Legacy text file output removed
                        CargoPath = config.CargoPath;
                        // Legacy text file output removed
                        EnableInfoOverlay = config.EnableInfoOverlay;
                        EnableCargoOverlay = config.EnableCargoOverlay;
                        EnableShipIconOverlay = config.EnableShipIconOverlay;
                        EnableExplorationOverlay = config.EnableExplorationOverlay;
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
                        OverlayTextColor = config.OverlayTextColor;
                        OverlayBackgroundColor = config.OverlayBackgroundColor;
                        OverlayBorderColor = config.OverlayBorderColor;
                        InfoOverlayLocation = config.InfoOverlayLocation;
                        CargoOverlayLocation = config.CargoOverlayLocation;
                        ShipIconOverlayLocation = config.ShipIconOverlayLocation;
                        ExplorationOverlayLocation = config.ExplorationOverlayLocation;
                        // Mining announcements removed
                        // OBS compatibility removed

                    // Performance and extras
                    FastStartSkipJournalHistory = config.FastStartSkipJournalHistory;
                    VerboseLogging = config.VerboseLogging;

                    // Exploration
                    ExplorationHistoryImported = config.ExplorationHistoryImported;

                        // Screenshot renamer
                        EnableScreenshotRenamer = config.EnableScreenshotRenamer;
                        ScreenshotRenameFormat = config.ScreenshotRenameFormat;

                        // Webhook removed

                    // Web overlay
                    EnableWebOverlayServer = config.EnableWebOverlayServer;
                    WebOverlayPort = config.WebOverlayPort;
                    WebOverlayOpacity = config.WebOverlayOpacity;

                    // Localization
                    UICulture = config.UICulture ?? string.Empty;
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
                    // Legacy text file output removed
                    CargoPath = AppConfiguration.CargoPath,
                    // Legacy text file output removed
                    EnableInfoOverlay = AppConfiguration.EnableInfoOverlay,
                    EnableCargoOverlay = AppConfiguration.EnableCargoOverlay,
                    EnableShipIconOverlay = AppConfiguration.EnableShipIconOverlay,
                    EnableExplorationOverlay = AppConfiguration.EnableExplorationOverlay,
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
                    OverlayTextColor = AppConfiguration.OverlayTextColor,
                    OverlayBackgroundColor = AppConfiguration.OverlayBackgroundColor,
                    OverlayBorderColor = AppConfiguration.OverlayBorderColor,
                    InfoOverlayLocation = AppConfiguration.InfoOverlayLocation,
                    CargoOverlayLocation = AppConfiguration.CargoOverlayLocation,
                    ShipIconOverlayLocation = AppConfiguration.ShipIconOverlayLocation,
                    ExplorationOverlayLocation = AppConfiguration.ExplorationOverlayLocation,
                    
     

                    // Performance and extras
                    FastStartSkipJournalHistory = AppConfiguration.FastStartSkipJournalHistory,
                    VerboseLogging = AppConfiguration.VerboseLogging,

                    // Exploration
                    ExplorationHistoryImported = AppConfiguration.ExplorationHistoryImported,

                    // Screenshot renamer
                    EnableScreenshotRenamer = AppConfiguration.EnableScreenshotRenamer,
                    ScreenshotRenameFormat = AppConfiguration.ScreenshotRenameFormat,

                    // Webhook removed

                    // Web overlay
                    EnableWebOverlayServer = AppConfiguration.EnableWebOverlayServer,
                    WebOverlayPort = AppConfiguration.WebOverlayPort,
                    WebOverlayOpacity = AppConfiguration.WebOverlayOpacity,

                    // Localization
                    UICulture = AppConfiguration.UICulture,
                };

                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                };
                options.Converters.Add(new ColorJsonConverter());
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
            // Legacy text file output removed
            public string CargoPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Saved Games\Frontier Developments\Elite Dangerous\Cargo.json");
            // Legacy text file output removed
            public bool EnableInfoOverlay { get; set; } = false;
            public bool EnableCargoOverlay { get; set; } = false;
            public bool EnableShipIconOverlay { get; set; } = false;
            public bool EnableExplorationOverlay { get; set; } = false;
            public bool AllowOverlayDrag { get; set; } = true;
            public bool EnableSessionTracking { get; set; } = true;
            public bool ShowSessionOnOverlay { get; set; } = false;
            public bool EnableHotkeys { get; set; } = true;
            public Keys StartMonitoringHotkey { get; set; } = Keys.Control | Keys.Alt | Keys.F9;
            public Keys StopMonitoringHotkey { get; set; } = Keys.Control | Keys.Alt | Keys.F10;
            public Keys ShowOverlayHotkey { get; set; } = Keys.Control | Keys.Alt | Keys.F11;
            public Keys HideOverlayHotkey { get; set; } = Keys.Control | Keys.Alt | Keys.F12;
            public int FileReadMaxAttempts { get; set; } = 3; // fewer retries for lower latency
            public int FileReadRetryDelayMs { get; set; } = 20; // shorter delay between retries
            public Point WindowLocation { get; set; } = Point.Empty;
            public FormWindowState WindowState { get; set; } = FormWindowState.Normal;
            public float DefaultFontSize { get; set; } = 9f;
            public int PollingIntervalMs { get; set; } = 1000;
            [JsonConverter(typeof(ColorJsonConverter))]
            public Color OverlayTextColor { get; set; } = Color.FromArgb(255, 128, 0);
            [JsonConverter(typeof(ColorJsonConverter))]
            public Color OverlayBackgroundColor { get; set; } = Color.FromArgb(0, 0, 0);
            [JsonConverter(typeof(ColorJsonConverter))]
            public Color OverlayBorderColor { get; set; } = Color.FromArgb(255, 111, 0);
            public Point InfoOverlayLocation { get; set; } = Point.Empty;
            public Point CargoOverlayLocation { get; set; } = Point.Empty;
            public Point ShipIconOverlayLocation { get; set; } = Point.Empty;
            public Point ExplorationOverlayLocation { get; set; } = new Point(20, 20);
            // Mining announcements removed
            // OBS compatibility removed

            // Performance and extras
            public bool FastStartSkipJournalHistory { get; set; } = true;
            public bool VerboseLogging { get; set; } = false;

            // Screenshot renamer
            public bool EnableScreenshotRenamer { get; set; } = false;
            public string ScreenshotRenameFormat { get; set; } = "{System} - {Body} - {Timestamp}";

            // Webhook removed

            // Web overlay
            public bool EnableWebOverlayServer { get; set; } = false;
            public int WebOverlayPort { get; set; } = 9005;
            public int WebOverlayOpacity { get; set; } = 85;

            // Exploration
            public bool ExplorationHistoryImported { get; set; } = false;

            // Localization
            public string? UICulture { get; set; } = string.Empty;
        }
    }
}


