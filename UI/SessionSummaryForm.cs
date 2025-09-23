using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class SessionSummaryForm : Form
    {
        // Fields for making the borderless form draggable
        private bool _isDragging;
        private Point _dragStartPoint = Point.Empty;

        // UI Controls
        private Panel _pnlTitleBar = null!;
        private Label _lblTitle = null!;
        private Button _btnClose = null!;
        private TableLayoutPanel _tlpStats = null!;
        private Label _lblDurationValue = null!;
        private Label _lblCargoCollectedValue = null!;
        private Label _lblCreditsEarnedValue = null!;
        private readonly SessionTrackingService _sessionTracker;

        public SessionSummaryForm(SessionTrackingService sessionTracker)
        {
            _sessionTracker = sessionTracker;
            InitializeComponent();
            _sessionTracker.SessionUpdated += OnSessionUpdated;
            this.FormClosing += OnFormClosing;
        }

        private void InitializeComponent()
        {
            // Form Properties
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.Gainsboro;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;
            this.ClientSize = new Size(350, 155);
            this.DoubleBuffered = true;

            // Fonts
            var titleFont = new Font("Verdana", 10F, FontStyle.Bold);
            var headerFont = new Font("Verdana", 10F, FontStyle.Regular);
            var valueFont = new Font("Consolas", 11F, FontStyle.Bold);

            // Custom Title Bar
            _pnlTitleBar = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Color.FromArgb(45, 45, 48) };
            _lblTitle = new Label { Text = "Session Summary", Font = titleFont, Location = new Point(10, 6), AutoSize = true };
            _btnClose = new Button
            {
                Text = "X",
                Font = new Font("Consolas", 9F, FontStyle.Bold),
                ForeColor = Color.Gainsboro,
                BackColor = _pnlTitleBar.BackColor,
                Size = new Size(30, 30),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0, MouseDownBackColor = Color.FromArgb(100, 100, 100), MouseOverBackColor = Color.FromArgb(63, 63, 70) }
            };
            _btnClose.Click += (s, e) => this.Close();
            _pnlTitleBar.Controls.Add(_lblTitle);
            _pnlTitleBar.Controls.Add(_btnClose);

            // Add dragging events to title bar and label
            _pnlTitleBar.MouseDown += TitleBar_MouseDown;
            _pnlTitleBar.MouseUp += TitleBar_MouseUp;
            _pnlTitleBar.MouseMove += TitleBar_MouseMove;
            _lblTitle.MouseDown += TitleBar_MouseDown;
            _lblTitle.MouseUp += TitleBar_MouseUp;
            _lblTitle.MouseMove += TitleBar_MouseMove;

            // Stats Layout Panel
            _tlpStats = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                ColumnCount = 2,
                RowCount = 3
            };
            _tlpStats.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _tlpStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 3; i++)
            {
                _tlpStats.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            }

            // Stat Labels
            var lblDurationHeader = new Label { Text = "Duration:", Font = headerFont, ForeColor = Color.Silver, Anchor = AnchorStyles.Left, AutoSize = true };
            _lblDurationValue = new Label { Text = "00:00:00", Font = valueFont, ForeColor = Color.Orange, Anchor = AnchorStyles.Right, AutoSize = true };

            var lblCargoCollectedHeader = new Label { Text = "Cargo Collected:", Font = headerFont, ForeColor = Color.Silver, Anchor = AnchorStyles.Left, AutoSize = true };
            _lblCargoCollectedValue = new Label { Text = "0 units", Font = valueFont, ForeColor = Color.Orange, Anchor = AnchorStyles.Right, AutoSize = true };

            var lblCreditsEarnedHeader = new Label { Text = "Credits Earned:", Font = headerFont, ForeColor = Color.Silver, Anchor = AnchorStyles.Left, AutoSize = true };
            _lblCreditsEarnedValue = new Label { Text = "0 CR", Font = valueFont, ForeColor = Color.Orange, Anchor = AnchorStyles.Right, AutoSize = true };

            // Add labels to TableLayoutPanel
            _tlpStats.Controls.Add(lblDurationHeader, 0, 0);
            _tlpStats.Controls.Add(_lblDurationValue, 1, 0);
            _tlpStats.Controls.Add(lblCargoCollectedHeader, 0, 1);
            _tlpStats.Controls.Add(_lblCargoCollectedValue, 1, 1);
            _tlpStats.Controls.Add(lblCreditsEarnedHeader, 0, 2);
            _tlpStats.Controls.Add(_lblCreditsEarnedValue, 1, 2);

            // Add controls to form
            this.Controls.Add(_tlpStats);
            this.Controls.Add(_pnlTitleBar);

            UpdateLabels();
        }

        #region Draggable Form
        private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragStartPoint = new Point(e.X, e.Y);
            }
        }

        private void TitleBar_MouseUp(object? sender, MouseEventArgs e)
        {
            _isDragging = false;
        }

        private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point p = PointToScreen(e.Location);
                Location = new Point(p.X - this._dragStartPoint.X, p.Y - this._dragStartPoint.Y);
            }
        }
        #endregion

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Draw a border to match the overlay aesthetic
            using (var pen = new Pen(Color.FromArgb(100, 100, 100)))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1);
            }
        }

        private void OnSessionUpdated(object? sender, EventArgs e)
        {
            if (this.IsDisposed) return;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateLabels));
            }
            else
            {
                UpdateLabels();
            }
        }

        private void UpdateLabels()
        {
            _lblDurationValue.Text = $"{_sessionTracker.SessionDuration:hh\\:mm\\:ss}";
            _lblCargoCollectedValue.Text = $"{_sessionTracker.TotalCargoCollected} units";
            _lblCreditsEarnedValue.Text = $"{_sessionTracker.CreditsEarned:N0} CR";
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            // Instead of closing, just hide the window. It can be re-shown from the main form.
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sessionTracker.SessionUpdated -= OnSessionUpdated;
                // The form will dispose of its child controls, so we don't need to do it manually.
            }
            base.Dispose(disposing);
        }
    }
}