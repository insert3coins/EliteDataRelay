using System;
using System.IO;

namespace EliteCargoMonitor.Configuration
{
    /// <summary>
    /// Central configuration class containing all application constants and settings
    /// </summary>
    public static class AppConfiguration
    {
        // File paths
        public static readonly string CargoPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Saved Games",
            "Frontier Developments",
            "Elite Dangerous",
            "Cargo.json");

        public const string OutputDirectory = "out";
        public const string OutputFileName = "cargo.txt";
        
        // Timing configurations
        public const int DebounceDelayMs = 500;
        public const int FileReadRetryDelayMs = 100;
        public const int FileReadMaxAttempts = 10;
        public const int PollingIntervalMs = 1000;
        public const int ThreadRetryDelayMs = 20;
        public const int ThreadMaxRetries = 10;
        public const int FileSystemDelayMs = 50;

        // UI configurations  
        public const int MaxTextBoxLines = 100;
        public const int FormWidth = 800;
        public const int FormHeight = 600;
        public const int ButtonHeight = 30;
        public const int ButtonPanelHeight = 35;

        // Application metadata
        public const string AboutText = "Made by insert3coins. Version 0.5a";
        public static string WelcomeMessage = "Welcome to Cargo Watcher" + Environment.NewLine +
                                             "Please press Start to start watching cargo" + Environment.NewLine +
                                             "Press Stop to stop watching" + Environment.NewLine +
                                             "Exit to shutdown program.";
        // Font configurations
        public const string ConsolasFontName = "Consolas";
        public const string VerdanaFontName = "Verdana";
        public const float DefaultFontSize = 10f;
    }
}