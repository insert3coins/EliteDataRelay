using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace EliteDataRelay.UI
{
    public static class MiningUIRenderer
    {
        // UI Colors
        private static readonly Color eliteOrange = Color.FromArgb(255, 136, 0);
        private static readonly Color eliteOrangeLight = Color.FromArgb(255, 170, 68);
        private static readonly Color eliteGreen = Color.FromArgb(0, 255, 0);

        public static void Paint(Graphics g, MiningUIData data)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Main interface panel - centered with margins
            Rectangle mainRect = new Rectangle(30, 30, 740, 540);
            DrawMainPanel(g, mainRect);

            // Header
            DrawHeader(g, mainRect);

            // Stats grid - 2x2 layout with better spacing
            int statY = mainRect.Y + 130;
            int statWidth = (mainRect.Width - 90) / 2;
            int statHeight = 100;
            int gap = 30;

            DrawStatBox(g, new Rectangle(mainRect.X + 30, statY, statWidth, statHeight),
                "▸ LIMPETS USED", $"{data.LimpetsUsed} units", false);

            DrawStatBox(g, new Rectangle(mainRect.X + 30, statY + statHeight + gap, statWidth, statHeight),
                "▸ DURATION", $"{data.Duration.Hours}:{data.Duration.Minutes:D2} h", false);

            DrawStatBox(g, new Rectangle(mainRect.X + 30 + statWidth + gap, statY, statWidth, statHeight),
                "▸ REFINED", $"{data.TonsRefined} tons", false);

            // Status bar
            DrawStatusBar(g, new Rectangle(mainRect.X + 30, mainRect.Bottom - 50, mainRect.Width - 60, 25), data.PulseValue, data.IsSessionActive);

            // Corner decorations
            DrawCornerDecorations(g, mainRect);
        }

        private static void DrawMainPanel(Graphics g, Rectangle rect)
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

        private static void DrawHeader(Graphics g, Rectangle mainRect)
        {
            using Font titleFont = new Font("Consolas", 22F, FontStyle.Bold);
            using Font subtitleFont = new Font("Consolas", 10F, FontStyle.Regular);

            string title = "MINING OPERATIONS";
            SizeF titleSize = g.MeasureString(title, titleFont);
            PointF titlePos = new PointF(mainRect.X + (mainRect.Width - titleSize.Width) / 2, mainRect.Y + 25);

            // Title glow
            using (Brush glowBrush = new SolidBrush(Color.FromArgb(80, eliteOrange)))
            {
                g.DrawString(title, titleFont, glowBrush, titlePos.X + 1, titlePos.Y + 1);
            }
            using (Brush titleBrush = new SolidBrush(eliteOrange))
            {
                g.DrawString(title, titleFont, titleBrush, titlePos);
            }

            string subtitle = "SESSION ANALYTICS";
            SizeF subtitleSize = g.MeasureString(subtitle, subtitleFont);
            PointF subtitlePos = new PointF(mainRect.X + (mainRect.Width - subtitleSize.Width) / 2, titlePos.Y + titleSize.Height + 5);
            using (Brush subtitleBrush = new SolidBrush(eliteOrangeLight))
            {
                g.DrawString(subtitle, subtitleFont, subtitleBrush, subtitlePos);
            }

            // Separator line
            int lineY = (int)(subtitlePos.Y + subtitleSize.Height + 15);
            using (Pen separatorPen = new Pen(eliteOrange, 1))
            {
                g.DrawLine(separatorPen, mainRect.X + 30, lineY, mainRect.Right - 30, lineY);
            }
        }

        private static void DrawStatBox(Graphics g, Rectangle rect, string label, string value, bool isMain)
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
            using Font labelFont = new Font("Consolas", isMain ? 11F : 9F, FontStyle.Regular);
            using (Brush labelBrush = new SolidBrush(eliteOrangeLight))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(label, labelFont, labelBrush,
                    new RectangleF(rect.X, rect.Y + 15, rect.Width, 25), sf);
            }

            // Value
            using Font valueFont = new Font("Consolas", isMain ? 28F : 22F, FontStyle.Bold);
            SizeF valueSize = g.MeasureString(value, valueFont);
            PointF valuePos = new PointF(
                rect.X + (rect.Width - valueSize.Width) / 2,
                rect.Y + rect.Height / 2 - valueSize.Height / 2 + (isMain ? 8 : 5)
            );

            // Value glow
            using (Brush glowBrush = new SolidBrush(Color.FromArgb(100, eliteOrange)))
            {
                g.DrawString(value, valueFont, glowBrush, valuePos.X + 1, valuePos.Y + 1);
            }
            using (Brush valueBrush = new SolidBrush(eliteOrange))
            {
                g.DrawString(value, valueFont, valueBrush, valuePos);
            }
        }

        private static void DrawStatusBar(Graphics g, Rectangle rect, float pulseValue, bool isSessionActive)
        {
            using Font statusFont = new Font("Consolas", 9F, FontStyle.Regular);

            // Pulsing status dot
            Color dotColor = Color.FromArgb((int)(pulseValue * 255), eliteGreen);
            using (SolidBrush dotBrush = new SolidBrush(dotColor))
            {
                g.FillEllipse(dotBrush, rect.X, rect.Y + 8, 10, 10);
            }

            using (Brush textBrush = new SolidBrush(eliteOrangeLight))
            {
                g.DrawString("SYSTEMS NOMINAL", statusFont, textBrush, rect.X + 18, rect.Y);

                string rightText = "CMDR STATUS: ACTIVE";
                SizeF textSize = g.MeasureString(rightText, statusFont);
                g.DrawString(rightText, statusFont, textBrush, rect.Right - textSize.Width, rect.Y);
            }
        }

        private static void DrawCornerDecorations(Graphics g, Rectangle rect)
        {
            int size = 18;
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
    }

    public struct MiningUIData
    {
        public decimal MiningProfit { get; set; }
        public int LimpetsUsed { get; set; }
        public int TonsRefined { get; set; }
        public TimeSpan Duration { get; set; }
        public decimal ProfitPerHour { get; set; }
        public float PulseValue { get; set; }
        public bool IsSessionActive { get; set; }
    }
}