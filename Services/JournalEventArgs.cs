using System;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Provides the raw journal line and event name for forwarding to external services (e.g., EDSM).
    /// </summary>
    public class JournalEventArgs : EventArgs
    {
        public JournalEventArgs(string eventName, string rawLine)
        {
            EventName = eventName;
            RawLine = rawLine;
        }

        public string EventName { get; }
        public string RawLine { get; }
    }
}
