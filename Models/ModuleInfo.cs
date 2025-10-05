using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    public class ModuleInfo
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Mount { get; set; }
        public int Class { get; set; }
        public string Rating { get; set; } = string.Empty;
    }
}