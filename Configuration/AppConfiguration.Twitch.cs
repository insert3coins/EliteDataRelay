using System.Drawing;

namespace EliteDataRelay.Configuration
{
    public static partial class AppConfiguration
    {
        // Twitch Settings
        public static bool EnableTwitchIntegration { get => _settings.EnableTwitchIntegration; set => _settings.EnableTwitchIntegration = value; }
        public static bool EnableTwitchChatOverlay { get => _settings.EnableTwitchChatOverlay; set => _settings.EnableTwitchChatOverlay = value; }
        public static bool EnableTwitchFollowerAlerts { get => _settings.EnableTwitchFollowerAlerts; set => _settings.EnableTwitchFollowerAlerts = value; }
        public static bool EnableTwitchRaidAlerts { get => _settings.EnableTwitchRaidAlerts; set => _settings.EnableTwitchRaidAlerts = value; }
        public static bool EnableTwitchSubAlerts { get => _settings.EnableTwitchSubAlerts; set => _settings.EnableTwitchSubAlerts = value; }
        public static string TwitchChannelName { get => _settings.TwitchChannelName; set => _settings.TwitchChannelName = value; }
        public static string TwitchUsername { get => _settings.TwitchUsername; set => _settings.TwitchUsername = value; }
        public static string TwitchOAuthToken { get => _settings.TwitchOAuthToken; set => _settings.TwitchOAuthToken = value; }
        public static string TwitchRefreshToken { get => _settings.TwitchRefreshToken; set => _settings.TwitchRefreshToken = value; }
        public static string TwitchClientSecret { get => _settings.TwitchClientSecret; set => _settings.TwitchClientSecret = value; }
        public static string TwitchClientId { get => _settings.TwitchClientId; set => _settings.TwitchClientId = value; }
        public static Point TwitchChatOverlayLocation { get => _settings.TwitchChatOverlayLocation; set => _settings.TwitchChatOverlayLocation = value; }

        private partial class AppSettings
        {
            public bool EnableTwitchIntegration { get; set; } = true;
            public bool EnableTwitchChatOverlay { get; set; } = true;
            public bool EnableTwitchFollowerAlerts { get; set; } = true;
            public bool EnableTwitchRaidAlerts { get; set; } = true;
            public bool EnableTwitchSubAlerts { get; set; } = true;
            public string TwitchChannelName { get; set; } = string.Empty;
            public string TwitchUsername { get; set; } = string.Empty;
            public string TwitchOAuthToken { get; set; } = string.Empty;
            public string TwitchRefreshToken { get; set; } = string.Empty;
            public string TwitchClientSecret { get; set; } = string.Empty;
            public string TwitchClientId { get; set; } = "9wz2k52qb5dp4w3v42emp66z2l0y2p"; // Default to a known working public ID
            public Point TwitchChatOverlayLocation { get; set; } = Point.Empty;
        }
    }
}