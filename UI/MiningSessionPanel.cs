using EliteDataRelay.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        private System.Windows.Forms.Timer? pulseTimer;
        private float pulseValue = 1.0f;
        private bool pulseDirection = false;
        private Button? _startSessionButton;
        private Button? _stopSessionButton;

        public event EventHandler? StartMiningClicked;
        public event EventHandler? StopMiningClicked;

        public MiningSessionPanel(SessionTrackingService sessionTracker, FontManager fontManager)
        {
            _sessionTracker = sessionTracker;
            _fontManager = fontManager;
            InitializeComponent();
            SetupPulseAnimation();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.Black;
            this.Font = new Font("Consolas", 10F, FontStyle.Regular);
            this.DoubleBuffered = true;

            this.Paint += MiningUI_Paint;
            this.Resize += OnPanelResize;

            SetupButtons();
            // Set initial visibility based on current session state
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

        public void UpdateStats()
        {
            if (this.IsHandleCreated)
            {
                UpdateControlsVisibility();
                this.Invalidate();
            }
        }

        private void SetupPulseAnimation()
        {
            pulseTimer = new System.Windows.Forms.Timer();
            pulseTimer.Interval = 50;
            pulseTimer.Tick += (s, e) =>
            {
                if (pulseDirection)
                {
                    pulseValue += 0.05f;
                    if (pulseValue >= 1.0f)
                    {
                        pulseValue = 1.0f;
                        pulseDirection = false;
                    }
                }
                else
                {
                    pulseValue -= 0.05f;
                    if (pulseValue <= 0.4f)
                    {
                        pulseValue = 0.4f;
                        pulseDirection = true;
                    }
                }
                this.Invalidate();
            };
            pulseTimer.Start();
        }

        private void UpdateControlsVisibility()
        {
            if (_startSessionButton != null)
            {
                _startSessionButton.Visible = !_sessionTracker.IsMiningSessionActive;
            }
            if (_stopSessionButton != null)
            {
                _stopSessionButton.Visible = _sessionTracker.IsMiningSessionActive;
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
                // Position stop button at the bottom right, respecting the scaled UI margin
                float scale = Math.Min(this.ClientSize.Width / 800f, this.ClientSize.Height / 600f);
                float xOffset = (this.ClientSize.Width - (800f * scale)) / 2;
                float yOffset = (this.ClientSize.Height - (600f * scale)) / 2;
                int margin = (int)(30 * scale);

                _stopSessionButton.Location = new Point(
                    (int)(xOffset + (800f * scale) - margin - _stopSessionButton.Width),
                    (int)(yOffset + (600f * scale) - margin - _stopSessionButton.Height));
            }
        }

        private void MiningUI_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            if (!_sessionTracker.IsMiningSessionActive)
            {
                return;
            }

            // Define margins and gaps based on a proportion of the control's size
            int margin = (int)(this.ClientSize.Width * 0.03f);
            int gap = (int)(this.ClientSize.Width * 0.02f);

            // Main interface panel
            Rectangle mainRect = new Rectangle(margin, margin, this.ClientSize.Width - (margin * 2), this.ClientSize.Height - (margin * 2));
            DrawMainPanel(g, mainRect);

            // Header
            DrawHeader(g, mainRect);

            // Main profit display
            int headerHeight = (int)(mainRect.Height * 0.18f);
            int profitBoxHeight = (int)(mainRect.Height * 0.2f);
            Rectangle profitRect = new Rectangle(mainRect.X + gap, mainRect.Y + headerHeight, mainRect.Width - (gap * 2), profitBoxHeight);
            DrawStatBox(g, profitRect, "◢ TOTAL MINING PROFIT ◣", $"{_sessionTracker.MiningProfit:N0} CR", true);

            // Stats grid
            int statY = profitRect.Bottom + gap;
            int statWidth = (mainRect.Width - (gap * 3)) / 2;
            int statHeight = (mainRect.Height - headerHeight - profitBoxHeight - (_stopSessionButton!.Height + (gap * 4))) / 2;
            statHeight = Math.Max(40, statHeight); // Ensure a minimum height

            DrawStatBox(g, new Rectangle(mainRect.X + gap, statY, statWidth, statHeight),
                "▸ LIMPETS USED", $"{_sessionTracker.LimpetsUsed} units", false);
            DrawStatBox(g, new Rectangle(mainRect.X + 30 + statWidth + gap, statY, statWidth, statHeight),
                "▸ REFINED", $"{_sessionTracker.TotalRefinedCount} tons", false);

            statY += statHeight + gap;
            var duration = _sessionTracker.MiningDuration;
            DrawStatBox(g, new Rectangle(mainRect.X + gap, statY, statWidth, statHeight),
                "▸ DURATION", $"{duration.Hours}:{duration.Minutes:D2} h", false);

            double profitPerHour = duration.TotalHours > 0 ? _sessionTracker.MiningProfit / duration.TotalHours : 0;
            DrawStatBox(g, new Rectangle(mainRect.X + 30 + statWidth + gap, statY, statWidth, statHeight),
                "▸ PROFIT/HOUR", $"{profitPerHour / 1000000:F1}M CR/h", false);
        }

        private void DrawMainPanel(Graphics g, Rectangle rect)
        {
            // Gradient background
            using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                rect, Color.FromArgb(240, 0, 10, 20), Color.FromArgb(250, 0, 5, 15), 135f))
            {
                g.FillRectangle(bgBrush, rect);
            }

            // Grid pattern
            using (Pen gridPen = new Pen(Color.FromArgb(8, 255, 136, 0)))
            {
                for (int x = rect.X; x < rect.Right; x += 20)
                    g.DrawLine(gridPen, x, rect.Y, x, rect.Bottom);
                for (int y = rect.Y; y < rect.Bottom; y += 20)
                    g.DrawLine(gridPen, rect.X, y, rect.Right, y);
            }

            // Border
            using (Pen borderPen = new Pen(eliteOrange, 2))
            {
                g.DrawRectangle(borderPen, rect);
            }

            // Glow effect
            using (Pen glowPen = new Pen(Color.FromArgb(50, eliteOrange), 8))
            {
                g.DrawRectangle(glowPen, rect);
            }
        }

        private void DrawHeader(Graphics g, Rectangle mainRect)
        {
            // Use fixed font sizes for consistency
            using Font titleFont = new Font("Consolas", 20F, FontStyle.Bold);

            string title = "SESSION ANALYTICS";
            SizeF titleSize = g.MeasureString(title, titleFont);

            // Calculate the vertical center of the header area (top of panel to the separator line)
            int headerHeight = (int)(mainRect.Height * 0.18f);
            float titleY = mainRect.Y + (headerHeight / 2) - (titleSize.Height / 2);

            PointF titlePos = new PointF(mainRect.X + (mainRect.Width - titleSize.Width) / 2, titleY);

            // Title glow
            using (Brush glowBrush = new SolidBrush(Color.FromArgb(80, eliteOrange)))
            {
                g.DrawString(title, titleFont, glowBrush, titlePos.X + 1, titlePos.Y + 1);
            }
            using (Brush titleBrush = new SolidBrush(eliteOrange))
            {
                g.DrawString(title, titleFont, titleBrush, titlePos);
            }

            // Separator line
            int lineY = mainRect.Y + (int)(mainRect.Height * 0.18f) - 1; // Position it just above the profit box
            using (Pen separatorPen = new Pen(eliteOrange, 1))
            {
                g.DrawLine(separatorPen, mainRect.X + (mainRect.Width * 0.05f), lineY, mainRect.Right - (mainRect.Width * 0.05f), lineY);
            }
        }

        private void DrawStatBox(Graphics g, Rectangle rect, string label, string value, bool isMain)
        {
            // Background
            Color bgColor = isMain ? Color.FromArgb(13, eliteOrange) : Color.FromArgb(153, 0, 0, 0);
            using (SolidBrush bgBrush = new SolidBrush(bgColor))
            {
                g.FillRectangle(bgBrush, rect);
            }

            // Border
            int borderWidth = isMain ? 2 : 1;
            using (Pen borderPen = new Pen(eliteOrange, borderWidth))
            {
                g.DrawRectangle(borderPen, rect);
            }

            // Left accent line
            using (Pen accentPen = new Pen(eliteOrange, 3))
            {
                g.DrawLine(accentPen, rect.X, rect.Y, rect.X, rect.Bottom);
            }

            // Label
            using Font labelFont = new Font("Consolas", isMain ? 12F : 10F, FontStyle.Regular);
            using Font valueFont = new Font("Consolas", isMain ? 18F : 14F, FontStyle.Bold);
            using StringFormat leftAlign = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            using StringFormat rightAlign = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
            using Brush labelBrush = new SolidBrush(eliteOrangeLight);
            using Brush valueBrush = new SolidBrush(Color.White); // Use white for data for better contrast

            // Use a two-column layout for all stat boxes for consistency and to prevent overlap.
            RectangleF textBounds = new RectangleF(rect.X + 15, rect.Y, rect.Width - 30, rect.Height);

            if (isMain)
            {
                // For the main profit box, use a more descriptive label.
                label = "TOTAL PROFIT:";
            }

            if (true) // Apply to all boxes
            {
                // Draw Label on the left
                g.DrawString(label, labelFont, labelBrush, textBounds, leftAlign);

                // Draw Value on the right
                g.DrawString(value, valueFont, valueBrush, textBounds, rightAlign);
            }
        }

        private void DrawCornerDecorations(Graphics g, Rectangle rect)
        {
            int size = (int)(rect.Width * 0.03f);
            using (Pen cornerPen = new Pen(eliteOrange, 2))
            {
                // Top-left
                g.DrawLine(cornerPen, rect.X, rect.Y, rect.X + size, rect.Y);
                g.DrawLine(cornerPen, rect.X, rect.Y, rect.X, rect.Y + size);

                // Top-right
                g.DrawLine(cornerPen, rect.Right - size, rect.Y, rect.Right, rect.Y);
                g.DrawLine(cornerPen, rect.Right, rect.Y, rect.Right, rect.Y + size);

                // Bottom-left
                g.DrawLine(cornerPen, rect.X, rect.Bottom, rect.X + size, rect.Bottom);
                g.DrawLine(cornerPen, rect.X, rect.Bottom - size, rect.X, rect.Bottom);

                // Bottom-right
                g.DrawLine(cornerPen, rect.Right - size, rect.Bottom, rect.Right, rect.Bottom);
                g.DrawLine(cornerPen, rect.Right, rect.Bottom - size, rect.Right, rect.Bottom);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pulseTimer?.Stop();
                pulseTimer?.Dispose();
                _startSessionButton?.Dispose();
                _stopSessionButton?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}