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
        private ProgressBar? _cargoFillProgressBar;
        private Label? _cargoFillPercentLabel;
        private ListBox? _announcementListBox;
        private CheckBox? _cargoPromptCheckbox;
        private CheckBox? _announcementCheckbox;
        private NumericUpDown? _reminderMinutesUpDown;
        private Label? _reminderStatusLabel;
        private Button? _reminderButton;
        private Button? _startSessionButton;
        private Button? _stopSessionButton;
        private System.Windows.Forms.Timer? _updateTimer;
        private System.Windows.Forms.Timer? _reminderTimer;
        private TimeSpan _reminderRemaining = TimeSpan.Zero;
        private bool _isApplyingPreferences;

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

            var mainTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Appearance = TabAppearance.Normal, // Use Normal for better compatibility with custom drawing
                Alignment = TabAlignment.Top, // Revert to top alignment
                SizeMode = TabSizeMode.Fixed
            };

            var dashboardTab = BuildDashboardTab();
            var announcementsTab = BuildAnnouncementsTab();
            var settingsTab = BuildSettingsTab();

            mainTabControl.TabPages.Add(dashboardTab);
            mainTabControl.TabPages.Add(announcementsTab);
            mainTabControl.TabPages.Add(settingsTab);

            Controls.Add(mainTabControl);

            _updateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _updateTimer.Tick += (s, e) => UpdateStats();

            _reminderTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _reminderTimer.Tick += ReminderTimerOnTick;

            UpdateControlsVisibility();

            // Apply custom styling to the TabControl
            mainTabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            mainTabControl.ItemSize = new Size(120, 30);
            mainTabControl.DrawItem += (s, e) =>
            {
                var tab = mainTabControl.TabPages[e.Index];
                var isSelected = mainTabControl.SelectedIndex == e.Index;

                // In Normal appearance, we need to draw over the default tab background.
                e.DrawBackground();

                using var bgBrush = new SolidBrush(isSelected ? Color.FromArgb(45, 45, 45) : Color.FromArgb(25, 25, 25));
                e.Graphics.FillRectangle(bgBrush, e.Bounds);

                using var textBrush = new SolidBrush(isSelected ? accentColor : textColor);
                TextRenderer.DrawText(e.Graphics, tab.Text, e.Font, e.Bounds, textBrush.Color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                if (isSelected)
                {
                    using var borderPen = new Pen(accentColor, 2);
                    e.Graphics.DrawLine(borderPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                }
            };
        }

        private TabPage BuildDashboardTab()
        {
            var tabPage = new TabPage("Dashboard") { BackColor = Color.FromArgb(15, 15, 15), Padding = new Padding(10) };
            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // This row will fill the remaining space.
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // This row will have a fixed height.

            // --- Stats Panel (Left) ---
            var statsGroup = new GroupBox { Text = "Real-time Statistics", ForeColor = secondaryTextColor, Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Bold) };
            var statsTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(12) };
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
            statsTable.Controls.Add(CreateHeaderLabel("Credits Earned", headerFont), 0, 3);
            statsTable.Controls.Add(_creditsValueLabel, 1, 3);
            statsTable.Controls.Add(CreateHeaderLabel("Cargo Collected", headerFont), 0, 4);
            statsTable.Controls.Add(_cargoValueLabel, 1, 4);
            statsGroup.Controls.Add(statsTable);

            // --- Controls Panel (Right) ---
            var controlsGroup = new GroupBox { Text = "Session Controls", ForeColor = secondaryTextColor, Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Bold) };
            var controlsLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10), WrapContents = false };
            _startSessionButton = CreatePrimaryButton("Start Session", (s, e) => StartMiningClicked?.Invoke(this, EventArgs.Empty));
            _stopSessionButton = CreateSecondaryButton("Stop Session", (s, e) => StopMiningClicked?.Invoke(this, EventArgs.Empty));
            controlsLayout.Controls.Add(_startSessionButton);
            controlsLayout.Controls.Add(_stopSessionButton);
            controlsLayout.Controls.Add(new Panel { Height = 20 }); // Spacer
            _reminderMinutesUpDown = new NumericUpDown { Minimum = 1, Maximum = 120, Value = 15, Width = 60, BackColor = Color.Black, ForeColor = Color.White };
            _reminderButton = CreateSecondaryButton("Start Reminder", OnReminderButtonClicked);
            var reminderFlow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            reminderFlow.Controls.Add(new Label { Text = "Reminder (min):", ForeColor = Color.White, AutoSize = true, Padding = new Padding(0, 5, 0, 0) });
            reminderFlow.Controls.Add(_reminderMinutesUpDown);
            reminderFlow.Controls.Add(_reminderButton);
            controlsLayout.Controls.Add(reminderFlow);
            _reminderStatusLabel = new Label { Text = "No reminder active", ForeColor = secondaryTextColor, AutoSize = true, Padding = new Padding(5) };
            controlsLayout.Controls.Add(_reminderStatusLabel);
            controlsGroup.Controls.Add(controlsLayout);

            // --- Cargo Bar (Bottom) ---
            var cargoPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            _cargoFillProgressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                ForeColor = accentColor,
                Style = ProgressBarStyle.Continuous,
            };
            _cargoFillPercentLabel = new Label { Dock = DockStyle.Right, ForeColor = Color.White, Font = new Font("Consolas", 10f, FontStyle.Bold), AutoSize = true, Padding = new Padding(0, 0, 10, 0) };
            cargoPanel.Controls.Add(_cargoFillProgressBar);
            cargoPanel.Controls.Add(_cargoFillPercentLabel);

            mainLayout.Controls.Add(statsGroup, 0, 0);
            mainLayout.Controls.Add(controlsGroup, 1, 0);
            mainLayout.Controls.Add(cargoPanel, 0, 1);
            mainLayout.SetColumnSpan(cargoPanel, 2);
            tabPage.Controls.Add(mainLayout);

            return tabPage;
        }

        private TabPage BuildSettingsTab()
        {
            var tabPage = new TabPage("Settings") { BackColor = Color.FromArgb(15, 15, 15), Padding = new Padding(10) };
            var layout = new TableLayoutPanel {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
            };

            _cargoPromptCheckbox = CreateCheckBox("Cargo full notifications", (s, e) =>
            {
                if (_isApplyingPreferences) return;
                _sessionTracker.Preferences.CargoFullPromptEnabled = _cargoPromptCheckbox!.Checked;
            });

            _announcementCheckbox = CreateCheckBox("Enable announcements", (s, e) =>
            {
                if (_isApplyingPreferences) return;
                _sessionTracker.Preferences.AnnouncementsEnabled = _announcementCheckbox!.Checked;
            });

            layout.Controls.Add(_cargoPromptCheckbox, 0, 0);
            layout.Controls.Add(_announcementCheckbox, 1, 0);

            tabPage.Controls.Add(layout);
            return tabPage;
        }

        private TabPage BuildAnnouncementsTab()
        {
            var tabPage = new TabPage("Announcements") { BackColor = Color.FromArgb(15, 15, 15), Padding = new Padding(10) };

            _announcementListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 15),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                IntegralHeight = false
            };

            var clearButton = CreateSecondaryButton("Clear", (s, e) => _announcementListBox?.Items.Clear())
                ?? new Button();
            clearButton.Dock = DockStyle.Bottom;

            tabPage.Controls.Add(_announcementListBox);
            tabPage.Controls.Add(clearButton);

            return tabPage;
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
                Padding = new Padding(8, 4, 8, 4),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            button.FlatAppearance.BorderColor = secondaryTextColor;
            button.FlatAppearance.BorderSize = 1;
            button.Click += onClick;
            return button;
        }

        private CheckBox CreateCheckBox(string text, EventHandler onChanged)
        {
            var checkbox = new CheckBox
            {
                Text = text,
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            checkbox.CheckedChanged += onChanged;
            return checkbox;
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
            _cargoFillPercentLabel!.Text = $"{_sessionTracker.CargoFillPercent:F1}%";

            if (_cargoFillProgressBar != null)
            {
                var value = Math.Max(0, Math.Min(100, (int)Math.Round(_sessionTracker.CargoFillPercent)));
                _cargoFillProgressBar.Value = value;
            }
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

        public void ApplyPreferences(MiningSessionPreferences preferences)
        {
            _isApplyingPreferences = true;
            try
            {
                _cargoPromptCheckbox!.Checked = preferences.CargoFullPromptEnabled;
                _announcementCheckbox!.Checked = preferences.AnnouncementsEnabled;
            }
            finally
            {
                _isApplyingPreferences = false;
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
