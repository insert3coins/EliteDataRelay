using System;
using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public class SessionSummaryForm : Form
    {
        private Label _lblDuration = null!;
        private Label _lblCargoCollected = null!;
        private Label _lblCargoPerHour = null!;
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
            this.Text = "Session Summary";
            this.ClientSize = new Size(300, 120);
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;

            var font = new Font("Verdana", 10F);

            _lblDuration = new Label { Location = new Point(15, 15), AutoSize = true, Font = font };
            _lblCargoCollected = new Label { Location = new Point(15, 45), AutoSize = true, Font = font };
            _lblCargoPerHour = new Label { Location = new Point(15, 75), AutoSize = true, Font = font };

            this.Controls.Add(_lblDuration);
            this.Controls.Add(_lblCargoCollected);
            this.Controls.Add(_lblCargoPerHour);

            UpdateLabels();
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
            _lblDuration.Text = $"Session Duration: {_sessionTracker.SessionDuration:hh\\:mm\\:ss}";
            _lblCargoCollected.Text = $"Total Cargo Collected: {_sessionTracker.TotalCargoCollected} units";
            _lblCargoPerHour.Text = $"Cargo Per Hour: {_sessionTracker.CargoPerHour:F1} units/hr";
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
            if (disposing) { _sessionTracker.SessionUpdated -= OnSessionUpdated; }
            base.Dispose(disposing);
        }
    }
}