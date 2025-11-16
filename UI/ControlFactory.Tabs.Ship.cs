using System.Drawing;
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
            var shipPage = new TabPage("Ship");
            shipPage.Padding = new Padding(10);

            var mainShipPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(20, 20, 25)
            };
            mainShipPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340F));
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
                BackColor = Color.FromArgb(25, 25, 30)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.FromArgb(30, 30, 35),
                Padding = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var summaryPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                BackColor = Color.FromArgb(35, 35, 40),
                Padding = new Padding(12),
                Margin = new Padding(0, 0, 0, 8)
            };
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            ShipTabNameLabel = new Label
            {
                Text = "Ship Name",
                Dock = DockStyle.Fill,
                Font = new Font(fontManager.ConsolasFont.FontFamily, 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(224, 224, 235),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 4)
            };
            summaryPanel.Controls.Add(ShipTabNameLabel, 0, 0);

            ShipTabIdentLabel = new Label
            {
                Text = "ID: N/A",
                Dock = DockStyle.Fill,
                Font = fontManager.ConsolasFont,
                ForeColor = Color.FromArgb(156, 163, 175),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 8)
            };
            summaryPanel.Controls.Add(ShipTabIdentLabel, 0, 1);

            var summaryGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0)
            };
            summaryGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            summaryGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            summaryGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            summaryGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            summaryGrid.Controls.Add(CreateSummaryHeaderLabel("Fuel"), 0, 0);
            summaryGrid.Controls.Add(CreateSummaryHeaderLabel("Value"), 1, 0);

            ShipFuelLabel = CreateSummaryValueLabel("Main: 0.0 T  |  Res: 0.0 T", fontManager);
            ShipValueLabel = CreateSummaryValueLabel("0 CR", fontManager);
            summaryGrid.Controls.Add(ShipFuelLabel, 0, 1);
            summaryGrid.Controls.Add(ShipValueLabel, 1, 1);

            summaryPanel.Controls.Add(summaryGrid, 0, 2);
            layout.Controls.Add(summaryPanel, 0, 0);

            ShipStatsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.FromArgb(30, 30, 35),
                Padding = new Padding(6),
                AutoSize = false
            };
            ShipStatsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            ShipStatsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            layout.Controls.Add(ShipStatsPanel, 0, 1);

            var spacerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 6,
                BackColor = Color.FromArgb(35, 35, 40)
            };
            layout.Controls.Add(spacerPanel, 0, 2);

            leftPanel.Controls.Add(layout);
            return leftPanel;
        }

        private static Label CreateSummaryHeaderLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(120, 126, 140),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 2)
            };
        }

        private static Label CreateSummaryValueLabel(string text, FontManager fontManager)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(224, 224, 235),
                Font = fontManager.ConsolasFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 4)
            };
        }

        private Panel CreateShipRightPanel(FontManager fontManager)
        {
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 35),
                Padding = new Padding(10)
            };

            ModuleTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.FlatButtons,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                Font = fontManager.ConsolasFont
            };
            ModuleTabControl.DrawItem += TabControl_DrawItem;
            rightPanel.Controls.Add(ModuleTabControl);

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
            using (var bgBrush = new SolidBrush(Color.FromArgb(30, 30, 35)))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            // Draw text
            using (Brush textBrush = isSelected ?
                new SolidBrush(Color.FromArgb(34, 211, 238)) : // Cyan
                new SolidBrush(Color.FromArgb(156, 163, 175))) // Gray
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
                using (var borderPen = new Pen(Color.FromArgb(34, 211, 238), 2))
                {
                    e.Graphics.DrawLine(borderPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                }
            }
        }

        /// <summary>
        /// A custom-drawn panel to display a single ship statistic, avoiding complex control nesting.
        /// </summary>
        public class StatPanel : Panel
        {
            private readonly string _label;
            private string _value;

            // Re-usable drawing resources
            private readonly Font _labelFont;
            private readonly Font _valueFont;
            private readonly SolidBrush _labelBrush;
            private readonly SolidBrush _valueBrush;

            public StatPanel(string label, string initialValue, Font font)
            {
                _label = label;
                _value = initialValue;

                _labelFont = font;
                _valueFont = new Font(font, FontStyle.Bold);
                _labelBrush = new SolidBrush(Color.FromArgb(150, 150, 160));
                _valueBrush = new SolidBrush(Color.FromArgb(220, 220, 230));

                Dock = DockStyle.Fill;
                Margin = new Padding(2);
                BackColor = Color.FromArgb(40, 40, 45);
                DoubleBuffered = true; // Prevents flicker
            }

            public void SetValue(string newValue)
            {
                _value = newValue;
                Invalidate(); // Redraw the panel with the new value
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Define two separate, non-overlapping rectangles for the label and the value.
                // This prevents them from drawing over each other.
                int labelWidth = (int)(ClientRectangle.Width * 0.45); // Give label 45% of the space
                int valueWidth = ClientRectangle.Width - labelWidth;

                Rectangle labelRect = new Rectangle(ClientRectangle.X, ClientRectangle.Y, labelWidth, ClientRectangle.Height);
                Rectangle valueRect = new Rectangle(ClientRectangle.X + labelWidth, ClientRectangle.Y, valueWidth, ClientRectangle.Height);

                // Define text formatting flags
                var textFormatLeft = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
                var textFormatRight = TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;

                // Add some padding to the rectangles for better spacing
                labelRect.Inflate(-5, 0);
                valueRect.Inflate(-5, 0);

                TextRenderer.DrawText(e.Graphics, _label, _labelFont, labelRect, _labelBrush.Color, textFormatLeft);
                TextRenderer.DrawText(e.Graphics, _value, _valueFont, valueRect, _valueBrush.Color, textFormatRight);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _labelFont.Dispose();
                    _valueFont.Dispose();
                    _labelBrush.Dispose();
                    _valueBrush.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
