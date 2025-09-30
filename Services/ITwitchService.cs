using System;
using TwitchLib.Api;

namespace EliteDataRelay.Services
{
    public interface ITwitchService : IDisposable
    {
        event EventHandler<TwitchChatMessageEventArgs>? ChatMessageReceived;
        event EventHandler<TwitchFollowerEventArgs>? FollowerReceived;
        event EventHandler<TwitchRaidEventArgs>? RaidReceived;
        event EventHandler<TwitchSubscriptionEventArgs>? SubscriptionReceived;

        TwitchAPI? ApiClient { get; }
        string? ChannelId { get; }

        void Start();
        void Stop();
    }

    public class TwitchChatMessageEventArgs : EventArgs
    {
        public string Username { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>> Badges { get; set; } = new();
        public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>> BadgeInfo { get; set; } = new();
    }

    public class TwitchFollowerEventArgs : EventArgs { public string Username { get; set; } = string.Empty; }

    public class TwitchRaidEventArgs : EventArgs { public string Username { get; set; } = string.Empty; public int ViewerCount { get; set; } }

    public class TwitchSubscriptionEventArgs : EventArgs
    {
        public string Username { get; set; } = string.Empty;
        public string Tier { get; set; } = "Tier 1";
        public bool IsGift { get; set; }
    }
}