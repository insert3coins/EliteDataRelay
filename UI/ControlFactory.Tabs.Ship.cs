using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
        // New controls for the redesigned ship tab
        public PictureBox ShipWireframePictureBox { get; private set; } = null!;
        public TableLayoutPanel ShipStatsPanel { get; private set; } = null!;
        public FlowLayoutPanel ModuleTabPanel { get; private set; } = null!;
        public ListView ModulesListView { get; private set; } = null!;

        // Old controls that are being replaced or removed
        // public PictureBox ShipIconPictureBox { get; private set; } = null!;
        // public Label HullHealthValueLabel { get; private set; } = null!;
        /*
        public Label MassValueLabel { get; private set; } = null!;
        public Label FuelValueLabel { get; private set; } = null!;
        public Label CargoValueLabel { get; private set; } = null!;
        public Label JumpRangeValueLabel { get; private set; } = null!;
        public Label RebuyValueLabel { get; private set; } = null!;
        private TreeNode? _lastHoveredNode;
        */

        private TabPage CreateShipTabPage(FontManager fontManager)
        {
            var shipPage = new TabPage("Ship");
            shipPage.Padding = new Padding(10);

            var mainShipPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
            };
            mainShipPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F));
            mainShipPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));

            var leftPanel = CreateShipLeftPanel(fontManager);
            var rightPanel = CreateShipRightPanel(fontManager);

            mainShipPanel.Controls.Add(leftPanel, 0, 0);
            mainShipPanel.Controls.Add(rightPanel, 1, 0);

            shipPage.Controls.Add(mainShipPanel);

            return shipPage;
        }

        private TableLayoutPanel CreateShipLeftPanel(FontManager fontManager)
        {
            var leftPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0, 0, 10, 0)
            };
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));

            // Ship Wireframe
            ShipWireframePictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(10, 10, 10) // Use a solid, dark color to prevent crash
            };

            // Ship Stats
            ShipStatsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                Padding = new Padding(10),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                BackColor = Color.FromArgb(10, 10, 10), // Use a solid, dark color
            };
            for (int i = 0; i < 4; i++) ShipStatsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            for (int i = 0; i < 2; i++) ShipStatsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            leftPanel.Controls.Add(ShipWireframePictureBox, 0, 0);
            leftPanel.Controls.Add(ShipStatsPanel, 0, 1);

            return leftPanel;
        }

        private TableLayoutPanel CreateShipRightPanel(FontManager fontManager)
        {
            var rightPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10, 0, 0, 0)
            };
            rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Module Tabs
            ModuleTabPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
            };

            // Create a dummy ImageList to control the row height for owner-drawn ListView.
            // The ListView uses the ImageSize.Height of the SmallImageList for row height.
            var dummyImageList = new ImageList();
            dummyImageList.ImageSize = new Size(1, 45); // Set desired row height (e.g., 45 pixels)
            dummyImageList.ColorDepth = ColorDepth.Depth8Bit; // Minimal color depth as no images are used

            // Module List
            ModulesListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                Font = fontManager.ConsolasFont,
                BackColor = Color.FromArgb(10, 10, 10), // Use a solid, dark color
                ForeColor = Color.Gainsboro,
                BorderStyle = BorderStyle.FixedSingle,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.None,
                OwnerDraw = true,
                SmallImageList = dummyImageList, // Assign the dummy ImageList to control row height
            };
            ModulesListView.DrawItem += ModulesListView_DrawItem;
            ModulesListView.DrawSubItem += ModulesListView_DrawSubItem;
            ModulesListView.Columns.Add("Module", -2);

            rightPanel.Controls.Add(ModuleTabPanel, 0, 0);
            rightPanel.Controls.Add(ModulesListView, 0, 1);

            return rightPanel;
        }

        private void DisposeShipTabControls()
        {
            ShipWireframePictureBox?.Dispose();
            ShipStatsPanel?.Dispose();
            ModuleTabPanel?.Dispose();
            if (ModulesListView != null)
            {
                var listView = ModulesListView;
                listView.SmallImageList?.Dispose(); // Dispose the dummy ImageList
                listView.DrawItem -= ModulesListView_DrawItem;
                listView.DrawSubItem -= ModulesListView_DrawSubItem;
                listView.Dispose();
            }
        }

        // Custom drawing for the ListView to match the WPF style
        private void ModulesListView_DrawItem(object? sender, DrawListViewItemEventArgs e)
        {
            // We are doing all the drawing in DrawSubItem, so we just draw the background here.
            if (e.Item.Selected)
            {
                // Use a color similar to the WPF example's selection
                using (var brush = new SolidBrush(Color.FromArgb(76, 255, 102, 0)))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }
            }
            else
            {
                e.DrawBackground();
            }
            e.DrawFocusRectangle();
        }

        private void ModulesListView_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            // We need to re-draw the background for the sub-item to respect selection state
            if (e.Item.Selected)
            {
                using (var brush = new SolidBrush(Color.FromArgb(76, 255, 102, 0)))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }
            }
            else
            {
                e.DrawBackground();
            }

            // Define colors from the WPF example
            var darkOrange = Color.FromArgb(153, 61, 0);
            var lightOrange = Color.FromArgb(255, 153, 68);
            var lightPeach = Color.FromArgb(255, 204, 153);
            var emptyGray = Color.FromArgb(102, 51, 0);

            // Define fonts
            using var smallFont = new Font("Consolas", 8f);
            using var mainFont = new Font("Consolas", 9f);
            using var italicFont = new Font("Consolas", 9f, FontStyle.Italic);

            var textFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;

            // Get the module from the tag
            if (e.Item.Tag is not ShipModule module)
            {
                e.DrawText();
                return;
            }

            // --- Draw Slot and Size ---
            // The 'Class' property doesn't exist on ShipModule. We can show the slot name.
            string slotText = $"{module.Slot}";
            var slotRect = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top + 5, e.Bounds.Width - 16, smallFont.Height);
            TextRenderer.DrawText(e.Graphics, slotText, smallFont, slotRect, darkOrange, textFormat);

            // --- Draw Rating/Class if module is not empty ---
            if (!string.IsNullOrEmpty(module.Item))
            {
                // The 'Grade' and 'Class' properties don't exist. We can show the item name as a placeholder.
                string ratingText = $"{ItemNameService.TranslateModuleName(module.Item)}";
                var ratingSize = TextRenderer.MeasureText(e.Graphics, ratingText, smallFont, Size.Empty, textFormat);
                var ratingRect = new Rectangle(e.Bounds.Right - ratingSize.Width - 8, slotRect.Top, ratingSize.Width, smallFont.Height);
                TextRenderer.DrawText(e.Graphics, ratingText, smallFont, ratingRect, lightOrange, textFormat);
            }

            // --- Draw Module Name or "Empty" ---
            var nameRect = new Rectangle(slotRect.Left, slotRect.Bottom, e.Bounds.Width - 16, mainFont.Height);
            if (!string.IsNullOrEmpty(module.Item))
            {
                string name = ModuleDataService.GetModuleDisplayName(module);
                TextRenderer.DrawText(e.Graphics, name, mainFont, nameRect, lightPeach, textFormat);

                // --- Draw Stats (Power, etc.) ---
                // The Power property is on the Engineering object.
                var statsRect = new Rectangle(nameRect.Left, nameRect.Bottom + 2, e.Bounds.Width - 16, smallFont.Height);
                string statsText = "PWR: N/A";
                if (module.Engineering?.Modifiers != null)
                {
                    var powerModifier = module.Engineering.Modifiers.FirstOrDefault(m => m.Label.Equals("PowerDraw", StringComparison.OrdinalIgnoreCase));
                    if (powerModifier != null)
                    {
                        statsText = $"PWR: {powerModifier.Value:F2}";
                    }
                }
                
                TextRenderer.DrawText(e.Graphics, statsText, smallFont, statsRect, darkOrange, textFormat);
            }
            else
            {
                TextRenderer.DrawText(e.Graphics, "Empty", italicFont, nameRect, emptyGray, textFormat);
            }

            // Draw bottom border for the item
            using (var pen = new Pen(Color.FromArgb(50, 255, 102, 0)))
            {
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }
        }
    }
}