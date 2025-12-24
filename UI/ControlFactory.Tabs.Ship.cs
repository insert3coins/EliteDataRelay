using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
        public TableLayoutPanel ShipStatsPanel { get; private set; } = null!;
        public Label ShipTabNameLabel { get; private set; } = null!;
        public Label ShipTabIdentLabel { get; private set; } = null!;
        public Label ShipFuelLabel { get; private set; } = null!;
        public Label ShipValueLabel { get; private set; } = null!;
        public Label BottomMassLabel { get; private set; } = null!;
        public Label BottomArmorLabel { get; private set; } = null!;
        public Label BottomCargoLabel { get; private set; } = null!;
        public Label BottomJumpLabel { get; private set; } = null!;
        public Label BottomRebuyLabel { get; private set; } = null!;

        public FlowLayoutPanel SidebarHardpointsPanel { get; private set; } = null!;
        public FlowLayoutPanel SidebarUtilitiesPanel { get; private set; } = null!;
        public FlowLayoutPanel HardpointListPanel { get; private set; } = null!;
        public FlowLayoutPanel UtilityListPanel { get; private set; } = null!;
        public FlowLayoutPanel CoreListPanel { get; private set; } = null!;
        public FlowLayoutPanel OptionalListPanel { get; private set; } = null!;

        private TabPage CreateShipTabPage(FontManager fontManager)
        {
            var shipPage = new TabPage("Ship")
            {
                Padding = new Padding(8),
                BackColor = Color.Black
            };

            var outerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f)); // Bottom spacing for bottom bar boxes

            var mainShipPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            mainShipPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            mainShipPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            mainShipPanel.Controls.Add(CreateShipLeftColumn(fontManager), 0, 0);
            mainShipPanel.Controls.Add(CreateShipRightColumn(fontManager), 1, 0);

            outerLayout.Controls.Add(mainShipPanel, 0, 0);
            outerLayout.Controls.Add(CreateShipValueBar(fontManager), 0, 1);

            shipPage.Controls.Add(outerLayout);

            return shipPage;
        }

        private Control CreateShipSidebar(FontManager fontManager)
        {
            var sidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(26, 10, 0),
                Padding = new Padding(8, 8, 4, 8),
                Margin = new Padding(0, 0, 6, 0)
            };

            var scroll = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(18, 8, 0),
                Padding = new Padding(6)
            };

            var hardpointSection = CreateSidebarSection("Hardpoints", fontManager, out var hardpointContainer);
            SidebarHardpointsPanel = hardpointContainer;
            var utilitySection = CreateSidebarSection("Utility Mounts", fontManager, out var utilityContainer);
            SidebarUtilitiesPanel = utilityContainer;

            scroll.Controls.Add(hardpointSection);
            scroll.Controls.Add(utilitySection);

            sidebar.Controls.Add(scroll);
            return sidebar;
        }

        private Control CreateSidebarSection(string title, FontManager fontManager, out FlowLayoutPanel container)
        {
            var wrapper = new Panel
            {
                Width = 290,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 10)
            };

            var header = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Font = fontManager.SegoeUIFontBold,
                ForeColor = Color.FromArgb(255, 136, 0),
                BackColor = Color.FromArgb(36, 16, 4),
                Padding = new Padding(10, 6, 10, 6),
                Margin = new Padding(0, 0, 0, 4),
                AutoSize = false,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft
            };

            container = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true,
                BackColor = Color.FromArgb(12, 12, 12),
                Padding = new Padding(4)
            };

            wrapper.Controls.Add(container);
            wrapper.Controls.Add(header);
            return wrapper;
        }

        private Control CreateShipLeftColumn(FontManager fontManager)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(12, 12, 12),
                Padding = new Padding(6, 8, 6, 8)
            };

            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = false,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var hardpoints = CreateLoadoutSection("Hardpoints", fontManager, out var hardpointList);
            HardpointListPanel = hardpointList;
            var utilities = CreateLoadoutSection("Utility Mounts", fontManager, out var utilityList);
            UtilityListPanel = utilityList;

            stack.Controls.Add(hardpoints, 0, 0);
            stack.Controls.Add(utilities, 1, 0);
            panel.Controls.Add(stack);
            return panel;
        }

        private Control CreateShipHeaderColumn(FontManager fontManager)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(14, 14, 14),
                Padding = new Padding(8, 10, 8, 10)
            };

            var header = CreateShipHeader(fontManager);
            header.Dock = DockStyle.Top;
            panel.Controls.Add(header);
            return panel;
        }

        private Control CreateShipRightColumn(FontManager fontManager)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(12, 12, 12),
                Padding = new Padding(6, 8, 6, 8)
            };

            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            var core = CreateLoadoutSection("Core Internal", fontManager, out var coreList);
            CoreListPanel = coreList;
            var optional = CreateLoadoutSection("Optional Internal", fontManager, out var optionalList);
            OptionalListPanel = optionalList;

            stack.Controls.Add(core, 0, 0);
            stack.Controls.Add(optional, 1, 0);
            panel.Controls.Add(stack);
            return panel;
        }

        private Control CreateShipHeader(FontManager fontManager)
        {
            var header = new Panel
            {
                Width = 100,
                Height = 12,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(26, 26, 26),
                Padding = new Padding(0),
                Margin = new Padding(0, 0, 0, 6)
            };

            ShipFuelLabel = new Label { Visible = false, Height = 0 };
            ShipTabNameLabel = new Label { Visible = false, Height = 0 };
            ShipTabIdentLabel = new Label { Visible = false, Height = 0 };

            return header;
        }

        private Control CreateLoadoutSection(string title, FontManager fontManager, out FlowLayoutPanel listPanel)
        {
            var section = new Panel
            {
                AutoSize = false,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(16, 16, 16),
                Margin = new Padding(6, 0, 6, 12),
                Padding = new Padding(0),
                MinimumSize = new Size(0, 180)
            };

            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Font = fontManager.SegoeUIFontBold,
                ForeColor = Color.FromArgb(255, 136, 0),
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(12, 6, 12, 6),
                AutoSize = false,
                Height = 26,
                TextAlign = ContentAlignment.MiddleLeft
            };

            listPanel = new NoBarFlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(14, 14, 14),
                Padding = new Padding(4),
                Margin = new Padding(0),
                MinimumSize = new Size(0, 160)
            };
            section.Controls.Add(listPanel);
            section.Controls.Add(titleLabel);
            return section;
        }

        private Control CreateShipRightPanel(FontManager fontManager)
        {
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(26, 10, 0),
                Padding = new Padding(8, 10, 10, 10),
                Margin = new Padding(10, 0, 0, 0)
            };

            var stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            ShipStatsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoScroll = false,
                ColumnCount = 2,
                BackColor = Color.FromArgb(20, 10, 0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                Margin = new Padding(0),
                Visible = false
            };
            ShipStatsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            ShipStatsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            stack.Controls.Add(ShipStatsPanel);

            rightPanel.Controls.Add(stack);
            return rightPanel;
        }

        private Control CreateShipValueBar(FontManager fontManager)
        {
            var bar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 10, 0),
                Padding = new Padding(10, 2, 10, 2),
                Margin = new Padding(0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f)); // Ship value
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var shipValueCell = CreateStackedCell(fontManager, "Ship Value", out var shipValueLabel, highlight: false);
            ShipValueLabel = shipValueLabel;
            var massCell = CreateStackedCell(fontManager, "MASS", out var massLabel);
            BottomMassLabel = massLabel;
            var armorCell = CreateStackedCell(fontManager, "ARMOR", out var armorLabel);
            BottomArmorLabel = armorLabel;
            var cargoCell = CreateStackedCell(fontManager, "CARGO", out var cargoLabel);
            BottomCargoLabel = cargoLabel;
            var jumpCell = CreateStackedCell(fontManager, "JUMP", out var jumpLabel);
            BottomJumpLabel = jumpLabel;
            var rebuyCell = CreateStackedCell(fontManager, "REBUY", out var rebuyLabel);
            BottomRebuyLabel = rebuyLabel;

            layout.Controls.Add(shipValueCell, 0, 0);
            layout.Controls.Add(massCell, 1, 0);
            layout.Controls.Add(armorCell, 2, 0);
            layout.Controls.Add(cargoCell, 3, 0);
            layout.Controls.Add(jumpCell, 4, 0);
            layout.Controls.Add(rebuyCell, 5, 0);

            bar.Controls.Add(layout);
            return bar;
        }

        private Control CreateStackedCell(FontManager fontManager, string label, out Label valueLabel, bool highlight = false)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(28, 18, 8),
                Padding = new Padding(6, 2, 6, 2),
                Margin = new Padding(4, 0, 4, 0),
                BorderStyle = BorderStyle.FixedSingle,
                MinimumSize = new Size(0, 0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var title = new Label
            {
                Text = label,
                Dock = DockStyle.Top,
                Font = fontManager.SegoeUIFontBold,
                ForeColor = highlight ? Color.White : Color.FromArgb(255, 136, 0),
                Height = 16,
                TextAlign = ContentAlignment.MiddleLeft
            };

            valueLabel = new Label
            {
                Text = "-",
                Dock = DockStyle.Fill,
                Font = highlight ? fontManager.SegoeUIFontBold : fontManager.SegoeUIFont,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(0),
                AutoSize = false
            };

            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(valueLabel, 0, 1);

            panel.Controls.Add(layout);
            return panel;
        }

        private void DisposeShipTabControls()
        {
            ShipStatsPanel?.Dispose();
            ShipTabNameLabel?.Dispose();
            ShipTabIdentLabel?.Dispose();
            ShipFuelLabel?.Dispose();
            ShipValueLabel?.Dispose();
            BottomMassLabel?.Dispose();
            BottomArmorLabel?.Dispose();
            BottomCargoLabel?.Dispose();
            BottomJumpLabel?.Dispose();
            BottomRebuyLabel?.Dispose();
            SidebarHardpointsPanel?.Dispose();
            SidebarUtilitiesPanel?.Dispose();
            HardpointListPanel?.Dispose();
            UtilityListPanel?.Dispose();
            CoreListPanel?.Dispose();
            OptionalListPanel?.Dispose();
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

        private sealed class NoBarFlowLayoutPanel : FlowLayoutPanel
        {
            private const int WS_VSCROLL = 0x00200000;
            private const int WS_HSCROLL = 0x00100000;
            private const int SB_BOTH = 3;

            public NoBarFlowLayoutPanel()
            {
                DoubleBuffered = true;
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.Style &= ~WS_VSCROLL;
                    cp.Style &= ~WS_HSCROLL;
                    return cp;
                }
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                if (IsHandleCreated)
                {
                    ShowScrollBar(Handle, SB_BOTH, false);
                }
            }

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);
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
