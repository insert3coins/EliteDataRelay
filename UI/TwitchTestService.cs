using System;
using System.Collections.Generic;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// A service dedicated to triggering simulated Twitch events for testing UI elements.
    /// </summary>
    public class TwitchTestService
    {
        private readonly TwitchOverlayManager _overlayManager;

        public TwitchTestService(TwitchOverlayManager overlayManager)
        {
            _overlayManager = overlayManager ?? throw new ArgumentNullException(nameof(overlayManager));
        }

        public void TestFollowerAlert()
        {
            // We can bypass the configuration check to ensure the test always works.
            // We call the public ShowAlert method on the manager.
            _overlayManager.ShowAlert("New Follower", "Test_Follower");
        }

        public void TestSubscriptionAlert(bool isGift)
        {
            string line1 = isGift ? "New Gift Subscription!" : "New Subscription!";
            _overlayManager.ShowAlert(line1, "Test_Subscriber (Tier 1)");
        }

        public void TestRaidAlert()
        {
            _overlayManager.ShowAlert("Incoming Raid!", "Test_Raider (42)");
        }

        public async void TestChatMessage()
        {
            // To test the chat ticker, we need to call the public AddMessage method.
            // This requires making a small change to TwitchOverlayManager to expose it.
            var testBadges = new List<KeyValuePair<string, string>> // This is the 'badges' tag
            {
                new KeyValuePair<string, string>("broadcaster", "1"),
                new KeyValuePair<string, string>("subscriber", "6"),
                new KeyValuePair<string, string>("premium", "1")
            };
            var testBadgeInfo = new List<KeyValuePair<string, string>> // This is the 'badge-info' tag
            {
                new KeyValuePair<string, string>("subscriber/6", "https://static-cdn.jtvnw.net/badges/v1/521973f3-3373-4705-aebb-a999a4e97333/1"),
                new KeyValuePair<string, string>("premium/1", "https://static-cdn.jtvnw.net/badges/v1/bbbe0db4-a5ce-4493-9919-253795915229/1")
            };
            await _overlayManager.AddChatMessage("Test_Chatter", "This is a test message with badges! o7", testBadges, testBadgeInfo);
        }
    }
}