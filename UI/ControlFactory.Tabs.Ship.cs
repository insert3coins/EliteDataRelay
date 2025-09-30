using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
        public PictureBox ShipIconPictureBox { get; private set; } = null!;
        public Label HullHealthValueLabel { get; private set; } = null!;
        public Label MassValueLabel { get; private set; } = null!;
        public Label FuelValueLabel { get; private set; } = null!;
        public Label CargoValueLabel { get; private set; } = null!;
        public Label JumpRangeValueLabel { get; private set; } = null!;
        public Label RebuyValueLabel { get; private set; } = null!;
        private TreeNode? _lastHoveredNode;

        private TabPage CreateShipTabPage(FontManager fontManager)
        {
            var shipPage = new TabPage("Ship");
            var shipPanel = new Panel { Dock = DockStyle.Fill };

            // Create the individual info and stats panels
            var shipInfoPanel = CreateShipInfoPanel(fontManager);
            var shipStatsPanel = CreateShipStatsPanel(fontManager);

            // Adjust padding for the new vertical layout
            shipInfoPanel.Padding = new Padding(10, 10, 10, 0);
            shipStatsPanel.Dock = DockStyle.Fill;
            shipStatsPanel.Padding = new Padding(10, 5, 10, 10);

            // Create a container to hold the info and stats panels stacked vertically
            var topContainerPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0) // No padding on the container itself
            };
            topContainerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            topContainerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            topContainerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            topContainerPanel.Controls.Add(shipInfoPanel, 0, 0);
            topContainerPanel.Controls.Add(shipStatsPanel, 0, 1);

            ShipModulesTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                ItemHeight = 38, // Taller rows for more info
                Font = fontManager.VerdanaFont,
                BorderStyle = BorderStyle.None,
                DrawMode = TreeViewDrawMode.OwnerDrawText,
                FullRowSelect = true,
                ShowLines = true,
                ShowNodeToolTips = false, // We use a custom tooltip component for owner drawing.
            };
            ShipModulesTreeView.BeforeSelect += ShipModulesTreeView_BeforeSelect;
            // Use a dedicated drawing handler for the ship loadout
            ShipModulesTreeView.DrawNode += ShipModulesTreeView_DrawNode;
            ShipModulesTreeView.MouseMove += ShipModulesTreeView_MouseMove;
            ShipModulesTreeView.MouseLeave += ShipModulesTreeView_MouseLeave;

            // Add the fill-docked control first to place it at the back of the Z-order.
            // Other docked controls will then claim their space from the edges, and the
            // Fill control will resize to occupy the remaining area.
            shipPanel.Controls.Add(ShipModulesTreeView);
            shipPanel.Controls.Add(topContainerPanel);
            shipPage.Controls.Add(shipPanel);

            return shipPage;
        }

        private TableLayoutPanel CreateShipInfoPanel(FontManager fontManager)
        {
            var shipInfoPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, // Fill the cell in the parent container
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10, 10, 5, 5) // Tweak padding for new layout
            };
            shipInfoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140)); // For image
            shipInfoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // For text

            ShipIconPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                Name = "ShipIconPictureBox"
            };
            

            var shipLabelsPanel = CreateShipLabelsPanel(fontManager);

            shipInfoPanel.Controls.Add(ShipIconPictureBox, 0, 0);
            shipInfoPanel.Controls.Add(shipLabelsPanel, 1, 0);

            return shipInfoPanel;
        }

        private TableLayoutPanel CreateShipStatsPanel(FontManager fontManager)
        {
            var statsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4, // Label, Value, Label, Value
                RowCount = 3,
                Padding = new Padding(10, 5, 10, 5),
            };
            statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var labelFont = fontManager.VerdanaFont;
            var valueFont = new Font(fontManager.VerdanaFont, FontStyle.Bold);

            // Row 0: Mass, Fuel
            statsPanel.Controls.Add(new Label { Text = "Mass:", Font = labelFont, AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            MassValueLabel = new Label { Text = "N/A", Font = valueFont, AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft };
            statsPanel.Controls.Add(MassValueLabel, 1, 0);

            statsPanel.Controls.Add(new Label { Text = "Fuel:", Font = labelFont, AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft }, 2, 0);
            FuelValueLabel = new Label { Text = "N/A", Font = valueFont, AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft };
            statsPanel.Controls.Add(FuelValueLabel, 3, 0);

            // Row 1: Jump Range, Cargo
            statsPanel.Controls.Add(new Label { Text = "Jump Range:", Font = labelFont, AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
            JumpRangeValueLabel = new Label { Text = "N/A", Font = valueFont, AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft };
            statsPanel.Controls.Add(JumpRangeValueLabel, 1, 1);

            statsPanel.Controls.Add(new Label { Text = "Cargo:", Font = labelFont, AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft }, 2, 1);
            CargoValueLabel = new Label { Text = "N/A", Font = valueFont, AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft };
            statsPanel.Controls.Add(CargoValueLabel, 3, 1);

            // Row 2: Rebuy, Hull Health
            statsPanel.Controls.Add(new Label { Text = "Rebuy:", Font = labelFont, AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
            RebuyValueLabel = new Label { Text = "N/A", Font = valueFont, AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft };
            statsPanel.Controls.Add(RebuyValueLabel, 1, 2);

            statsPanel.Controls.Add(new Label { Text = "Hull Health:", Font = labelFont, AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft }, 2, 2);
            HullHealthValueLabel = new Label { Text = "N/A", Font = valueFont, AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft };
            statsPanel.Controls.Add(HullHealthValueLabel, 3, 2);

            return statsPanel;
        }

        private void ShipModulesTreeView_DrawNode(object? sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node == null || e.Graphics == null) return;

            // We are taking full control of drawing.
            e.DrawDefault = false;

            var tree = e.Node.TreeView;
            Rectangle rowBounds = e.Bounds;

            bool isSelected = (e.State & TreeNodeStates.Selected) == TreeNodeStates.Selected;

            // 1. Determine colors and draw the background.
            Color backColor = isSelected ? SystemColors.Highlight : tree.BackColor;
            Color foreColor = isSelected ? SystemColors.HighlightText : e.Node.ForeColor;
            if (foreColor.IsEmpty) foreColor = tree.ForeColor;

            using (var backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, rowBounds);
            }

            // If it's a category node (we assume it has no tag)
            if (e.Node.Tag == null)
            {
                // Draw category header text (e.g., "Hardpoints (3)") with a bold font.
                using (var boldFont = new Font(tree.Font, FontStyle.Bold))
                {
                    // Define a drawing rectangle that respects the TreeView's client width to avoid drawing under the scrollbar.
                    var textBounds = new Rectangle(e.Bounds.Left, e.Bounds.Top, tree.ClientSize.Width - e.Bounds.Left - 4, e.Bounds.Height);
                    TextRenderer.DrawText(e.Graphics, e.Node.Text, boldFont, textBounds, foreColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }
                return;
            }

            // It's a module node. The Tag should contain our module info.
            if (e.Node.Tag is not Models.ShipModule module)
            {
                TextRenderer.DrawText(e.Graphics, e.Node.Text, tree.Font, e.Bounds, foreColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                return;
            }

            // --- Prepare text and fonts ---
            string name = Services.ModuleDataService.GetModuleDisplayName(module);
            string details = Services.ModuleDataService.GetModuleDetails(module);

            using var mainFont = new Font(tree.Font.FontFamily, 9f, FontStyle.Regular);
            using var detailFont = new Font(tree.Font.FontFamily, 8f, FontStyle.Regular);

            // --- Drawing ---
            int left = e.Bounds.Left + 2;
            int top = e.Bounds.Top + 2;

            // Use the TreeView's client width to avoid drawing under a potential scrollbar.
            // Subtract the node's horizontal position and add a small margin.
            int availableWidth = tree.ClientSize.Width - left - 4;

            // --- Line 1: Module Name and Details ---
            // Measure the details text first to reserve space for it.
            var detailSize = TextRenderer.MeasureText(e.Graphics, details, detailFont);
            const int detailPadding = 5;

            // To prevent text from being cut off, we draw the right-aligned text first.
            // Calculate the starting X position for the details text.
            int detailLeft = left + availableWidth - detailSize.Width;
            var detailRect = new Rectangle(detailLeft, top + 1, detailSize.Width, detailFont.Height);
            TextRenderer.DrawText(e.Graphics, details, detailFont, detailRect, isSelected ? Color.LightGray : Color.DimGray, TextFormatFlags.Left | TextFormatFlags.Top);

            // Now, calculate the remaining space for the main module name and draw it, truncating with an ellipsis if needed.
            int nameWidth = Math.Max(0, detailLeft - left - detailPadding);
            var nameRect = new Rectangle(left, top, nameWidth, mainFont.Height);
            TextRenderer.DrawText(e.Graphics, name, mainFont, nameRect, foreColor, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);

            // --- Line 2: Engineering Info ---
            if (module.Engineering != null)
            {
                var engRect = new Rectangle(left, nameRect.Bottom, availableWidth, detailFont.Height);
                float currentX = engRect.Left;

                // Define colors for blueprint and experimental effects
                var blueprintColor = isSelected ? Color.LightSkyBlue : Color.DodgerBlue;
                var experimentalColor = isSelected ? Color.Plum : Color.MediumPurple;
                var textFormat = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPadding;

                // 1. Draw Blueprint Name and Grade
                string blueprintText = $"{module.Engineering.BlueprintName} G{module.Engineering.Level}";
                TextRenderer.DrawText(e.Graphics, blueprintText, detailFont, new Point((int)currentX, engRect.Top), blueprintColor, textFormat);
                var blueprintSize = TextRenderer.MeasureText(e.Graphics, blueprintText, detailFont, Size.Empty, textFormat);
                currentX += blueprintSize.Width;

                // 2. Draw Experimental Effect, if any, and if it fits
                if (!string.IsNullOrEmpty(module.Engineering.ExperimentalEffect_Localised))
                {
                    string experimentalText = $", {module.Engineering.ExperimentalEffect_Localised}";
                    var experimentalSize = TextRenderer.MeasureText(e.Graphics, experimentalText, detailFont, Size.Empty, textFormat);
                    if (currentX + experimentalSize.Width <= engRect.Right)
                    {
                        TextRenderer.DrawText(e.Graphics, experimentalText, detailFont, new Point((int)currentX, engRect.Top), experimentalColor, textFormat);
                    }
                }
            }
        }

        private TableLayoutPanel CreateShipLabelsPanel(FontManager fontManager)
        {
            // Use a TableLayoutPanel to stack and center the labels.
            var shipLabelsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
            };
            shipLabelsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            shipLabelsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            ShipNameLabel = new Label
            {
                Font = new Font(fontManager.VerdanaFont.FontFamily, 10f, FontStyle.Bold),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomCenter,
                Padding = new Padding(0, 0, 0, 2) // Padding to push text up slightly
            };

            ShipIdentLabel = new Label
            {
                Font = fontManager.VerdanaFont,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopCenter,
                Padding = new Padding(0, 2, 0, 0) // Padding to push text down slightly
            };

            shipLabelsPanel.Controls.Add(ShipNameLabel, 0, 0);
            shipLabelsPanel.Controls.Add(ShipIdentLabel, 0, 1);
            return shipLabelsPanel;
        }

        private void DisposeShipTabControls()
        {
            ShipNameLabel?.Dispose();
            ShipIdentLabel?.Dispose();
            if (ShipModulesTreeView != null)
            {
                ShipModulesTreeView.DrawNode -= ShipModulesTreeView_DrawNode;
                ShipModulesTreeView.MouseMove -= ShipModulesTreeView_MouseMove;
                ShipModulesTreeView.MouseLeave -= ShipModulesTreeView_MouseLeave;
                ShipModulesTreeView.BeforeSelect -= ShipModulesTreeView_BeforeSelect;
                ShipModulesTreeView.Dispose();
            }
            HullHealthValueLabel?.Dispose();
            MassValueLabel?.Dispose();
            FuelValueLabel?.Dispose();
            CargoValueLabel?.Dispose();
            JumpRangeValueLabel?.Dispose();
            RebuyValueLabel?.Dispose();
        }

        private void ShipModulesTreeView_MouseMove(object? sender, MouseEventArgs e)
        {
            var node = ShipModulesTreeView.GetNodeAt(e.Location);
            if (node != _lastHoveredNode)
            {
                _lastHoveredNode = node;
                if (node != null && !string.IsNullOrEmpty(node.ToolTipText))
                {
                    ToolTip.SetToolTip(ShipModulesTreeView, node.ToolTipText);
                }
                else
                {
                    ToolTip.SetToolTip(ShipModulesTreeView, null);
                }
            }
        }

        private void ShipModulesTreeView_MouseLeave(object? sender, System.EventArgs e)
        {
            _lastHoveredNode = null;
            ToolTip.SetToolTip(ShipModulesTreeView, null);
        }

        private void ShipModulesTreeView_BeforeSelect(object? sender, TreeViewCancelEventArgs e)
        {
            // By canceling the BeforeSelect event, we prevent any node from being selected,
            // which in turn prevents the blue selection highlight from being drawn.
            e.Cancel = true;
        }
    }
}