using EliteDataRelay.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public class MiningSessionPanel : UserControl
    {
        private readonly SessionTrackingService _sessionTracker;
        private readonly FontManager _fontManager;

        // UI Colors
        private readonly Color eliteOrange = Color.FromArgb(255, 136, 0);
        private readonly Color eliteOrangeLight = Color.FromArgb(255, 153, 68);
        private readonly Color eliteGreen = Color.FromArgb(0, 255, 0);

        private Button? _startSessionButton;
        private Button? _stopSessionButton;
        private System.Windows.Forms.Timer? _updateTimer;

        // Basic labels for stats
        private TableLayoutPanel? _statsTable;
        private Label? _limpetsValueLabel;
        private Label? _refinedValueLabel;
        private Label? _durationValueLabel;

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
            this.BackColor = Color.FromArgb(10, 10, 10);
            this.Font = new Font("Consolas", 10F, FontStyle.Regular);
            this.DoubleBuffered = true;

            this.Resize += OnPanelResize;

            SetupButtons();
            SetupStatsLabels();
            
            // Timer to update duration labels in real-time
            _updateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _updateTimer.Tick += (s, e) => {
                if (_sessionTracker.IsMiningSessionActive) UpdateStats();
            };

            UpdateControlsVisibility();
        }

        private void SetupButtons()
        {
            _startSessionButton = new Button
            {
                Text = "START MINING SESSION",
                Font = new Font("Consolas", 14F, FontStyle.Bold),
                ForeColor = eliteOrange,
                BackColor = Color.FromArgb(0, 10, 20),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(240, 60),
                Cursor = Cursors.Hand,
            };
            _startSessionButton.FlatAppearance.BorderColor = eliteOrange;
            _startSessionButton.FlatAppearance.BorderSize = 2;
            _startSessionButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 255, 136, 0);
            _startSessionButton.Click += (s, e) => StartMiningClicked?.Invoke(this, EventArgs.Empty);

            _stopSessionButton = new Button
            {
                Text = "Stop Session",
                Font = _fontManager.ConsolasFont,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlatStyle = FlatStyle.Flat,
                ForeColor = eliteOrange,
                BackColor = Color.Black,
            };
            _stopSessionButton.FlatAppearance.BorderColor = eliteOrange;
            _stopSessionButton.FlatAppearance.BorderSize = 1;
            _stopSessionButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 255, 136, 0);
            _stopSessionButton.Click += (s, e) => StopMiningClicked?.Invoke(this, EventArgs.Empty);

            this.Controls.Add(_startSessionButton);
            this.Controls.Add(_stopSessionButton);
        }

        private void SetupStatsLabels()
        {
            _statsTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(30),
                Visible = false // Initially hidden
            };

            _statsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _statsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            for (int i = 0; i < 3; i++)
            {
                _statsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            }

            var headerFont = new Font("Consolas", 14F, FontStyle.Bold);
            var valueFont = new Font("Consolas", 14F, FontStyle.Regular);

            // Create labels
            _limpetsValueLabel = CreateValueLabel(valueFont);
            _refinedValueLabel = CreateValueLabel(valueFont);
            _durationValueLabel = CreateValueLabel(valueFont);

            // Add to table
            _statsTable.Controls.Add(CreateHeaderLabel("Limpets Used:", headerFont), 0, 0);
            _statsTable.Controls.Add(_limpetsValueLabel, 1, 0);
            _statsTable.Controls.Add(CreateHeaderLabel("Refined:", headerFont), 0, 1);
            _statsTable.Controls.Add(_refinedValueLabel, 1, 1);
            _statsTable.Controls.Add(CreateHeaderLabel("Duration:", headerFont), 0, 2);
            _statsTable.Controls.Add(_durationValueLabel, 1, 2);

            this.Controls.Add(_statsTable);
        }

        private Label CreateHeaderLabel(string text, Font font) => new Label { Text = text, Font = font, ForeColor = eliteOrange, Anchor = AnchorStyles.Left, AutoSize = true };
        private Label CreateValueLabel(Font font) => new Label { Text = "0", Font = font, ForeColor = Color.White, Anchor = AnchorStyles.Right, AutoSize = true };

        public void UpdateStats()
        {
            if (this.IsHandleCreated)
            {
                UpdateControlsVisibility();
                if (_sessionTracker.IsMiningSessionActive)
                {
                    var duration = _sessionTracker.MiningDuration;

                    if (_limpetsValueLabel != null) _limpetsValueLabel.Text = $"{_sessionTracker.LimpetsUsed}";
                    if (_refinedValueLabel != null) _refinedValueLabel.Text = $"{_sessionTracker.TotalRefinedCount} tons";
                    if (_durationValueLabel != null) _durationValueLabel.Text = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                }
            }
        }

        private void UpdateControlsVisibility()
        {
            bool sessionActive = _sessionTracker.IsMiningSessionActive;
            if (_startSessionButton != null) _startSessionButton.Visible = !sessionActive;
            if (_stopSessionButton != null) _stopSessionButton.Visible = sessionActive;
            if (_statsTable != null) _statsTable.Visible = sessionActive;

            // Start or stop the real-time update timer based on session state
            if (sessionActive && _updateTimer?.Enabled == false)
            {
                _updateTimer.Start();
            }
            else if (!sessionActive && _updateTimer?.Enabled == true)
            {
                _updateTimer.Stop();
            }
        }

        private void OnPanelResize(object? sender, EventArgs e) => PositionControls();

        private void PositionControls()
        {
            if (_startSessionButton != null)
            {
                _startSessionButton.Location = new Point(
                    (this.ClientSize.Width - _startSessionButton.Width) / 2,
                    (this.ClientSize.Height - _startSessionButton.Height) / 2);
            }
            if (_stopSessionButton != null)
            {
                _stopSessionButton.Location = new Point(
                    this.ClientSize.Width - _stopSessionButton.Width - 10,
                    this.ClientSize.Height - _stopSessionButton.Height - 10);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _startSessionButton?.Dispose();
                _stopSessionButton?.Dispose();
                _statsTable?.Dispose();
                _updateTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}