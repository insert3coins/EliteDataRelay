using EliteDataRelay.UI;
using System;
using System.Threading.Tasks;
using EliteDataRelay.Configuration;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace EliteDataRelay.Services
{
    public class TwitchOverlayManager : IDisposable
    {
        private readonly ITwitchService _twitchService;
        private readonly OverlayService _overlayService;
        private readonly TwitchBadgeService _badgeService;
        private TwitchChatOverlayForm? _chatOverlayForm;

        public TwitchOverlayManager(ITwitchService twitchService, OverlayService overlayService, TwitchBadgeService badgeService)
        {
            _twitchService = twitchService;
            _overlayService = overlayService;
            _badgeService = badgeService;

            _twitchService.ChatMessageReceived += OnChatMessageReceived;
            _twitchService.FollowerReceived += OnFollowerReceived;
            _twitchService.RaidReceived += OnRaidReceived;
            _twitchService.SubscriptionReceived += OnSubscriptionReceived;
        }

        private void OnChatMessageReceived(object? sender, TwitchChatMessageEventArgs e)
        {
            try
            {
                // The check for whether the feature is enabled is now inside AddChatMessage.
                AddChatMessage(e.Username, e.Message, e.Badges, e.BadgeInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TwitchOverlayManager] Failed to show chat message: {ex.Message}");
            }
        }

        public void AddChatMessage(string username, string message, List<KeyValuePair<string, string>>? badges = null, List<KeyValuePair<string, string>>? badgeInfo = null)
        {
            if (!AppConfiguration.EnableTwitchChatOverlay) return;

            var mainForm = Application.OpenForms.OfType<CargoForm>().FirstOrDefault();
            if (mainForm == null || mainForm.IsDisposed) return;

            // We must ensure that the UI-related work is done on the main UI thread.
            // BeginInvoke queues the action and returns immediately. The 'async' lambda
            // ensures that work is correctly marshaled between UI and background threads.
            mainForm.BeginInvoke(new Action(async () =>
            {
                try
                {
                    if (_chatOverlayForm == null || _chatOverlayForm.IsDisposed)
                    {
                        var infoOverlay = _overlayService.GetOverlay(OverlayForm.OverlayPosition.Info);
                        int startX, startY, width, height;
                        if (infoOverlay != null && infoOverlay.Visible)
                        {
                            startX = infoOverlay.Left;
                            width = infoOverlay.Width;
                            height = 400;
                            startY = infoOverlay.Top - height - 10; // Position above the info overlay
                        }
                        else
                        {
                            var screen = Screen.FromControl(mainForm).WorkingArea;
                            width = 320;
                            height = 400;
                            startX = screen.Left + 20;
                            startY = screen.Bottom - height - 70;
                        }
                        _chatOverlayForm = new TwitchChatOverlayForm { Location = new Point(startX, startY), Size = new Size(width, height) };
                        _chatOverlayForm.Show();
                    }
                    // Fetching badges is I/O bound, so we await it here. The UI thread is freed up during the await.
                    var badgeImages = await _badgeService.GetBadgesForUser(badgeInfo);
                    // The code continues on the UI thread, so it's safe to call AddMessage.
                    _chatOverlayForm?.AddMessage(username, message, badgeImages);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TwitchOverlayManager] Error in AddChatMessage UI Action: {ex.Message}");
                }
            }));
        }

        private void OnFollowerReceived(object? sender, TwitchFollowerEventArgs e)
        {
            if (!AppConfiguration.EnableTwitchFollowerAlerts) return;
            ShowAlert("New Follower", e.Username);
        }

        private void OnRaidReceived(object? sender, TwitchRaidEventArgs e)
        {
            if (!AppConfiguration.EnableTwitchRaidAlerts) return;
            ShowAlert("Incoming Raid!", $"{e.Username} ({e.ViewerCount})");
        }

        private void OnSubscriptionReceived(object? sender, TwitchSubscriptionEventArgs e)
        {
            if (!AppConfiguration.EnableTwitchSubAlerts) return;
            string line1 = e.IsGift ? "New Gift Subscription!" : "New Subscription!";
            ShowAlert(line1, $"{e.Username} ({e.Tier})");
        }

        public void ShowAlert(string line1, string line2)
        {
            var mainForm = Application.OpenForms.OfType<CargoForm>().FirstOrDefault();
            if (mainForm == null) return;

            mainForm.BeginInvoke(new Action(() =>
            {
                var alert = new TwitchAlertForm(line1, line2)
                {
                    Width = 400,
                    Height = 100
                };

                // Center the alert on the primary screen.
                var screen = Screen.PrimaryScreen;
                if (screen == null) return; // Can be null in rare edge cases

                alert.Location = new Point(
                    screen.WorkingArea.Left + (screen.WorkingArea.Width - alert.Width) / 2,
                    screen.WorkingArea.Top + 100);

                alert.Show();
            }));
        }

        public void Dispose()
        {
            _twitchService.ChatMessageReceived -= OnChatMessageReceived;
            _twitchService.FollowerReceived -= OnFollowerReceived;
            _twitchService.RaidReceived -= OnRaidReceived;
            _twitchService.SubscriptionReceived -= OnSubscriptionReceived;

            _chatOverlayForm?.Close();
            _chatOverlayForm = null;
        }
    }
}