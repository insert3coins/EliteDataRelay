using EliteDataRelay.UI;
using System;
using EliteDataRelay.Configuration;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.Services
{
    public class TwitchOverlayManager : IDisposable
    {
        private readonly ITwitchService _twitchService;
        private readonly OverlayService _overlayService;
        private readonly List<Form> _activeChatBubbles = new();

        public TwitchOverlayManager(ITwitchService twitchService, OverlayService overlayService)
        {
            _twitchService = twitchService;
            _overlayService = overlayService;

            _twitchService.ChatMessageReceived += OnChatMessageReceived;
            _twitchService.FollowerReceived += OnFollowerReceived;
            _twitchService.RaidReceived += OnRaidReceived;
            _twitchService.SubscriptionReceived += OnSubscriptionReceived;
        }

        private void OnChatMessageReceived(object? sender, TwitchChatMessageEventArgs e)
        {
            if (!AppConfiguration.EnableTwitchChatBubbles) return;

            var shipIconOverlay = _overlayService.GetOverlay(OverlayForm.OverlayPosition.ShipIcon);
            if (shipIconOverlay == null || shipIconOverlay.IsDisposed) return;

            // Use BeginInvoke to ensure UI creation happens on the main UI thread.
            shipIconOverlay.BeginInvoke(new Action(() =>
            {
                var bubble = new TwitchChatBubbleForm(e.Username, e.Message)
                {
                    Width = 250,
                    Height = 100
                };

                // Position the new bubble above existing ones.
                int yOffset = _activeChatBubbles.Sum(f => f.Height + 5);
                bubble.Location = new Point(shipIconOverlay.Right + 10, shipIconOverlay.Top - yOffset);

                bubble.FormClosed += (s, a) => _activeChatBubbles.Remove(bubble);
                _activeChatBubbles.Add(bubble);
                bubble.Show();
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

        private void ShowAlert(string line1, string line2)
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
        }
    }
}