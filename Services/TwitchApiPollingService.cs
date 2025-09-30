using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// A helper service to poll the Twitch API for events that are not available via IRC, like new followers.
    /// </summary>
    public class TwitchApiPollingService : IDisposable
    {
        private readonly TwitchAPI _twitchApi;
        private System.Threading.Timer? _followerPollTimer;
        private List<string>? _knownFollowerIds;

        public string? ChannelId { get; private set; }
        public event EventHandler<string>? NewFollowerDetected;
        public event EventHandler? TokenExpired;

        public TwitchApiPollingService(TwitchAPI twitchApi)
        {
            _twitchApi = twitchApi;
        }

        public async Task Start(string channelName, string accessToken)
        {
            if (string.IsNullOrEmpty(channelName) || string.IsNullOrEmpty(accessToken))
            {
                Debug.WriteLine("[TwitchApiPollingService] Cannot start polling without channel name and access token.");
                return;
            }

            ChannelId = await GetChannelId(channelName);
            if (string.IsNullOrEmpty(ChannelId))
            {
                Debug.WriteLine($"[TwitchApiPollingService] Could not resolve channel ID for '{channelName}'. Follower polling will not start.");
                return;
            }

            // Initialize the list of known followers to prevent firing events for all existing followers on startup.
            _knownFollowerIds = await GetFollowerIds(ChannelId, accessToken);
            Debug.WriteLine($"[TwitchApiPollingService] Initialized with {_knownFollowerIds.Count} known followers.");

            // Start polling for new followers every 30 seconds.
            _followerPollTimer = new System.Threading.Timer(PollForNewFollowers, accessToken, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
        }

        public void Stop()
        {
            _followerPollTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private async void PollForNewFollowers(object? state)
        {
            if (string.IsNullOrEmpty(ChannelId) || state is not string accessToken) return;

            try
            {
                var currentFollowers = await GetFollowerIds(ChannelId, accessToken);
                if (_knownFollowerIds == null)
                {
                    _knownFollowerIds = currentFollowers;
                    return;
                }

                var newFollowers = currentFollowers.Except(_knownFollowerIds).ToList();
                foreach (var followerId in newFollowers)
                {
                    // We get the ID, but for the alert, we need the name.
                    var userResponse = await _twitchApi.Helix.Users.GetUsersAsync(new List<string> { followerId });
                    var username = userResponse?.Users.FirstOrDefault()?.DisplayName ?? "A new follower";
                    NewFollowerDetected?.Invoke(this, username);
                }

                _knownFollowerIds = currentFollowers;
            }
            catch (BadTokenException)
            {
                Debug.WriteLine("[TwitchApiPollingService] Bad token detected during API poll. Firing TokenExpired event.");
                Stop(); // Stop polling to prevent repeated errors.
                TokenExpired?.Invoke(this, EventArgs.Empty);
            }
        }

        private async Task<string?> GetChannelId(string channelName)
        {
            var users = await _twitchApi.Helix.Users.GetUsersAsync(logins: new List<string> { channelName });
            return users?.Users.FirstOrDefault()?.Id;
        }

        private async Task<List<string>> GetFollowerIds(string channelId, string accessToken)
        {
            var followersResponse = await _twitchApi.Helix.Channels.GetChannelFollowersAsync(channelId, first: 100, accessToken: accessToken);
            return followersResponse?.Data.Select(f => f.UserId).ToList() ?? new List<string>();
        }

        public void Dispose()
        {
            Stop();
            _followerPollTimer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}