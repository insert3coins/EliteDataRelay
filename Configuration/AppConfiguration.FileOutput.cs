namespace EliteDataRelay.Configuration
{
    public static partial class AppConfiguration
    {
        // File Output Settings
        public static bool EnableFileOutput { get => _settings.EnableFileOutput; set => _settings.EnableFileOutput = value; }
        public static string OutputFileFormat { get => _settings.OutputFileFormat; set => _settings.OutputFileFormat = value; }
        public static string OutputFileName { get => _settings.OutputFileName; set => _settings.OutputFileName = value; }
        public static string OutputDirectory { get => _settings.OutputDirectory; set => _settings.OutputDirectory = value; }

        private partial class AppSettings
        {
            public bool EnableFileOutput { get; set; } = false;
            public string OutputFileFormat { get; set; } = "{name} - {count}";
            public string OutputFileName { get; set; } = "cargo.txt";
            public string OutputDirectory { get; set; } = string.Empty;
        }
    }
}