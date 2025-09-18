namespace EliteDataRelay.Models
{
    /// <summary>
    /// Represents user-configurable settings that are persisted to settings.json.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// The format string for the cargo.txt output file.
        /// </summary>
        public string? OutputFileFormat { get; set; }

        /// <summary>
        /// The file name for the cargo text file.
        /// </summary>
        public string? OutputFileName { get; set; }

        /// <summary>
        /// The output directory for the cargo text file.
        /// </summary>
        public string? OutputDirectory { get; set; }
    }
}