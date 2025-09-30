using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public class TwitchChatBubbleForm : Form
    {
        private readonly System.Windows.Forms.Timer _lifeTimer;
        private readonly string _username;
        private readonly string _message;

        public TwitchChatBubbleForm(string username, string message)
        {
            _username = username;
            _message = message;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Magenta; // Will be made transparent
            TransparencyKey = Color.Magenta;
            DoubleBuffered = true;
            Opacity = 0;

            _lifeTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _lifeTimer.Tick += LifeTimer_Tick;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _lifeTimer.Start();
        }

        private int _life = 400; // Ticks to live
        private void LifeTimer_Tick(object? sender, EventArgs e)
        {
            _life--;
            if (_life > 350) // Fade in
            {
                Opacity = Math.Min(1.0, Opacity + 0.1);
            }
            else if (_life < 50) // Fade out
            {
                Opacity = Math.Max(0.0, Opacity - 0.1);
                if (Opacity == 0)
                {
                    _lifeTimer.Stop();
                    Close();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var bubbleRect = new Rectangle(0, 0, Width - 1, Height - 11);
            using var path = GetBubblePath(bubbleRect, 10);

            using var brush = new SolidBrush(Color.FromArgb(220, 30, 30, 50));
            e.Graphics.FillPath(brush, path);
            using var pen = new Pen(Color.FromArgb(100, 140, 255), 2);
            e.Graphics.DrawPath(pen, path);

            using var userFont = new Font("Verdana", 9, FontStyle.Bold);
            using var msgFont = new Font("Verdana", 9, FontStyle.Regular);

            e.Graphics.DrawString(_username, userFont, Brushes.Gold, new PointF(10, 8));
            var userSize = e.Graphics.MeasureString(_username, userFont);
            var messageRect = new RectangleF(10, userSize.Height + 5, bubbleRect.Width - 20, bubbleRect.Height - userSize.Height - 15);
            e.Graphics.DrawString(_message, msgFont, Brushes.White, messageRect);
        }

        private static GraphicsPath GetBubblePath(RectangleF rect, float tailSize)
        {
            var path = new GraphicsPath();
            float radius = 10;
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddLine(rect.Right - radius, rect.Bottom, rect.Left + rect.Width / 2 + tailSize, rect.Bottom);
            path.AddLine(rect.Left + rect.Width / 2 + tailSize, rect.Bottom, rect.Left + rect.Width / 2, rect.Bottom + tailSize);
            path.AddLine(rect.Left + rect.Width / 2, rect.Bottom + tailSize, rect.Left + rect.Width / 2 - tailSize, rect.Bottom);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _lifeTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}