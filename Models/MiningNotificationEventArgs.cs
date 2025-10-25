using System;

namespace EliteDataRelay.Models
{
    public enum MiningNotificationType
    {
        Info,
        AutoStart,
        CargoFull,
        BackupCreated,
        BackupRestored,
        ReportGenerated,
        Reminder
    }

    public class MiningNotificationEventArgs : EventArgs
    {
        public MiningNotificationType Type { get; }
        public string Message { get; }
        public DateTime Timestamp { get; }
        public bool IsPersistent { get; }

        public MiningNotificationEventArgs(MiningNotificationType type, string message, DateTime timestamp, bool isPersistent)
        {
            Type = type;
            Message = message;
            Timestamp = timestamp;
            IsPersistent = isPersistent;
        }

        public override string ToString() => $"[{Timestamp:HH:mm}] {Message}";
    }
}
