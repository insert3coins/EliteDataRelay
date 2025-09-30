using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public class TwitchAlertForm : Form
    {
        private readonly System.Windows.Forms.Timer _fadeTimer;
        private readonly string _line1;
        private readonly string _line2;
        private int _life = 200; // Time to stay on screen in timer ticks

        public TwitchAlertForm(string line1, string line2)
        {
            _line1 = line1;
            _line2 = line2;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(255, 20, 0, 30);
            Opacity = 0;
            DoubleBuffered = true;

            _fadeTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _fadeTimer.Tick += FadeTimer_Tick;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _fadeTimer.Start();
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            _life--;

            if (_life > 150) // Fade In
            {
                Opacity = Math.Min(1.0, Opacity + 0.05);
            }
            else if (_life < 50) // Fade Out
            {
                Opacity = Math.Max(0.0, Opacity - 0.05);
                if (Opacity == 0)
                {
                    _fadeTimer.Stop();
                    Close();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var headerFont = new Font("Verdana", 14, FontStyle.Bold);
            using var userFont = new Font("Verdana", 18, FontStyle.Bold);
            using var brush = new SolidBrush(Color.White);
            using var stringFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            var headerRect = new Rectangle(0, 10, Width, 30);
            var userRect = new Rectangle(0, 40, Width, 40);

            e.Graphics.DrawString(_line1, headerFont, brush, headerRect, stringFormat);
            e.Graphics.DrawString(_line2, userFont, Brushes.Gold, userRect, stringFormat);

            // Draw border
            using var pen = new Pen(Color.FromArgb(100, 140, 255), 2);
            e.Graphics.DrawRectangle(pen, 1, 1, Width - 2, Height - 2);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fadeTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}