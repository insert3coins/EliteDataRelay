using System.ComponentModel;

namespace EliteDataRelay.Configuration
{
    public static partial class AppConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether to show mining-related announcements in the UI.
        /// </summary>
        [DefaultValue(false)]
        public static bool EnableMiningAnnouncements { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to show a tray notification when cargo is full.
        /// </summary>
        [DefaultValue(false)]
        public static bool NotifyOnCargoFull { get; set; } = false;
    }
}