using EliteDataRelay.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// A single, efficient overlay form for rendering all Twitch chat messages.
    /// </summary>
    public class TwitchChatOverlayForm : Form
    {
        private class ChatMessage
        {
            public string Username { get; }
            public string Message { get; }
            public List<Image> Badges { get; }
            public RectangleF Bounds { get; set; }
            public float TargetY { get; set; }
            public int Life { get; set; } = 500; // Ticks to live (~10 seconds)
            public float Opacity { get; set; } = 0f;

            public ChatMessage(string username, string message, List<Image> badges)
            {
                Username = username;
                Message = message;
                Badges = badges;
            }
        }

        private readonly List<ChatMessage> _messages = new List<ChatMessage>();
        private readonly System.Windows.Forms.Timer _animationTimer;
        private readonly Font _userFont;
        private readonly Font _msgFont;

        private const int PADDING = 10;
        private const int TEXT_PADDING = 5;
        private const int BADGE_SIZE = 18;
        private const int BADGE_SPACING = 4;
        private const int SPACING = 5;
        private const float ANIMATION_SPEED = 4f;

        public TwitchChatOverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(255, AppConfiguration.OverlayBackgroundColor);
            this.Opacity = AppConfiguration.OverlayOpacity / 100.0;

            _userFont = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize, FontStyle.Bold);
            _msgFont = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize, FontStyle.Regular);

            _animationTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start();
        }

        public void AddMessage(string username, string message, List<Image> badges)
        {
            var newChatMessage = new ChatMessage(username, message, badges);

            // Calculate message height
            int badgesWidth = badges.Any() ? badges.Count * (BADGE_SIZE + BADGE_SPACING) : 0;
            var maxTextWidth = this.Width - (PADDING * 2) - badgesWidth;
            var userSize = TextRenderer.MeasureText($"{username}:", _userFont, new Size(maxTextWidth, 0), TextFormatFlags.NoPadding);
            var msgSize = TextRenderer.MeasureText(message, _msgFont, new Size(this.Width - (PADDING * 2), 0), TextFormatFlags.WordBreak);
            var totalHeight = PADDING + Math.Max(BADGE_SIZE, userSize.Height) + TEXT_PADDING + msgSize.Height + PADDING;

            // Set initial position at the bottom of the overlay area
            float startY = this.Height - totalHeight;
            newChatMessage.Bounds = new RectangleF(0, startY, this.Width, totalHeight);
            newChatMessage.TargetY = startY;

            // Move existing messages up
            float yOffset = totalHeight + SPACING;
            lock (_messages)
            {
                foreach (var msg in _messages)
                {
                    msg.TargetY -= yOffset;
                }
                _messages.Add(newChatMessage);
            }
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            bool needsRedraw = false;
            lock (_messages)
            {
                if (!_messages.Any()) return;

                for (int i = _messages.Count - 1; i >= 0; i--)
                {
                    var msg = _messages[i];
                    needsRedraw = true;

                    // Animate position
                    var currentBounds = msg.Bounds;
                    if (Math.Abs(currentBounds.Y - msg.TargetY) > 1)
                    {
                        float direction = msg.TargetY > currentBounds.Y ? 1 : -1;
                        float newY = currentBounds.Y + (direction * ANIMATION_SPEED);
                        if (Math.Abs(newY - msg.TargetY) < ANIMATION_SPEED) newY = msg.TargetY;
                        msg.Bounds = new RectangleF(currentBounds.X, newY, currentBounds.Width, currentBounds.Height);
                    }

                    // Animate life and opacity
                    msg.Life--;
                    if (msg.Life > 450) // Fade in
                    {
                        msg.Opacity = Math.Min(1.0f, msg.Opacity + 0.1f);
                    }
                    else if (msg.Life < 50) // Fade out
                    {
                        msg.Opacity = Math.Max(0.0f, msg.Opacity - 0.05f);
                    }

                    if (msg.Life <= 0 || msg.Bounds.Bottom < 0)
                    {
                        _messages.RemoveAt(i);
                    }
                }
            }

            if (needsRedraw)
            {
                this.Invalidate(); // Request a repaint
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            lock (_messages)
            {
                foreach (var msg in _messages)
                {
                    if (msg.Bounds.Y > this.Height) continue; // Don't draw if off-screen

                    var g = e.Graphics;
                    var state = g.Save();
                    g.TranslateTransform(msg.Bounds.X, msg.Bounds.Y);

                    // Draw background with opacity
                    using (var bgBrush = new SolidBrush(Color.FromArgb((int)(msg.Opacity * 255), this.BackColor)))
                    {
                        g.FillRectangle(bgBrush, 0, 0, msg.Bounds.Width, msg.Bounds.Height);
                    }

                    // Draw life bar
                    float lifePercent = Math.Max(0, msg.Life / 500f);
                    int barWidth = (int)(msg.Bounds.Width * lifePercent);
                    using (var lifeBrush = new SolidBrush(Color.FromArgb((int)(msg.Opacity * 100), 140, 255)))
                    {
                        g.FillRectangle(lifeBrush, 0, msg.Bounds.Height - 4, barWidth, 4);
                    }

                    // Draw border
                    using (var pen = new Pen(Color.FromArgb((int)(msg.Opacity * 100), 100, 100)))
                    {
                        g.DrawRectangle(pen, 0, 0, msg.Bounds.Width - 1, msg.Bounds.Height - 1);
                    }

                    // --- Draw Content ---
                    int currentX = PADDING;
                    if (msg.Badges.Any())
                    {
                        foreach (var badge in msg.Badges)
                        {
                            g.DrawImage(badge, currentX, PADDING, BADGE_SIZE, BADGE_SIZE);
                            currentX += BADGE_SIZE + BADGE_SPACING;
                        }
                    }

                    var userRect = new Rectangle(currentX, PADDING, (int)msg.Bounds.Width - currentX - PADDING, (int)msg.Bounds.Height);
                    var userSize = TextRenderer.MeasureText(g, $"{msg.Username}:", _userFont, userRect.Size, TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(g, $"{msg.Username}:", _userFont, userRect, Color.FromArgb((int)(msg.Opacity * 255), Color.Gold), TextFormatFlags.NoPadding);

                    var messageY = userRect.Top + userSize.Height + TEXT_PADDING;
                    var messageRect = new Rectangle(PADDING, messageY, (int)msg.Bounds.Width - (PADDING * 2), (int)msg.Bounds.Height - messageY - PADDING);
                    TextRenderer.DrawText(g, msg.Message, _msgFont, messageRect, Color.FromArgb((int)(msg.Opacity * 255), AppConfiguration.OverlayTextColor), TextFormatFlags.WordBreak | TextFormatFlags.PreserveGraphicsClipping);

                    g.Restore(state);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Dispose();
                _userFont?.Dispose();
                _msgFont?.Dispose();
                lock (_messages)
                {
                    foreach (var msg in _messages)
                    {
                        foreach (var badge in msg.Badges) badge.Dispose();
                    }
                    _messages.Clear();
                }
            }
            base.Dispose(disposing);
        }
    }
}