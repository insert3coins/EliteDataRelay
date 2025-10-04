using System.IO;

namespace EliteDataRelay.Configuration
{
    public static partial class AppConfiguration
    {
        /// <summary>
        /// Gets the full path to the Materials.json file.
        /// </summary>
        public static string MaterialsPath => Path.Combine(JournalPath, "Materials.json");
    }
}