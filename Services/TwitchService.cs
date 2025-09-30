using EliteDataRelay.Configuration;
using System;
using System.Diagnostics;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Real implementation of ITwitchService using TwitchLib to connect to Twitch chat and APIs.
    /// </summary>
    public class TwitchService : ITwitchService
    {
        public event EventHandler<TwitchChatMessageEventArgs>? ChatMessageReceived;
        public event EventHandler<TwitchFollowerEventArgs>? FollowerReceived;
        public event EventHandler<TwitchRaidEventArgs>? RaidReceived;
        public event EventHandler<TwitchSubscriptionEventArgs>? SubscriptionReceived;

        private TwitchClient? _client;
        private TwitchAPI? _api;
        private TwitchApiPollingService? _pollingService;

        public TwitchAPI? ApiClient => _api;
        public string? ChannelId { get; private set; }

        public void Start()
        {
            var username = AppConfiguration.TwitchUsername;
            var channel = AppConfiguration.TwitchChannelName;
            var oauth = AppConfiguration.TwitchOAuthToken;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(channel) || string.IsNullOrEmpty(oauth))
            {
                Debug.WriteLine("[TwitchService] Cannot start. Twitch username, channel, or OAuth token is missing.");
                return;
            }

            // The token from our auth flow includes "oauth:", which we need for chat.
            // The API needs the token without the prefix.
            var apiOauth = oauth.Replace("oauth:", "");

            // --- Initialize API and Polling Service ---
            _api = new TwitchAPI();
            _api.Settings.ClientId = AppConfiguration.TwitchClientId;
            _api.Settings.AccessToken = apiOauth;

            _pollingService = new TwitchApiPollingService(_api);
            _pollingService.NewFollowerDetected += OnNewFollowerDetected;
            _pollingService.TokenExpired += OnTokenExpired;
            _pollingService.Start(channel, apiOauth).ContinueWith(t => { ChannelId = _pollingService.ChannelId; });

            // --- Initialize Client for chat and other events ---
            var credentials = new ConnectionCredentials(username, oauth);
            var clientOptions = new ClientOptions { MessagesAllowedInPeriod = 750, ThrottlingPeriod = TimeSpan.FromSeconds(30) };
            var customClient = new WebSocketClient(clientOptions);
            _client = new TwitchClient(customClient);
            _client.OnConnected += (s, e) => _client.SendRaw("CAP REQ :twitch.tv/tags"); // Request tags for badge info
            _client.Initialize(credentials, channel);

            // Subscribe to events
            _client.OnMessageReceived += OnMessageReceived;
            _client.OnNewSubscriber += OnNewSubscriber;
            _client.OnReSubscriber += OnReSubscriber;
            _client.OnGiftedSubscription += OnGiftedSubscription;
            _client.OnRaidNotification += OnRaidNotification;
            _client.OnConnected += (s, e) => Debug.WriteLine($"[TwitchService] Connected to Twitch chat: {e.BotUsername}");
            _client.OnError += (s, e) => Debug.WriteLine($"[TwitchService] Error: {e.Exception}");

            _client.Connect();
        }

        public void Stop()
        {
            _pollingService?.Stop();
            _client?.Disconnect();
            _client = null; // Ensure the client is fully released
        }

        #region Event Handlers

        private void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            ChatMessageReceived?.Invoke(this, new TwitchChatMessageEventArgs
            {
                Username = e.ChatMessage.DisplayName,
                Message = e.ChatMessage.Message,
                Badges = e.ChatMessage.Badges,
                BadgeInfo = e.ChatMessage.BadgeInfo
            });
        }

        private void OnNewSubscriber(object? sender, OnNewSubscriberArgs e)
        {
            SubscriptionReceived?.Invoke(this, new TwitchSubscriptionEventArgs
            {
                Username = e.Subscriber.DisplayName,
                Tier = e.Subscriber.SubscriptionPlan.ToString().Replace("Tier", "Tier "),
                IsGift = false
            });
        }

        private void OnReSubscriber(object? sender, OnReSubscriberArgs e)
        {
            SubscriptionReceived?.Invoke(this, new TwitchSubscriptionEventArgs
            {
                Username = e.ReSubscriber.DisplayName,
                Tier = e.ReSubscriber.SubscriptionPlan.ToString().Replace("Tier", "Tier "),
                IsGift = false
            });
        }

        private void OnGiftedSubscription(object? sender, OnGiftedSubscriptionArgs e)
        {
            SubscriptionReceived?.Invoke(this, new TwitchSubscriptionEventArgs
            {
                Username = e.GiftedSubscription.DisplayName,
                Tier = e.GiftedSubscription.MsgParamSubPlan.ToString().Replace("Tier", "Tier "),
                IsGift = true
            });
        }

        private void OnRaidNotification(object? sender, OnRaidNotificationArgs e)
        {
            if (int.TryParse(e.RaidNotification.MsgParamViewerCount, out int viewerCount))
            {
                RaidReceived?.Invoke(this, new TwitchRaidEventArgs
                {
                    Username = e.RaidNotification.DisplayName,
                    ViewerCount = viewerCount
                });
            }
        }

        private void OnNewFollowerDetected(object? sender, string username)
        {
            FollowerReceived?.Invoke(this, new TwitchFollowerEventArgs { Username = username });
        }

        private async void OnTokenExpired(object? sender, EventArgs e)
        {
            Debug.WriteLine("[TwitchService] Token expired. Attempting to refresh and reconnect...");
            Stop(); // Stop all current connections

            bool success = await TwitchAuthService.RefreshTokenAsync();
            if (success)
            {
                Debug.WriteLine("[TwitchService] Token refresh successful. Restarting service...");
                Start(); // Restart with the new token
            }
            else
            {
                Debug.WriteLine("[TwitchService] Token refresh failed. User will need to log in again.");
                // Optionally, you could raise an event here to notify the main UI to update the login status.
            }
        }

        #endregion

        public void Dispose()
        {
            Stop();
            _pollingService?.Dispose();
            // No need to dispose _client, Disconnect handles it.
            GC.SuppressFinalize(this);
        }
    }
}