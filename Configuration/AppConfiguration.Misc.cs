using System;
using System.IO;

namespace EliteDataRelay.Configuration
{
    public static partial class AppConfiguration
    {
        // General
        public static string AppDataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EliteDataRelay");
        public static string WelcomeMessage { get; set; } = "Welcome, CMDR! Click 'Start' to begin monitoring.";

        // File Paths
        public static string JournalPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Saved Games\Frontier Developments\Elite Dangerous");
        public static string CargoPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Saved Games\Frontier Developments\Elite Dangerous\Cargo.json");

        // Session Tracking
        public static bool EnableSessionTracking { get; set; } = true;

        // Advanced
        public static int FileReadMaxAttempts { get; set; } = 5;
        public static int FileReadRetryDelayMs { get; set; } = 30; // Reduced from 50ms for faster in-game response
        public static int PollingIntervalMs { get; set; } = 1000;

        // Sound
        public static bool PlaySounds { get; set; } = true;
        public static string StartSoundPath { get; set; }
        public static string StopSoundPath { get; set; }
    }
}