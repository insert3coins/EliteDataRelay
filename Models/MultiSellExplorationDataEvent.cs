using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models

{
    /// <summary>
    /// Represents the structure of a MultiSellExplorationData journal event for deserialization.
    /// This event occurs when selling on-foot genetic data.
    /// </summary>
    public class MultiSellExplorationDataEvent
    {
        [JsonPropertyName("TotalEarnings")]
        public long TotalEarnings { get; set; }

        [JsonPropertyName("FirstFootfallCount")]
        public int FirstFootfallCount { get; set; }
    }
}