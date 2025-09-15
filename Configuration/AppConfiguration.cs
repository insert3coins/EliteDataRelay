using System;
using System.IO;

namespace EliteCargoMonitor.Configuration
{
    /// <summary>
    /// Static class to hold application configuration values.
    /// In a real application, these might be loaded from a config file.
    /// </summary>
    public static class AppConfiguration
    {
        private static readonly string UserSavedGames = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games", "Frontier Developments", "Elite Dangerous");

        /// <summary>
        /// The path to the Cargo.json file.
        /// </summary>
        public static string CargoPath { get; } = Path.Combine(UserSavedGames, "Cargo.json");

        /// <summary>
        /// The path to the Elite Dangerous Player Journal directory.
        /// If not specified (is null or empty), the application will default to the standard journal location within the user's "Saved Games" folder.
        /// This can be overridden if the journal files are in a custom location.
        /// </summary>
        public static string JournalPath { get; set; } = UserSavedGames;

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
        /// About text for the application.
        /// </summary>
        public static string AboutText { get; } = "Elite Cargo Monitor v1.1\nCreated by insert3coins";

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
        public static string OutputFileName { get; } = "cargo.txt";

        // UI Configuration
        public static int FormWidth { get; } = 800;
        public static int FormHeight { get; } = 600;
        public static int ButtonHeight { get; } = 30;
        public static int ButtonPanelHeight { get; } = 40;
        public static float DefaultFontSize { get; } = 9.0f;
        public static string VerdanaFontName { get; } = "Verdana";
        public static string ConsolasFontName { get; } = "Consolas";
        public static int MaxTextBoxLines { get; } = 1000;
    }
}