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
        public PictureBox ShipPictureBox { get; private set; } = null!;
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
            Panel leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 35)
            };

            // Ship Wireframe
            ShipPictureBox = new PictureBox
            {
                Location = new Point(0, 0),
                Size = new Size(160, 160),
                BackColor = Color.FromArgb(40, 40, 45),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            leftPanel.Controls.Add(ShipPictureBox);

            // Ship Stats
            ShipStatsPanel = new TableLayoutPanel
            {
                Location = new Point(170, 0),
                Size = new Size(170, 160),
                BackColor = Color.FromArgb(35, 35, 40),
                ColumnCount = 3,
                RowCount = 2,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(2)
            };
            ShipStatsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 6; i++) ShipStatsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66F));

            leftPanel.Controls.Add(ShipStatsPanel);

            // Ship Name Label
            ShipTabNameLabel = new Label
            {
                Text = "Ship Name",
                Font = new Font(fontManager.ConsolasFont.FontFamily, 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 230),
                Location = new Point(0, 170),
                Size = new Size(340, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(35, 35, 40)
            };
            leftPanel.Controls.Add(ShipTabNameLabel);

            // Ship ID Label
            ShipTabIdentLabel = new Label
            {
                Text = "ID: N/A",
                Font = fontManager.ConsolasFont,
                ForeColor = Color.FromArgb(156, 163, 175),
                Location = new Point(0, 200),
                Size = new Size(340, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            leftPanel.Controls.Add(ShipTabIdentLabel);

            // Ship Fuel Label
            ShipFuelLabel = new Label
            {
                Text = "Fuel: 0 / 0 T",
                Font = fontManager.ConsolasFont,
                ForeColor = Color.FromArgb(156, 163, 175),
                Location = new Point(0, 230),
                Size = new Size(340, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(35, 35, 40)
            };
            leftPanel.Controls.Add(ShipFuelLabel);

            // Ship Value Label
            ShipValueLabel = new Label
            {
                Text = "Value: 0 CR",
                Font = fontManager.ConsolasFont,
                ForeColor = Color.FromArgb(156, 163, 175),
                Location = new Point(0, 260),
                Size = new Size(340, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(35, 35, 40)
            };
            leftPanel.Controls.Add(ShipValueLabel);

            return leftPanel;
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
            ShipPictureBox?.Dispose();
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