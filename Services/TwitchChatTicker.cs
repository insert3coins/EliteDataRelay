using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Manages the state and drawing of a scrolling chat ticker.
    /// </summary>
    public class TwitchChatTicker : IDisposable
    {
        private class TickerMessage
        {
            public string Username { get; }
            public string Message { get; }
            public string FullText => $"{Username}: {Message}";
            public SizeF MeasuredSize { get; set; }
            public float UsernameWidth { get; set; }
            public float CurrentX { get; set; }
            public int Life { get; set; } = 600; // Ticks to live (e.g., 10 seconds at 60fps)

            public TickerMessage(string username, string message)
            {
                Username = username;
                Message = message;
            }
        }

        private readonly List<TickerMessage> _messages = new List<TickerMessage>();
        private readonly System.Windows.Forms.Timer _animationTimer;
        private readonly Font _userFont;
        private readonly Font _msgFont;
        private readonly Graphics _measurementGraphics;
        private readonly Image _dummyImage = new Bitmap(1, 1);
        private readonly Control _targetControl;

        public TwitchChatTicker(Control targetControl)
        {
            _targetControl = targetControl;
            _animationTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 FPS
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start();

            // Cache font objects to avoid creating them 60 times per second.
            _userFont = new Font("Verdana", 10, FontStyle.Bold);
            _msgFont = new Font("Verdana", 10, FontStyle.Regular);
            // Create a graphics object for text measurement that doesn't depend on the paint event.
            _measurementGraphics = Graphics.FromImage(_dummyImage);
        }

        public void AddMessage(string username, string message)
        {
            lock (_messages)
            {
                var newMessage = new TickerMessage(username, message);

                // Pre-measure the text here instead of in the Draw method.
                // This moves the measurement cost off the critical paint path.
                var userSize = _measurementGraphics.MeasureString($"{newMessage.Username}: ", _userFont);
                var msgSize = _measurementGraphics.MeasureString(newMessage.Message, _msgFont);
                newMessage.UsernameWidth = userSize.Width;
                newMessage.MeasuredSize = new SizeF(userSize.Width + msgSize.Width, userSize.Height);

                // Position the new message to the right of the last one, or at the start.
                float startX = _targetControl.Width;
                if (_messages.Any())
                {
                    var lastMessage = _messages.Last();
                    startX = lastMessage.CurrentX + lastMessage.MeasuredSize.Width + 30; // 30px spacing
                }
                newMessage.CurrentX = startX;

                _messages.Add(newMessage);
            }
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            bool needsRedraw = false;
            lock (_messages)
            {
                if (_messages.Any())
                {
                    // Update positions and lifetimes
                    for (int i = _messages.Count - 1; i >= 0; i--)
                    {
                        var msg = _messages[i];
                        msg.CurrentX -= 2; // Scroll speed
                        msg.Life--;

                        if (msg.Life <= 0)
                        {
                            _messages.RemoveAt(i);
                        }
                    }
                    needsRedraw = true;
                }
            }

            if (needsRedraw)
            {
                _targetControl.Invalidate();
            }
        }

        public void Draw(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            lock (_messages)
            {
                foreach (var msg in _messages)
                {
                    // Don't draw if it's completely off-screen
                    if (msg.CurrentX + msg.MeasuredSize.Width < 0 || msg.CurrentX > _targetControl.Width)
                    {
                        continue;
                    }

                    // Use the pre-measured sizes for drawing.
                    float y = _targetControl.Height - msg.MeasuredSize.Height - 5; // 5px from bottom
                    g.DrawString($"{msg.Username}: ", _userFont, Brushes.Gold, msg.CurrentX, y);
                    g.DrawString(msg.Message, _msgFont, Brushes.White, msg.CurrentX + msg.UsernameWidth, y);
                }
            }
        }

        public void Dispose()
        {
            _animationTimer?.Stop();
            _animationTimer?.Dispose();
            _userFont?.Dispose();
            _msgFont?.Dispose();
            _measurementGraphics?.Dispose();
            _dummyImage?.Dispose();
        }
    }
}