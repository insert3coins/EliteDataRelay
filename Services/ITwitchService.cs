using System;

namespace EliteDataRelay.Services
{
    public interface ITwitchService : IDisposable
    {
        event EventHandler<TwitchChatMessageEventArgs>? ChatMessageReceived;
        event EventHandler<TwitchFollowerEventArgs>? FollowerReceived;
        event EventHandler<TwitchRaidEventArgs>? RaidReceived;
        event EventHandler<TwitchSubscriptionEventArgs>? SubscriptionReceived;

        void Start();
        void Stop();
    }

    public class TwitchChatMessageEventArgs : EventArgs
    {
        public string Username { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
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