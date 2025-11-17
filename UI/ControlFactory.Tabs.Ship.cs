using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Text;
using System.Linq;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
        public TableLayoutPanel ShipStatsPanel { get; private set; } = null!;
        public Label ShipTabNameLabel { get; private set; } = null!;
        public Label ShipTabIdentLabel { get; private set; } = null!;
        public Label ShipFuelLabel { get; private set; } = null!;
        public Label ShipValueLabel { get; private set; } = null!;
        public TabControl ModuleTabControl { get; private set; } = null!;

        private TabPage CreateShipTabPage(FontManager fontManager)
        {
            var shipPage = new TabPage("Ship")
            {
                Padding = new Padding(12),
                BackColor = Color.FromArgb(14, 16, 22)
            };

            var mainShipPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            mainShipPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360F));
            mainShipPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var leftPanel = CreateShipLeftPanel(fontManager);
            var rightPanel = CreateShipRightPanel(fontManager);
            mainShipPanel.Controls.Add(leftPanel, 0, 0);
            mainShipPanel.Controls.Add(rightPanel, 1, 0);

            shipPage.Controls.Add(mainShipPanel);

            return shipPage;
        }

        private Panel CreateShipLeftPanel(FontManager fontManager)
        {
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            layout.Controls.Add(CreateHeroCard(fontManager), 0, 0);
            layout.Controls.Add(CreateStatsCard(fontManager), 0, 1);

            leftPanel.Controls.Add(layout);
            return leftPanel;
        }

        private Control CreateHeroCard(FontManager fontManager)
        {
            var heroCard = new AccentPanel
            {
                Dock = DockStyle.Top,
                Padding = new Padding(18),
                Margin = new Padding(0, 0, 12, 14),
                AccentStart = Color.FromArgb(58, 74, 112),
                AccentEnd = Color.FromArgb(24, 28, 40)
            };

            var heroLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            heroLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            heroLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            heroLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            heroLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            ShipTabNameLabel = new Label
            {
                Text = "Ship Name",
                Dock = DockStyle.Fill,
                Font = fontManager.SegoeUIFontLarge,
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 0, 4)
            };
            heroLayout.Controls.Add(ShipTabNameLabel, 0, 0);

            ShipTabIdentLabel = new Label
            {
                Text = "ID: N/A",
                Dock = DockStyle.Fill,
                Font = fontManager.SegoeUIFont,
                ForeColor = Color.FromArgb(206, 212, 224),
                Margin = new Padding(0, 0, 0, 12)
            };
            heroLayout.Controls.Add(ShipTabIdentLabel, 0, 1);

            var metaRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 6)
            };
            metaRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            metaRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            ShipFuelLabel = CreateHeroChipLabel(fontManager.SegoeUIFontBold, "Main: 0.0 T  |  Res: 0.0 T");
            ShipValueLabel = CreateHeroChipLabel(fontManager.SegoeUIFontBold, "Value: 0 CR");
            metaRow.Controls.Add(ShipFuelLabel, 0, 0);
            metaRow.Controls.Add(ShipValueLabel, 1, 0);
            heroLayout.Controls.Add(metaRow, 0, 2);

            var infoLabel = new Label
            {
                Text = "Click the ship name to open the current loadout on EDSY.",
                Dock = DockStyle.Fill,
                Font = fontManager.SegoeUIFont,
                ForeColor = Color.FromArgb(200, 210, 220),
                Margin = new Padding(0)
            };
            heroLayout.Controls.Add(infoLabel, 0, 3);

            heroCard.Controls.Add(heroLayout);
            return heroCard;
        }

        private Label CreateHeroChipLabel(Font font, string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(224, 229, 242),
                Font = font,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(10, 6, 10, 6),
                BackColor = Color.FromArgb(46, 56, 84),
                BorderStyle = BorderStyle.None
            };
        }

        private Control CreateStatsCard(FontManager fontManager)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 26, 33),
                Padding = new Padding(16),
                Margin = new Padding(0, 0, 12, 0)
            };

            var header = new Label
            {
                Text = "Performance Snapshot",
                Dock = DockStyle.Top,
                Font = fontManager.SegoeUIFontBold,
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 0, 4)
            };
            var subtitle = new Label
            {
                Text = "Live stats derived from current loadout and journal telemetry.",
                Dock = DockStyle.Top,
                Font = fontManager.SegoeUIFont,
                ForeColor = Color.FromArgb(170, 179, 196),
                Margin = new Padding(0, 0, 0, 12)
            };

            ShipStatsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                AutoSize = false,
                ColumnCount = 2,
                BackColor = Color.Transparent,
                Padding = new Padding(4),
                Margin = new Padding(0),
                GrowStyle = TableLayoutPanelGrowStyle.AddRows
            };
            ShipStatsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            ShipStatsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            card.Controls.Add(ShipStatsPanel);
            card.Controls.Add(subtitle);
            card.Controls.Add(header);
            return card;
        }

        private Panel CreateShipRightPanel(FontManager fontManager)
        {
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            var container = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            container.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var header = new Label
            {
                Text = "Module Breakdown",
                Dock = DockStyle.Top,
                Font = fontManager.SegoeUIFontBold,
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 0, 6)
            };
            container.Controls.Add(header, 0, 0);

            var moduleCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 27, 36),
                Padding = new Padding(12),
                Margin = new Padding(0)
            };

            ModuleTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.Normal,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                Font = fontManager.SegoeUIFont,
                ItemSize = new Size(120, 28),
                SizeMode = TabSizeMode.Fixed
            };
            ModuleTabControl.DrawItem += TabControl_DrawItem;
            moduleCard.Controls.Add(ModuleTabControl);

            container.Controls.Add(moduleCard, 0, 1);
            rightPanel.Controls.Add(container);

            return rightPanel;
        }

        private void DisposeShipTabControls()
        {
            ShipStatsPanel?.Dispose();
            ShipTabNameLabel?.Dispose();
            ShipTabIdentLabel?.Dispose();
            ShipFuelLabel?.Dispose();
            ShipValueLabel?.Dispose();
            if (ModuleTabControl != null)
            {
                ModuleTabControl.DrawItem -= TabControl_DrawItem;
                ModuleTabControl.Dispose();
            }
        }

        // Custom drawing for the ListView to match the WPF style
        private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not TabControl tabControl) return;
            if (e.Font is null) return; // Prevent drawing if font is not available
            TabPage tab = tabControl.TabPages[e.Index];

            // Draw background
            bool isSelected = (e.Index == tabControl.SelectedIndex);
            using (var bgBrush = new SolidBrush(isSelected ? Color.FromArgb(41, 46, 64) : Color.FromArgb(30, 32, 42)))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            // Draw text
            using (Brush textBrush = isSelected ?
                new SolidBrush(Color.FromArgb(52, 199, 89)) :
                new SolidBrush(Color.FromArgb(156, 163, 175)))
            {
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString(tab.Text, e.Font, textBrush, e.Bounds, sf);
            }

            // Draw bottom border for selected tab
            if (isSelected)
            {
                using (var borderPen = new Pen(Color.FromArgb(52, 199, 89), 2))
                {
                    e.Graphics.DrawLine(borderPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                }
            }
        }

        /// <summary>
        /// A custom-drawn panel to display a single ship statistic, avoiding complex control nesting.
        /// </summary>
        public enum StatPanelTheme
        {
            Dark,
            Light
        }

        public class StatPanel : Panel
        {
            private readonly string _label;
            private string _value;
            private readonly Font _labelFont;
            private readonly Font _valueFont;
            private readonly StringFormat _labelFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
            private readonly StringFormat _valueFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };

            public StatPanel(string label, string initialValue, Font font, StatPanelTheme theme = StatPanelTheme.Dark)
            {
                _label = label;
                _value = initialValue;

                _labelFont = new Font(font.FontFamily, font.Size - 1f, FontStyle.Regular);
                _valueFont = new Font(font.FontFamily, font.Size + 2f, FontStyle.Bold);

                bool lightTheme = theme == StatPanelTheme.Light;
                LabelColor = lightTheme ? Color.FromArgb(140, 144, 160) : Color.FromArgb(184, 189, 204);
                ValueColor = lightTheme ? Color.FromArgb(26, 30, 38) : Color.FromArgb(250, 250, 255);
                AccentColor = lightTheme ? Color.FromArgb(200, 208, 230) : Color.FromArgb(63, 132, 231);

                Dock = DockStyle.Fill;
                Margin = new Padding(6);
                BackColor = lightTheme ? Color.FromArgb(245, 246, 252) : Color.FromArgb(34, 37, 47);
                DoubleBuffered = true;
                _labelFormat.Trimming = StringTrimming.EllipsisCharacter;
                _valueFormat.Trimming = StringTrimming.EllipsisCharacter;
            }

            public Color LabelColor { get; }
            public Color ValueColor { get; }
            public Color AccentColor { get; }

            public void SetValue(string newValue)
            {
                _value = newValue;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var rect = ClientRectangle;
                rect.Inflate(-4, -4);

                using (var bgBrush = new SolidBrush(BackColor))
                using (var accentPen = new Pen(AccentColor, 1.5f))
                using (var labelBrush = new SolidBrush(LabelColor))
                using (var valueBrush = new SolidBrush(ValueColor))
                {
                    using (var path = DrawingUtils.CreateRoundedRectPath(rect, 8))
                    {
                        e.Graphics.FillPath(bgBrush, path);
                        e.Graphics.DrawPath(accentPen, path);
                    }

                var labelRect = new RectangleF(rect.X + 8, rect.Y + 6, rect.Width - 16, _labelFont.GetHeight(e.Graphics) + 2);
                var valueRect = new RectangleF(rect.X + 8, labelRect.Bottom + 4, rect.Width - 16, rect.Height - labelRect.Height - 12);

                    e.Graphics.DrawString(_label, _labelFont, labelBrush, labelRect, _labelFormat);
                    e.Graphics.DrawString(_value, _valueFont, valueBrush, valueRect, _valueFormat);
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _labelFont.Dispose();
                    _valueFont.Dispose();
                    _labelFormat.Dispose();
                    _valueFormat.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        private sealed class AccentPanel : Panel
        {
            [Browsable(false)]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Color AccentStart { get; set; } = Color.FromArgb(46, 58, 87);

            [Browsable(false)]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Color AccentEnd { get; set; } = Color.FromArgb(20, 24, 35);

            public AccentPanel()
            {
                DoubleBuffered = true;
                ForeColor = Color.White;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                using var brush = new LinearGradientBrush(ClientRectangle, AccentStart, AccentEnd, 45f);
                e.Graphics.FillRectangle(brush, ClientRectangle);
                using var borderPen = new Pen(Color.FromArgb(60, Color.White));
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}
