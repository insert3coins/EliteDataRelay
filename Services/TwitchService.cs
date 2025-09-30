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

            // --- Initialize API for follower polling ---
            _api = new TwitchAPI();
            _api.Settings.ClientId = "9wz2k52qb5dp4w3v42emp66z2l0y2p"; // Using the same public client ID as auth
            _api.Settings.AccessToken = apiOauth;

            _pollingService = new TwitchApiPollingService(_api);
            _pollingService.NewFollowerDetected += OnNewFollowerDetected;
            _ = _pollingService.Start(channel, apiOauth);

            // --- Initialize Client for chat and other events ---
            var credentials = new ConnectionCredentials(username, oauth);
            var clientOptions = new ClientOptions { MessagesAllowedInPeriod = 750, ThrottlingPeriod = TimeSpan.FromSeconds(30) };
            var customClient = new WebSocketClient(clientOptions);
            _client = new TwitchClient(customClient);
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
        }

        #region Event Handlers

        private void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            ChatMessageReceived?.Invoke(this, new TwitchChatMessageEventArgs
            {
                Username = e.ChatMessage.DisplayName,
                Message = e.ChatMessage.Message
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