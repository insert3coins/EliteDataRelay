using EliteDataRelay.UI;
using System;
using System.Threading.Tasks;
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
        private readonly TwitchBadgeService _badgeService;
        private readonly List<TwitchChatBubbleForm> _activeChatForms = new();

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
            // The check for whether the feature is enabled is now inside AddChatMessage.
            _ = AddChatMessage(e.Username, e.Message, e.Badges, e.BadgeInfo);
        }

        public async Task AddChatMessage(string username, string message, List<KeyValuePair<string, string>>? badges = null, List<KeyValuePair<string, string>>? badgeInfo = null)
        {
            if (!AppConfiguration.EnableTwitchChatOverlay) return;

            var mainForm = Application.OpenForms.OfType<CargoForm>().FirstOrDefault();
            if (mainForm == null || mainForm.IsDisposed) return;

            // Use a TaskCompletionSource to bridge the gap between the older BeginInvoke
            // and the modern async/await pattern. This is necessary because Control.InvokeAsync
            // is not available in all .NET Framework versions.
            var tcs = new TaskCompletionSource<object?>();
            mainForm.BeginInvoke(new Func<Task>(async () =>
            { // Note: This lambda will now capture the result of the async operation.
                try
                {
                    // Clean up any closed forms from the list
                    _activeChatForms.RemoveAll(f => f.IsDisposed);

                    var infoOverlay = _overlayService.GetOverlay(OverlayForm.OverlayPosition.Info);

                    const int spacing = 5;
                    int startX, startY, width;

                    if (infoOverlay != null && infoOverlay.Visible)
                    {
                        startX = infoOverlay.Left;
                        startY = infoOverlay.Top - 70; // A bit above the info overlay
                        width = infoOverlay.Width;
                    }
                    else
                    {
                        var screen = Screen.FromControl(mainForm).WorkingArea;
                        startX = screen.Left + 20;
                        startY = screen.Bottom - 70; // 10px from bottom edge
                        width = 320; // Default width if info overlay isn't visible
                    }

                    // Get badge images. This is done inside the BeginInvoke to ensure the result is used on the UI thread.
                    var badgeImages = await _badgeService.GetBadgesForUser(badgeInfo);

                    // Create the new chat form
                    var newChatForm = new TwitchChatBubbleForm(username, message, startX, startY, width, badgeImages);
                    newChatForm.FormClosed += (s, a) => _activeChatForms.Remove(newChatForm);

                    // Move existing forms up
                    int yOffset = newChatForm.Height + spacing;
                    foreach (var form in _activeChatForms)
                    {
                        form.SetTargetY(form.Top - yOffset);
                    }

                    _activeChatForms.Add(newChatForm);
                    newChatForm.Show();
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));
            await tcs.Task;
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
            
            // Create a copy of the list to iterate over. Calling form.Close() triggers an
            // event that modifies the original _activeChatForms collection, which would
            // otherwise cause a "Collection was modified" exception.
            foreach (var form in _activeChatForms.ToList())
            {
                form.Close();
            }
        }
    }
}