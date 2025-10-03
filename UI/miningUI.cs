using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EliteMiningUI
{
    public class MiningUI : Form
    {
        // Data properties
        public decimal MiningProfit { get; set; } = 2847650;
        public int LimpetsUsed { get; set; } = 142;
        public int TonsRefined { get; set; } = 86;
        public TimeSpan Duration { get; set; } = new TimeSpan(2, 34, 0);
        public decimal ProfitPerHour { get; set; } = 1100000;

        // UI Colors
        private readonly Color eliteOrange = Color.FromArgb(255, 136, 0);
        private readonly Color eliteOrangeLight = Color.FromArgb(255, 170, 68);
        private readonly Color eliteGreen = Color.FromArgb(0, 255, 0);
        private readonly Color darkBackground = Color.FromArgb(0, 10, 20);
        
        private System.Windows.Forms.Timer? pulseTimer;
        private float pulseValue = 1.0f;
        private bool pulseDirection = false;
        
        private Button? startStopButton;
        private bool isSessionActive = false;

        public MiningUI()
        {
            InitializeComponent();
            SetupPulseAnimation();
            SetupStartStopButton();
        }

        private void InitializeComponent()
        {
            this.Text = "Mining Operations Interface";
            this.ClientSize = new Size(800, 600);
            this.BackColor = Color.Black;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Consolas", 10F, FontStyle.Regular);
            this.DoubleBuffered = true;
            
            this.Paint += MiningUI_Paint;
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

        private void SetupStartStopButton()
        {
            startStopButton = new Button();
            startStopButton.Size = new Size(200, 60);
            startStopButton.Location = new Point((this.ClientSize.Width - startStopButton.Width) / 2, 
                                                  (this.ClientSize.Height - startStopButton.Height) / 2);
            startStopButton.Text = "START SESSION";
            startStopButton.Font = new Font("Consolas", 14F, FontStyle.Bold);
            startStopButton.ForeColor = eliteOrange;
            startStopButton.BackColor = Color.FromArgb(0, 10, 20);
            startStopButton.FlatStyle = FlatStyle.Flat;
            startStopButton.FlatAppearance.BorderColor = eliteOrange;
            startStopButton.FlatAppearance.BorderSize = 2;
            startStopButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 255, 136, 0);
            startStopButton.Cursor = Cursors.Hand;
            startStopButton.Click += StartStopButton_Click;
            
            this.Controls.Add(startStopButton);
        }

        private void StartStopButton_Click(object? sender, EventArgs e)
        {
            isSessionActive = !isSessionActive;
            
            if (isSessionActive)
            {
                startStopButton!.Text = "STOP SESSION";
                startStopButton.Visible = false;
            }
            else
            {
                startStopButton!.Text = "START SESSION";
                startStopButton.Visible = true;
            }
            
            this.Invalidate();
        }

        private void MiningUI_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Only draw stats if session is active
            if (!isSessionActive)
                return;

            // Main interface panel - centered with margins
            Rectangle mainRect = new Rectangle(30, 30, 740, 540);
            DrawMainPanel(g, mainRect);

            // Header
            DrawHeader(g, mainRect);

            // Main profit display - larger and more prominent
            Rectangle profitRect = new Rectangle(mainRect.X + 30, mainRect.Y + 100, mainRect.Width - 60, 100);
            DrawStatBox(g, profitRect, "◢ TOTAL MINING PROFIT ◣", $"{MiningProfit:N0} CR", true);

            // Stats grid - 2x2 layout with better spacing
            int statY = profitRect.Bottom + 30;
            int statWidth = (mainRect.Width - 90) / 2;
            int statHeight = 100;
            int gap = 30;

            DrawStatBox(g, new Rectangle(mainRect.X + 30, statY, statWidth, statHeight), 
                "▸ LIMPETS USED", $"{LimpetsUsed} units", false);
            DrawStatBox(g, new Rectangle(mainRect.X + 30 + statWidth + gap, statY, statWidth, statHeight), 
                "▸ REFINED", $"{TonsRefined} tons", false);
            
            statY += statHeight + gap;
            DrawStatBox(g, new Rectangle(mainRect.X + 30, statY, statWidth, statHeight), 
                "▸ DURATION", $"{Duration.Hours}:{Duration.Minutes:D2} h", false);
            DrawStatBox(g, new Rectangle(mainRect.X + 30 + statWidth + gap, statY, statWidth, statHeight), 
                "▸ PROFIT/HOUR", $"{ProfitPerHour / 1000000:F1}M CR/h", false);

            // Status bar
            DrawStatusBar(g, new Rectangle(mainRect.X + 30, mainRect.Bottom - 50, mainRect.Width - 60, 25));

            // Corner decorations
            DrawCornerDecorations(g, mainRect);
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
            Font titleFont = new Font("Consolas", 22F, FontStyle.Bold);
            Font subtitleFont = new Font("Consolas", 10F, FontStyle.Regular);

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

            titleFont.Dispose();
            subtitleFont.Dispose();
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
            Font labelFont = new Font("Consolas", isMain ? 11F : 9F, FontStyle.Regular);
            using (Brush labelBrush = new SolidBrush(eliteOrangeLight))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(label, labelFont, labelBrush, 
                    new RectangleF(rect.X, rect.Y + 15, rect.Width, 25), sf);
            }
            labelFont.Dispose();

            // Value
            Font valueFont = new Font("Consolas", isMain ? 28F : 22F, FontStyle.Bold);
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
            valueFont.Dispose();
        }

        private void DrawStatusBar(Graphics g, Rectangle rect)
        {
            Font statusFont = new Font("Consolas", 9F, FontStyle.Regular);
            
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
            
            statusFont.Dispose();
        }

        private void DrawCornerDecorations(Graphics g, Rectangle rect)
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pulseTimer?.Stop();
                pulseTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MiningUI());
        }
    }
}