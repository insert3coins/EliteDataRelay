using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    public class MiningSessionRecord
    {
        public DateTime SessionStart { get; set; }
        public DateTime SessionEnd { get; set; }
        public double SessionDurationSeconds { get; set; }
        public double MiningDurationSeconds { get; set; }
        public int LimpetsUsed { get; set; }
        public long CreditsEarned { get; set; }
        public long TotalCargoCollected { get; set; }
        public Dictionary<string, int> RefinedCommodities { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> CollectedCommodities { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public double FinalCargoFillPercent { get; set; }
        public bool CargoHoldFullAtEnd { get; set; }
        public string Notes { get; set; } = string.Empty;

        [JsonIgnore]
        public TimeSpan SessionDuration => TimeSpan.FromSeconds(SessionDurationSeconds);

        [JsonIgnore]
        public TimeSpan MiningDuration => TimeSpan.FromSeconds(MiningDurationSeconds);

        public MiningSessionRecord Clone() => new()
        {
            SessionStart = SessionStart,
            SessionEnd = SessionEnd,
            SessionDurationSeconds = SessionDurationSeconds,
            MiningDurationSeconds = MiningDurationSeconds,
            LimpetsUsed = LimpetsUsed,
            CreditsEarned = CreditsEarned,
            TotalCargoCollected = TotalCargoCollected,
            RefinedCommodities = new Dictionary<string, int>(RefinedCommodities, StringComparer.OrdinalIgnoreCase),
            CollectedCommodities = new Dictionary<string, int>(CollectedCommodities, StringComparer.OrdinalIgnoreCase),
            FinalCargoFillPercent = FinalCargoFillPercent,
            CargoHoldFullAtEnd = CargoHoldFullAtEnd,
            Notes = Notes
        };
    }
}

