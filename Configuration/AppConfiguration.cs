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
                        EnableExplorationOverlay = config.EnableExplorationOverlay;
                        EnableSessionOverlay = config.EnableSessionOverlay || config.ShowSessionOnOverlay;
                        EnableMiningOverlay = config.EnableMiningOverlay;
                        EnableProspectorOverlay = config.EnableProspectorOverlay;
                        EnableJumpOverlay = config.EnableJumpOverlay;
                        AllowOverlayDrag = config.AllowOverlayDrag;
                        EnableSessionTracking = config.EnableSessionTracking;
                        // Hotkeys removed
                        FileReadMaxAttempts = config.FileReadMaxAttempts;
                        FileReadRetryDelayMs = config.FileReadRetryDelayMs;
                        WindowLocation = config.WindowLocation;
                        WindowState = config.WindowState;
                        DefaultFontSize = config.DefaultFontSize;
                        PollingIntervalMs = config.PollingIntervalMs;
                        OverlayTextColor = config.OverlayTextColor;
                        OverlayBackgroundColor = config.OverlayBackgroundColor;
                        OverlayBorderColor = config.OverlayBorderColor;
                        OverlayOpacity = config.OverlayOpacity;
                        OverlayShowBorder = config.OverlayShowBorder;
                        OverlayShowBorderInfo = config.OverlayShowBorderInfo;
                        OverlayShowBorderCargo = config.OverlayShowBorderCargo;
                        OverlayShowBorderSession = config.OverlayShowBorderSession;
                        OverlayShowBorderExploration = config.OverlayShowBorderExploration;
                        OverlayShowBorderMining = config.OverlayShowBorderMining;
                        OverlayShowBorderProspector = config.OverlayShowBorderProspector;
                        OverlayShowBorderJump = config.OverlayShowBorderJump;
                        ShowTrafficOnExplorationOverlay = config.ShowTrafficOnExplorationOverlay;
                        ShowTrafficOnJumpOverlay = config.ShowTrafficOnJumpOverlay;

                        // Migration: if legacy global border is explicitly false and all per-overlay toggles are still defaults,
                        // apply the global value to each overlay.
                        if (config.OverlayShowBorder == false &&
                            config.OverlayShowBorderInfo == true &&
                            config.OverlayShowBorderCargo == true &&
                            config.OverlayShowBorderExploration == true &&
                            config.OverlayShowBorderMining == true &&
                            config.OverlayShowBorderProspector == true &&
                            config.OverlayShowBorderJump == true)
                        {
                            OverlayShowBorderInfo = false;
                            OverlayShowBorderCargo = false;
                            OverlayShowBorderExploration = false;
                            OverlayShowBorderMining = false;
                            OverlayShowBorderProspector = false;
                            OverlayShowBorderJump = false;
                        }
                        InfoOverlayLocation = config.InfoOverlayLocation;
                        CargoOverlayLocation = config.CargoOverlayLocation;
                        SessionOverlayLocation = config.SessionOverlayLocation;
                        ExplorationOverlayLocation = config.ExplorationOverlayLocation;
                        MiningOverlayLocation = config.MiningOverlayLocation;
                        ProspectorOverlayLocation = config.ProspectorOverlayLocation;
                        JumpOverlayLocation = config.JumpOverlayLocation;
                        ShowNextJumpJumpsLeft = config.ShowNextJumpJumpsLeft;
                        // Mining announcements removed
                        // OBS compatibility removed

                    // Performance and extras
                    FastStartSkipJournalHistory = config.FastStartSkipJournalHistory;

                    // Exploration
                    ExplorationHistoryImported = config.ExplorationHistoryImported;

                        // Screenshot renamer
                        EnableScreenshotRenamer = config.EnableScreenshotRenamer;
                        ScreenshotRenameFormat = config.ScreenshotRenameFormat;

                        // EDSM upload
                        EdsmCommanderName = config.EdsmCommanderName ?? string.Empty;
                        EdsmApiKey = config.EdsmApiKey ?? string.Empty;

                        // Webhook removed

                        // Web overlay
                        
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
                    EnableExplorationOverlay = AppConfiguration.EnableExplorationOverlay,
                    EnableMiningOverlay = AppConfiguration.EnableMiningOverlay,
                    EnableProspectorOverlay = AppConfiguration.EnableProspectorOverlay,
                    EnableJumpOverlay = AppConfiguration.EnableJumpOverlay,
                    AllowOverlayDrag = AppConfiguration.AllowOverlayDrag,
                    EnableSessionTracking = AppConfiguration.EnableSessionTracking,
                    EnableSessionOverlay = AppConfiguration.EnableSessionOverlay,
                    ShowSessionOnOverlay = AppConfiguration.EnableSessionOverlay,
                    // Hotkeys removed
                    FileReadMaxAttempts = AppConfiguration.FileReadMaxAttempts,
                    FileReadRetryDelayMs = AppConfiguration.FileReadRetryDelayMs,
                    WindowLocation = AppConfiguration.WindowLocation,
                    WindowState = AppConfiguration.WindowState,
                    DefaultFontSize = AppConfiguration.DefaultFontSize,
                    PollingIntervalMs = AppConfiguration.PollingIntervalMs,
                    OverlayTextColor = AppConfiguration.OverlayTextColor,
                    OverlayBackgroundColor = AppConfiguration.OverlayBackgroundColor,
                    OverlayBorderColor = AppConfiguration.OverlayBorderColor,
                    OverlayShowBorder = AppConfiguration.OverlayShowBorder,
                    OverlayShowBorderInfo = AppConfiguration.OverlayShowBorderInfo,
                    OverlayShowBorderCargo = AppConfiguration.OverlayShowBorderCargo,
                    OverlayShowBorderSession = AppConfiguration.OverlayShowBorderSession,
                    OverlayShowBorderExploration = AppConfiguration.OverlayShowBorderExploration,
                    OverlayShowBorderMining = AppConfiguration.OverlayShowBorderMining,
                    OverlayShowBorderProspector = AppConfiguration.OverlayShowBorderProspector,
                    OverlayShowBorderJump = AppConfiguration.OverlayShowBorderJump,
                    ShowTrafficOnExplorationOverlay = AppConfiguration.ShowTrafficOnExplorationOverlay,
                    ShowTrafficOnJumpOverlay = AppConfiguration.ShowTrafficOnJumpOverlay,
                    InfoOverlayLocation = AppConfiguration.InfoOverlayLocation,
                    CargoOverlayLocation = AppConfiguration.CargoOverlayLocation,
                    SessionOverlayLocation = AppConfiguration.SessionOverlayLocation,
                    ExplorationOverlayLocation = AppConfiguration.ExplorationOverlayLocation,
                    MiningOverlayLocation = AppConfiguration.MiningOverlayLocation,
                    ProspectorOverlayLocation = AppConfiguration.ProspectorOverlayLocation,
                    JumpOverlayLocation = AppConfiguration.JumpOverlayLocation,
                    ShowNextJumpJumpsLeft = AppConfiguration.ShowNextJumpJumpsLeft,
                    // Performance and extras
                    FastStartSkipJournalHistory = AppConfiguration.FastStartSkipJournalHistory,

                    // Exploration
                    ExplorationHistoryImported = AppConfiguration.ExplorationHistoryImported,

                    // Screenshot renamer
                    EnableScreenshotRenamer = AppConfiguration.EnableScreenshotRenamer,
                    ScreenshotRenameFormat = AppConfiguration.ScreenshotRenameFormat,

                    // EDSM upload
                    EdsmCommanderName = AppConfiguration.EdsmCommanderName,
                    EdsmApiKey = AppConfiguration.EdsmApiKey,

                    // Webhook removed

                    // Web overlay
                    
                    OverlayOpacity = AppConfiguration.OverlayOpacity,
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
            public bool EnableSessionOverlay { get; set; } = false;
            public bool EnableExplorationOverlay { get; set; } = false;
            public bool EnableMiningOverlay { get; set; } = false;
            public bool EnableProspectorOverlay { get; set; } = false;
            public bool EnableJumpOverlay { get; set; } = true;
            public bool AllowOverlayDrag { get; set; } = true;
            public bool EnableSessionTracking { get; set; } = true;
            public bool ShowSessionOnOverlay { get; set; } = false;
            // Hotkeys removed
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
            public int OverlayOpacity { get; set; } = 85;
            public bool OverlayShowBorder { get; set; } = true;
            public bool OverlayShowBorderInfo { get; set; } = true;
            public bool OverlayShowBorderCargo { get; set; } = true;
            public bool OverlayShowBorderSession { get; set; } = true;
            public bool OverlayShowBorderExploration { get; set; } = true;
            public bool OverlayShowBorderMining { get; set; } = true;
            public bool OverlayShowBorderProspector { get; set; } = true;
            public bool OverlayShowBorderJump { get; set; } = true;
            public bool ShowTrafficOnExplorationOverlay { get; set; } = true;
            public bool ShowTrafficOnJumpOverlay { get; set; } = true;
            public Point InfoOverlayLocation { get; set; } = Point.Empty;
            public Point CargoOverlayLocation { get; set; } = Point.Empty;
            public Point SessionOverlayLocation { get; set; } = Point.Empty;
            public Point ExplorationOverlayLocation { get; set; } = new Point(20, 20);
            public Point MiningOverlayLocation { get; set; } = Point.Empty;
            public Point ProspectorOverlayLocation { get; set; } = Point.Empty;
            public Point JumpOverlayLocation { get; set; } = Point.Empty;
            public bool ShowNextJumpJumpsLeft { get; set; } = true;
            // Mining announcements removed
            // OBS compatibility removed

            // Performance and extras
            public bool FastStartSkipJournalHistory { get; set; } = true;

            // Screenshot renamer
            public bool EnableScreenshotRenamer { get; set; } = false;
            public string ScreenshotRenameFormat { get; set; } = "{System} - {Body} - {Timestamp}";

            // EDSM upload
            public string EdsmCommanderName { get; set; } = string.Empty;
            public string EdsmApiKey { get; set; } = string.Empty;

            // Webhook removed

            // Web overlay
            

            // Exploration
            public bool ExplorationHistoryImported { get; set; } = false;
        }
    }
}
