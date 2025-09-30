using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using EliteDataRelay.Configuration;
using System.Drawing.Text;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public class TwitchChatBubbleForm : Form
    {
        private readonly System.Windows.Forms.Timer _lifeTimer;
        private readonly System.Windows.Forms.Timer _moveTimer;
        private readonly string _username;
        private readonly string _message;
        private readonly Font _userFont;
        private readonly Font _msgFont;
        private readonly List<Image> _badges;

        private int _targetY;
        private int _life = 500; // Ticks to live (~10 seconds)
        private const int ANIMATION_SPEED = 4;

        public TwitchChatBubbleForm(string username, string message, int startX, int startY, int width, List<Image> badges)
        {
            _username = username;
            _message = message;
            _badges = badges;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(startX, startY);
            DoubleBuffered = true;
            Opacity = 0; // Start transparent to prevent flicker

            // Use shared appearance settings
            this.BackColor = Color.FromArgb(255, AppConfiguration.OverlayBackgroundColor);
            _userFont = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize, FontStyle.Bold);
            _msgFont = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize, FontStyle.Regular);

            // --- Dynamic Height Calculation ---
            const int padding = 10;
            const int textPadding = 5;
            const int badgeSize = 18;
            const int badgeSpacing = 4;
            int badgesWidth = _badges.Count * (badgeSize + badgeSpacing);

            var maxTextWidth = width - (padding * 2) - badgesWidth;

            var userSize = TextRenderer.MeasureText($"{_username}:", _userFont, new Size(maxTextWidth, 0), TextFormatFlags.NoPadding);
            var msgSize = TextRenderer.MeasureText(_message, _msgFont, new Size(width - (padding * 2), 0), TextFormatFlags.WordBreak);

            var totalHeight = padding + userSize.Height + textPadding + msgSize.Height + padding;

            // Set the final size of the form
            this.Size = new Size(width, totalHeight);

            _targetY = startY;

            _lifeTimer = new System.Windows.Forms.Timer { Interval = 20 };
            _lifeTimer.Tick += LifeTimer_Tick;

            _moveTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _moveTimer.Tick += MoveTimer_Tick;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Set opacity after the form is loaded to prevent visual artifacts.
            this.Opacity = AppConfiguration.OverlayOpacity / 100.0;

            _lifeTimer.Start();
            _moveTimer.Start();
        }

        /// <summary>
        /// Moves the form smoothly to a new vertical position.
        /// </summary>
        public void SetTargetY(int y)
        {
            _targetY = y;
        }

        private void LifeTimer_Tick(object? sender, EventArgs e)
        {
            _life--;
            if (_life > 450) // Fade in
            {
                Opacity = Math.Min(AppConfiguration.OverlayOpacity / 100.0, Opacity + 0.1);
            }
            else if (_life < 50) // Fade out
            {
                Opacity = Math.Max(0.0, Opacity - 0.05); // Slower fade out
                if (Opacity == 0)
                {
                    _lifeTimer.Stop();
                    _moveTimer.Stop();
                    Close();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw a "progress" bar at the bottom indicating remaining life
            float lifePercent = Math.Max(0, _life / 500f);
            int barWidth = (int)(this.Width * lifePercent);
            using (var lifeBrush = new SolidBrush(Color.FromArgb(100, 140, 255)))
            {
                e.Graphics.FillRectangle(lifeBrush, 0, this.Height - 4, barWidth, 4);
            }

            // Draw a standard border to match other overlays
            using (var pen = new Pen(Color.FromArgb(100, 100, 100)))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1);
            }

            // --- Redesigned Two-Line Drawing Logic ---
            const int padding = 10;
            const int textPadding = 5;
            const int badgeSize = 18;
            const int badgeSpacing = 4;
            int currentX = padding;

            // 0. Draw Badges
            foreach (var badge in _badges)
            {
                e.Graphics.DrawImage(badge, currentX, padding, badgeSize, badgeSize);
                currentX += badgeSize + badgeSpacing;
            }

            // 1. Draw Username
            var userRect = new Rectangle(currentX, padding, this.Width - currentX - padding, this.Height);
            var userSize = TextRenderer.MeasureText(e.Graphics, $"{_username}:", _userFont, userRect.Size, TextFormatFlags.NoPadding);
            TextRenderer.DrawText(e.Graphics, $"{_username}:", _userFont, userRect, Color.Gold, TextFormatFlags.NoPadding);

            // 2. Draw Message below username
            var messageY = userRect.Top + userSize.Height + textPadding;
            var messageRect = new Rectangle(padding, messageY, this.Width - (padding * 2), this.Height - messageY - padding);
            TextRenderer.DrawText(e.Graphics, _message, _msgFont, messageRect, AppConfiguration.OverlayTextColor, TextFormatFlags.WordBreak | TextFormatFlags.PreserveGraphicsClipping);
        }

        private void MoveTimer_Tick(object? sender, EventArgs e)
        {
            if (this.Top == _targetY) return;

            int direction = _targetY > this.Top ? 1 : -1;
            int newY = this.Top + (direction * ANIMATION_SPEED);

            if (Math.Abs(newY - _targetY) < ANIMATION_SPEED) newY = _targetY;
            this.Top = newY;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _lifeTimer?.Dispose();
                _moveTimer?.Dispose();
                _userFont?.Dispose();
                _msgFont?.Dispose();
                foreach(var badge in _badges)
                {
                    badge.Dispose();
                }
                _badges.Clear();
            }
            base.Dispose(disposing);
        }
    }
}