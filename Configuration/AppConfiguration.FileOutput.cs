using System.IO;

namespace EliteDataRelay.Configuration
{
    public static partial class AppConfiguration
    {
        public static bool EnableFileOutput { get; set; } = false;
        public static string OutputFileFormat { get; set; } = "Cargo: {count_slash_capacity}\\n\\n{items_multiline}";
        public static string OutputFileName { get; set; } = "cargo.txt";
        public static string OutputDirectory { get; set; }
    }
}