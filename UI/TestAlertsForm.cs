using EliteDataRelay.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// A small form with buttons to trigger various test alerts.
    /// </summary>
    public class TestAlertsForm : Form
    {
        private readonly TwitchTestService _testService;

        public TestAlertsForm(TwitchTestService testService)
        {
            _testService = testService;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Test Twitch Alerts";
            ClientSize = new Size(280, 200);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            StartPosition = FormStartPosition.CenterParent;

            var mainLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(15),
                WrapContents = false,
            };

            var btnTestFollow = new Button
            {
                Text = "Test Follower Alert",
                Size = new Size(240, 30),
            };
            btnTestFollow.Click += (s, e) => _testService.TestFollowerAlert();

            var btnTestSub = new Button
            {
                Text = "Test Subscription Alert",
                Size = new Size(240, 30),
                Margin = new Padding(0, 10, 0, 0),
            };
            btnTestSub.Click += (s, e) => _testService.TestSubscriptionAlert(isGift: false);

            var btnTestRaid = new Button
            {
                Text = "Test Raid Alert",
                Size = new Size(240, 30),
                Margin = new Padding(0, 10, 0, 0),
            };
            btnTestRaid.Click += (s, e) => _testService.TestRaidAlert();

            var btnTestChat = new Button
            {
                Text = "Test Chat Message",
                Size = new Size(240, 30),
                Margin = new Padding(0, 10, 0, 0),
            };
            btnTestChat.Click += (s, e) => _testService.TestChatMessage();

            mainLayout.Controls.Add(btnTestFollow);
            mainLayout.Controls.Add(btnTestSub);
            mainLayout.Controls.Add(btnTestRaid);
            mainLayout.Controls.Add(btnTestChat);

            Controls.Add(mainLayout);
        }
    }
}