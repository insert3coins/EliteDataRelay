using EliteDataRelay.Models;
using EliteDataRelay.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public class MiningSessionPanel : UserControl
    {
        private readonly SessionTrackingService _sessionTracker;
        private readonly FontManager _fontManager;

        // Theme-aligned colors
        private readonly Color accentColor = Color.FromArgb(34, 211, 238); // Cyan
        private readonly Color textColor = Color.FromArgb(220, 220, 230); // Light Gray/White
        private readonly Color secondaryTextColor = Color.FromArgb(156, 163, 175); // Gray
        
        private Label? _limpetsValueLabel;
        private Label? _refinedValueLabel;
        private Label? _durationValueLabel;
        private Label? _creditsValueLabel;
        private Label? _cargoValueLabel;
        private ListBox? _announcementListBox;        
        private NumericUpDown? _reminderMinutesUpDown;
        private Label? _reminderStatusLabel;
        private Button? _reminderButton;
        private Button? _startSessionButton;
        private Button? _stopSessionButton;
        private System.Windows.Forms.Timer? _updateTimer;
        private System.Windows.Forms.Timer? _reminderTimer;
        private TimeSpan _reminderRemaining = TimeSpan.Zero;

        private const int MaxAnnouncements = 50;

        public event EventHandler? StartMiningClicked;
        public event EventHandler? StopMiningClicked;

        public MiningSessionPanel(SessionTrackingService sessionTracker, FontManager fontManager)
        {
            _sessionTracker = sessionTracker;
            _fontManager = fontManager;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(10, 10, 10);
            Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            Padding = new Padding(10);

            // Use TableLayoutPanel instead of SplitContainer for precise control
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            
            // Top section takes ~2/3 of space, bottom takes ~1/3 (lower third)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 66F)); // Stats & Controls
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 34F)); // Announcements

            // --- Top Panel (Stats and Controls) ---
            var topPanel = new TableLayoutPanel { 
                Dock = DockStyle.Fill, 
                ColumnCount = 2, 
                BackColor = Color.FromArgb(15, 15, 15) 
            };
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            var statsGroup = BuildStatsGroup();
            var controlsGroup = BuildControlsGroup();
            topPanel.Controls.Add(statsGroup, 0, 0);
            topPanel.Controls.Add(controlsGroup, 1, 0);

            // --- Bottom Panel (Announcements) ---
            var announcementsGroup = BuildAnnouncementsGroup();

            // Add to main layout
            mainLayout.Controls.Add(topPanel, 0, 0);
            mainLayout.Controls.Add(announcementsGroup, 0, 1);

            Controls.Add(mainLayout);

            _updateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _updateTimer.Tick += (s, e) => UpdateStats();

            _reminderTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _reminderTimer.Tick += ReminderTimerOnTick;
            
            UpdateControlsVisibility();
        }

        private GroupBox BuildStatsGroup()
        {
            var statsGroup = new GroupBox { Text = "Real-time Statistics", ForeColor = secondaryTextColor, Font = new Font(Font, FontStyle.Bold), Dock = DockStyle.Fill, Padding = new Padding(5) };
            var statsTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(12), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            statsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            statsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var headerFont = new Font("Consolas", 12F, FontStyle.Bold);
            var valueFont = new Font("Consolas", 12F, FontStyle.Regular);
            
            _limpetsValueLabel = CreateValueLabel(valueFont);
            _refinedValueLabel = CreateValueLabel(valueFont);
            _durationValueLabel = CreateValueLabel(valueFont);
            _creditsValueLabel = CreateValueLabel(valueFont);
            _cargoValueLabel = CreateValueLabel(valueFont);
            
            statsTable.Controls.Add(CreateHeaderLabel("Limpets Used", headerFont), 0, 0);
            statsTable.Controls.Add(_limpetsValueLabel, 1, 0);
            statsTable.Controls.Add(CreateHeaderLabel("Refined", headerFont), 0, 1);
            statsTable.Controls.Add(_refinedValueLabel, 1, 1);
            statsTable.Controls.Add(CreateHeaderLabel("Active Duration", headerFont), 0, 2);
            statsTable.Controls.Add(_durationValueLabel, 1, 2);
            statsTable.Controls.Add(CreateHeaderLabel("Cargo Collected", headerFont), 0, 3);
            statsTable.Controls.Add(_cargoValueLabel, 1, 3);
            //statsTable.Controls.Add(CreateHeaderLabel("Credits Earned", headerFont), 0, 4);
            statsTable.Controls.Add(_creditsValueLabel, 1, 4);
            statsGroup.Controls.Add(statsTable);
            return statsGroup;
        }

        private GroupBox BuildControlsGroup()
        {
            var controlsGroup = new GroupBox 
            { 
                Text = "Controls", 
                ForeColor = secondaryTextColor,
                Font = new Font(Font, FontStyle.Bold), 
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            
            var mainFlowLayout = new FlowLayoutPanel 
            { 
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true
            };

            // --- Session Controls ---
            var sessionFlow = new FlowLayoutPanel 
            { 
                FlowDirection = FlowDirection.LeftToRight, 
                AutoSize = true, 
                Padding = new Padding(0, 5, 0, 15) 
            };
            _startSessionButton = CreatePrimaryButton("Start Session", (s, e) => StartMiningClicked?.Invoke(this, EventArgs.Empty));
            _stopSessionButton = CreateSecondaryButton("Stop Session", (s, e) => StopMiningClicked?.Invoke(this, EventArgs.Empty));
            sessionFlow.Controls.Add(_startSessionButton);
            sessionFlow.Controls.Add(_stopSessionButton);

            // --- Reminder Controls ---
            var reminderLayout = new TableLayoutPanel { ColumnCount = 2, AutoSize = true };
            reminderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            reminderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _reminderMinutesUpDown = new NumericUpDown 
            { 
                Minimum = 1, 
                Maximum = 120, 
                Value = 15, 
                Width = 70, 
                BackColor = Color.Black, 
                ForeColor = Color.White 
            };
            _reminderButton = CreateSecondaryButton("Start Reminder", OnReminderButtonClicked);
            reminderLayout.Controls.Add(new Label { Text = "Reminder (min):", ForeColor = Color.White, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 0, 0) }, 0, 0);
            reminderLayout.Controls.Add(_reminderMinutesUpDown, 1, 0);
            reminderLayout.Controls.Add(_reminderButton, 0, 1);
            reminderLayout.SetColumnSpan(_reminderButton, 2);

            _reminderStatusLabel = new Label 
            { 
                Text = "No reminder active", 
                ForeColor = secondaryTextColor, 
                AutoSize = true, 
                Padding = new Padding(5, 0, 5, 0) 
            };
            reminderLayout.Controls.Add(_reminderStatusLabel, 0, 2);
            reminderLayout.SetColumnSpan(_reminderStatusLabel, 2);

            // Add to main flow layout
            mainFlowLayout.Controls.Add(sessionFlow);
            mainFlowLayout.Controls.Add(reminderLayout);

            controlsGroup.Controls.Add(mainFlowLayout);
            return controlsGroup;
        }

        private GroupBox BuildAnnouncementsGroup()
        {
            var announcementsGroup = new GroupBox { 
                Text = "Announcements", 
                ForeColor = secondaryTextColor, 
                Dock = DockStyle.Fill, 
                Font = new Font(Font, FontStyle.Bold), 
                BackColor = Color.FromArgb(15, 15, 15) 
            };
            var layout = new TableLayoutPanel { 
                Dock = DockStyle.Fill, 
                ColumnCount = 1, 
                RowCount = 2, 
                Padding = new Padding(10) 
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _announcementListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 15),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                IntegralHeight = false
            };
            var clearButton = CreateSecondaryButton("Clear", (s, e) => _announcementListBox?.Items.Clear());
            clearButton.Anchor = AnchorStyles.Right;

            layout.Controls.Add(_announcementListBox, 0, 0);
            layout.Controls.Add(clearButton, 0, 1);
            announcementsGroup.Controls.Add(layout);
            return announcementsGroup;
        }

        private Button CreatePrimaryButton(string text, EventHandler onClick)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                BackColor = Color.FromArgb(28, 28, 35),
                ForeColor = accentColor,
                FlatStyle = FlatStyle.Flat,
                Padding = new Padding(10, 6, 10, 6),
                Font = new Font("Consolas", 11F, FontStyle.Bold)
            };
            button.FlatAppearance.BorderColor = accentColor;
            button.FlatAppearance.BorderSize = 1;
            button.Click += onClick;
            return button;
        }

        private Button CreateSecondaryButton(string text, EventHandler onClick)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                BackColor = Color.FromArgb(45, 45, 50),
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Padding = new Padding(6, 4, 6, 4),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            button.FlatAppearance.BorderColor = secondaryTextColor;
            button.FlatAppearance.BorderSize = 1;
            button.Click += onClick;
            return button;
        }

        private Label CreateHeaderLabel(string text, Font font) => new()
        {
            Text = text,
            Font = font,
            ForeColor = secondaryTextColor,
            Anchor = AnchorStyles.Left,
            AutoSize = true
        };

        private Label CreateValueLabel(Font font) => new()
        {
            Text = "0",
            Font = font,
            ForeColor = textColor,
            Anchor = AnchorStyles.Right,
            AutoSize = true
        };

        private void ReminderTimerOnTick(object? sender, EventArgs e)
        {
            if (_reminderRemaining <= TimeSpan.Zero)
            {
                _reminderTimer?.Stop();
                _reminderStatusLabel!.Text = "Reminder finished";
                _reminderButton!.Text = "Start Reminder";
                _sessionTracker.PublishCustomNotification("Mining reminder completed.", MiningNotificationType.Reminder, true);
                return;
            }

            _reminderRemaining -= TimeSpan.FromSeconds(1);
            _reminderStatusLabel!.Text = $"Reminder active: {_reminderRemaining:hh\\:mm\\:ss}";
        }

        private void OnReminderButtonClicked(object? sender, EventArgs e)
        {
            if (_reminderTimer == null || _reminderMinutesUpDown == null) return;

            if (_reminderTimer.Enabled)
            {
                _reminderTimer.Stop();
                _reminderStatusLabel!.Text = "Reminder cancelled";
                _reminderButton!.Text = "Start Reminder";
                return;
            }

            _reminderRemaining = TimeSpan.FromMinutes((double)_reminderMinutesUpDown.Value);
            _reminderStatusLabel!.Text = $"Reminder active: {_reminderRemaining:hh\\:mm\\:ss}";
            _reminderButton!.Text = "Cancel Reminder";
            _reminderTimer.Start();
        }

        public void UpdateStats()
        {
            if (!IsHandleCreated) return;

            UpdateControlsVisibility();

            var duration = _sessionTracker.MiningDuration;
            _limpetsValueLabel!.Text = _sessionTracker.LimpetsUsed.ToString("N0");
            _refinedValueLabel!.Text = $"{_sessionTracker.TotalRefinedCount:N0} units";
            _durationValueLabel!.Text = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            _creditsValueLabel!.Text = $"{_sessionTracker.CreditsEarned:N0} cr";
            _cargoValueLabel!.Text = $"{_sessionTracker.TotalCargoCollected:N0} units";
        }

        private void UpdateControlsVisibility()
        {
            bool sessionActive = _sessionTracker.IsMiningSessionActive;
            _startSessionButton!.Enabled = !sessionActive;
            _stopSessionButton!.Enabled = sessionActive;

            if (sessionActive && _updateTimer?.Enabled == false)
            {
                _updateTimer!.Start();
            }
            else if (!sessionActive && _updateTimer?.Enabled == true)
            {
                _updateTimer!.Stop();
            }
        }

        public void AddAnnouncement(MiningNotificationEventArgs notification)
        {
            if (_announcementListBox == null) return;
            var text = notification.ToString();
            _announcementListBox.BeginUpdate();
            _announcementListBox.Items.Insert(0, text);
            while (_announcementListBox.Items.Count > MaxAnnouncements)
            {
                _announcementListBox.Items.RemoveAt(_announcementListBox.Items.Count - 1);
            }
            _announcementListBox.EndUpdate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updateTimer?.Dispose();
                _reminderTimer?.Dispose();
                _startSessionButton?.Dispose();
                _stopSessionButton?.Dispose();
                _announcementListBox?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}