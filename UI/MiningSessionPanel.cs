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

        private readonly Color eliteOrange = Color.FromArgb(255, 136, 0);
        private readonly Color eliteOrangeLight = Color.FromArgb(255, 153, 68);
        private readonly Color eliteGreen = Color.FromArgb(0, 255, 0);

        private Label? _limpetsValueLabel;
        private Label? _refinedValueLabel;
        private Label? _durationValueLabel;
        private Label? _creditsValueLabel;
        private Label? _cargoValueLabel;
        private ProgressBar? _cargoFillProgressBar;
        private Label? _cargoFillPercentLabel;
        private ListBox? _announcementListBox;
        private ListView? _historyListView;
        private CheckBox? _autoStartCheckbox;
        private CheckBox? _cargoPromptCheckbox;
        private CheckBox? _announcementCheckbox;
        private CheckBox? _autoReportCheckbox;
        private NumericUpDown? _idleMinutesUpDown;
        private ComboBox? _firegroupCombo;
        private NumericUpDown? _reminderMinutesUpDown;
        private Label? _reminderStatusLabel;
        private Button? _reminderButton;
        private Button? _startSessionButton;
        private Button? _stopSessionButton;
        private Button? _createBackupButton;
        private Button? _restoreBackupButton;
        private Button? _generateReportButton;
        private System.Windows.Forms.Timer? _updateTimer;
        private System.Windows.Forms.Timer? _reminderTimer;
        private TimeSpan _reminderRemaining = TimeSpan.Zero;
        private bool _isApplyingPreferences;

        private const int MaxAnnouncements = 50;

        public event EventHandler? StartMiningClicked;
        public event EventHandler? StopMiningClicked;
        public event EventHandler? BackupRequested;
        public event EventHandler? RestoreRequested;
        public event EventHandler? GenerateReportRequested;

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

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(12),
                BackColor = Color.Transparent
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 35f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 35f));

            var statsPanel = BuildStatsPanel();
            var controlsPanel = BuildControlsPanel();
            var announcementsPanel = BuildAnnouncementsPanel();
            var historyPanel = BuildHistoryPanel();

            mainLayout.Controls.Add(statsPanel, 0, 0);
            mainLayout.SetColumnSpan(statsPanel, 2);
            mainLayout.Controls.Add(controlsPanel, 0, 1);
            mainLayout.Controls.Add(announcementsPanel, 1, 1);
            mainLayout.Controls.Add(historyPanel, 0, 2);
            mainLayout.SetColumnSpan(historyPanel, 2);

            Controls.Add(mainLayout);

            _updateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _updateTimer.Tick += (s, e) => UpdateStats();

            _reminderTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _reminderTimer.Tick += ReminderTimerOnTick;

            UpdateControlsVisibility();
        }

        private Control BuildStatsPanel()
        {
            var container = new GroupBox
            {
                Text = "Real-time Mining Statistics",
                ForeColor = eliteOrangeLight,
                Dock = DockStyle.Fill,
                Font = new Font(Font, FontStyle.Bold)
            };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                BackColor = Color.Transparent,
                Padding = new Padding(12)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            var headerFont = new Font("Consolas", 12F, FontStyle.Bold);
            var valueFont = new Font("Consolas", 12F, FontStyle.Regular);

            _limpetsValueLabel = CreateValueLabel(valueFont);
            _refinedValueLabel = CreateValueLabel(valueFont);
            _durationValueLabel = CreateValueLabel(valueFont);
            _creditsValueLabel = CreateValueLabel(valueFont);
            _cargoValueLabel = CreateValueLabel(valueFont);
            _cargoFillPercentLabel = CreateValueLabel(valueFont);

            table.Controls.Add(CreateHeaderLabel("Limpets Used", headerFont), 0, 0);
            table.Controls.Add(_limpetsValueLabel, 1, 0);
            table.Controls.Add(CreateHeaderLabel("Refined", headerFont), 0, 1);
            table.Controls.Add(_refinedValueLabel, 1, 1);
            table.Controls.Add(CreateHeaderLabel("Active Duration", headerFont), 0, 2);
            table.Controls.Add(_durationValueLabel, 1, 2);
            table.Controls.Add(CreateHeaderLabel("Credits Earned", headerFont), 0, 3);
            table.Controls.Add(_creditsValueLabel, 1, 3);
            table.Controls.Add(CreateHeaderLabel("Cargo Collected", headerFont), 0, 4);
            table.Controls.Add(_cargoValueLabel, 1, 4);

            _cargoFillProgressBar = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 18,
                ForeColor = eliteGreen,
                Style = ProgressBarStyle.Continuous,
            };

            var cargoFillPanel = new Panel { Dock = DockStyle.Bottom, Height = 36 };
            var cargoFillLabel = new Label
            {
                Text = "Cargo Fill",
                ForeColor = eliteOrangeLight,
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = headerFont
            };
            _cargoFillPercentLabel.TextAlign = ContentAlignment.MiddleRight;
            _cargoFillPercentLabel.Dock = DockStyle.Right;

            cargoFillPanel.Controls.Add(_cargoFillPercentLabel);
            cargoFillPanel.Controls.Add(cargoFillLabel);

            container.Controls.Add(_cargoFillProgressBar);
            container.Controls.Add(cargoFillPanel);
            container.Controls.Add(table);

            return container;
        }

        private Control BuildControlsPanel()
        {
            var group = new GroupBox
            {
                Text = "Session Controls",
                ForeColor = eliteOrangeLight,
                Dock = DockStyle.Fill,
                Font = new Font(Font, FontStyle.Bold)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(10)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            var buttonFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true
            };

            _startSessionButton = CreatePrimaryButton("Start Session", (s, e) => StartMiningClicked?.Invoke(this, EventArgs.Empty));
            _stopSessionButton = CreateSecondaryButton("Stop Session", (s, e) => StopMiningClicked?.Invoke(this, EventArgs.Empty));

            buttonFlow.Controls.Add(_startSessionButton);
            buttonFlow.Controls.Add(_stopSessionButton);

            _autoStartCheckbox = CreateCheckBox("Auto-start on prospector limpets", (s, e) =>
            {
                if (_isApplyingPreferences) return;
                _sessionTracker.Preferences.AutoStartOnProspector = _autoStartCheckbox!.Checked;
                _sessionTracker.NotifyPreferencesChanged();
            });

            _cargoPromptCheckbox = CreateCheckBox("Cargo full notifications", (s, e) =>
            {
                if (_isApplyingPreferences) return;
                _sessionTracker.Preferences.CargoFullPromptEnabled = _cargoPromptCheckbox!.Checked;
                _sessionTracker.NotifyPreferencesChanged();
            });

            _announcementCheckbox = CreateCheckBox("Enable announcements", (s, e) =>
            {
                if (_isApplyingPreferences) return;
                _sessionTracker.Preferences.AnnouncementsEnabled = _announcementCheckbox!.Checked;
                _sessionTracker.NotifyPreferencesChanged();
            });

            _autoReportCheckbox = CreateCheckBox("Auto-generate HTML reports", (s, e) =>
            {
                if (_isApplyingPreferences) return;
                _sessionTracker.Preferences.AutoGenerateHtmlReports = _autoReportCheckbox!.Checked;
                _sessionTracker.NotifyPreferencesChanged();
            });

            _idleMinutesUpDown = new NumericUpDown
            {
                Minimum = 0.5M,
                Maximum = 10M,
                DecimalPlaces = 1,
                Increment = 0.5M,
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                ForeColor = Color.White
            };
            _idleMinutesUpDown.ValueChanged += (s, e) =>
            {
                if (_isApplyingPreferences) return;
                _sessionTracker.Preferences.CargoIdleMinutesThreshold = (double)_idleMinutesUpDown!.Value;
                _sessionTracker.NotifyPreferencesChanged();
            };

            _firegroupCombo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(18, 18, 18),
                ForeColor = Color.White
            };
            for (int i = 1; i <= 8; i++)
            {
                _firegroupCombo.Items.Add($"Firegroup {i}");
            }
            _firegroupCombo.SelectedIndexChanged += (s, e) =>
            {
                if (_isApplyingPreferences) return;
                _sessionTracker.Preferences.PreferredFireGroup = _firegroupCombo!.SelectedIndex + 1;
                _sessionTracker.NotifyPreferencesChanged();
            };

            _reminderMinutesUpDown = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 120,
                Value = 15,
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                ForeColor = Color.White
            };

            _reminderStatusLabel = new Label
            {
                Text = "No reminder active",
                ForeColor = eliteOrangeLight,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _reminderButton = CreateSecondaryButton("Start Reminder", OnReminderButtonClicked);

            _createBackupButton = CreateSecondaryButton("Create Backup", (s, e) => BackupRequested?.Invoke(this, EventArgs.Empty));
            _restoreBackupButton = CreateSecondaryButton("Restore Backup", (s, e) => RestoreRequested?.Invoke(this, EventArgs.Empty));
            _generateReportButton = CreateSecondaryButton("Generate Report", (s, e) => GenerateReportRequested?.Invoke(this, EventArgs.Empty));

            layout.Controls.Add(buttonFlow, 0, 0);
            layout.SetColumnSpan(buttonFlow, 2);

            layout.Controls.Add(_autoStartCheckbox, 0, 1);
            layout.Controls.Add(_cargoPromptCheckbox, 1, 1);
            layout.Controls.Add(_announcementCheckbox, 0, 2);
            layout.Controls.Add(_autoReportCheckbox, 1, 2);

            layout.Controls.Add(new Label { Text = "Idle minutes before cargo prompt", ForeColor = eliteOrangeLight, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 3);
            layout.Controls.Add(_idleMinutesUpDown, 1, 3);

            layout.Controls.Add(new Label { Text = "Preferred Firegroup", ForeColor = eliteOrangeLight, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 4);
            layout.Controls.Add(_firegroupCombo, 1, 4);

            var reminderRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            reminderRow.Controls.Add(new Label { Text = "Reminder (minutes)", ForeColor = eliteOrangeLight, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
            reminderRow.Controls.Add(_reminderMinutesUpDown);
            reminderRow.Controls.Add(_reminderButton);

            layout.Controls.Add(reminderRow, 0, 5);
            layout.Controls.Add(_reminderStatusLabel, 1, 5);

            var footerFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true
            };
            footerFlow.Controls.Add(_createBackupButton);
            footerFlow.Controls.Add(_restoreBackupButton);
            footerFlow.Controls.Add(_generateReportButton);

            group.Controls.Add(footerFlow);
            group.Controls.Add(layout);

            return group;
        }

        private Control BuildAnnouncementsPanel()
        {
            var group = new GroupBox
            {
                Text = "Announcements & Notifications",
                ForeColor = eliteOrangeLight,
                Dock = DockStyle.Fill,
                Font = new Font(Font, FontStyle.Bold)
            };

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

            group.Controls.Add(_announcementListBox);
            group.Controls.Add(clearButton);

            return group;
        }

        private Control BuildHistoryPanel()
        {
            var group = new GroupBox
            {
                Text = "Session History",
                ForeColor = eliteOrangeLight,
                Dock = DockStyle.Fill,
                Font = new Font(Font, FontStyle.Bold)
            };

            _historyListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(12, 12, 12),
                ForeColor = Color.White
            };

            _historyListView.Columns.Add("Start", 150);
            _historyListView.Columns.Add("Duration", 120);
            _historyListView.Columns.Add("Mining", 100);
            _historyListView.Columns.Add("Credits", 120);
            _historyListView.Columns.Add("Cargo", 100);
            _historyListView.Columns.Add("Limpets", 80);
            _historyListView.Columns.Add("Cargo Fill", 100);

            group.Controls.Add(_historyListView);
            return group;
        }

        private Button CreatePrimaryButton(string text, EventHandler onClick)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                BackColor = Color.FromArgb(0, 20, 40),
                ForeColor = eliteOrange,
                FlatStyle = FlatStyle.Flat,
                Padding = new Padding(10, 6, 10, 6),
                Font = new Font("Consolas", 11F, FontStyle.Bold)
            };
            button.FlatAppearance.BorderColor = eliteOrange;
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
                BackColor = Color.FromArgb(25, 25, 25),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Padding = new Padding(8, 4, 8, 4),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            button.FlatAppearance.BorderColor = eliteOrangeLight;
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
            ForeColor = eliteOrangeLight,
            Anchor = AnchorStyles.Left,
            AutoSize = true
        };

        private Label CreateValueLabel(Font font) => new()
        {
            Text = "0",
            Font = font,
            ForeColor = Color.White,
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
            _reminderStatusLabel!.Text = $"Reminder active: {_reminderRemaining:hh\:mm\:ss}";
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
            _reminderStatusLabel!.Text = $"Reminder active: {_reminderRemaining:hh\:mm\:ss}";
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
            _cargoValueLabel!.Text = $"{_sessionTracker.TotalCargoCollected:N0} tons";
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
                _autoStartCheckbox!.Checked = preferences.AutoStartOnProspector;
                _cargoPromptCheckbox!.Checked = preferences.CargoFullPromptEnabled;
                _announcementCheckbox!.Checked = preferences.AnnouncementsEnabled;
                _autoReportCheckbox!.Checked = preferences.AutoGenerateHtmlReports;
                _idleMinutesUpDown!.Value = Math.Min(_idleMinutesUpDown.MaxValue, Math.Max(_idleMinutesUpDown.Minimum, (decimal)preferences.CargoIdleMinutesThreshold));
                var firegroupIndex = Math.Clamp(preferences.PreferredFireGroup - 1, 0, _firegroupCombo!.Items.Count - 1);
                _firegroupCombo.SelectedIndex = firegroupIndex;
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

        public void UpdateSessionHistory(IReadOnlyList<MiningSessionRecord> history)
        {
            if (_historyListView == null) return;

            _historyListView.BeginUpdate();
            _historyListView.Items.Clear();

            foreach (var record in history.OrderByDescending(r => r.SessionStart))
            {
                var item = new ListViewItem(record.SessionStart.ToString("yyyy-MM-dd HH:mm"));
                item.SubItems.Add(record.SessionDuration.ToString());
                item.SubItems.Add(record.MiningDuration.ToString());
                item.SubItems.Add(record.CreditsEarned.ToString("N0"));
                item.SubItems.Add(record.TotalCargoCollected.ToString("N0"));
                item.SubItems.Add(record.LimpetsUsed.ToString("N0"));
                item.SubItems.Add($"{record.FinalCargoFillPercent:F1}%");
                _historyListView.Items.Add(item);
            }

            _historyListView.EndUpdate();
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
                _historyListView?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
