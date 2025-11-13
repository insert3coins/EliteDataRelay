using System;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Snapshot of the current session metrics used by overlays and UI.
    /// </summary>
    public class SessionOverlayData
    {
        public long CargoCollected { get; set; }
        public long CreditsEarned { get; set; }
        public TimeSpan SessionDuration { get; set; }
        public int SystemsVisited { get; set; }
    }
}
