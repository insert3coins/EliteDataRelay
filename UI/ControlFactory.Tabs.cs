using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
        public Label HullHealthValueLabel { get; private set; } = null!;
        public Label MassValueLabel { get; private set; } = null!;
        public Label FuelValueLabel { get; private set; } = null!;
        public Label CargoValueLabel { get; private set; } = null!;
        public Label JumpRangeValueLabel { get; private set; } = null!;
        public Label RebuyValueLabel { get; private set; } = null!;

        private void CreateTabControls(FontManager fontManager)
        {
            // Tab control to switch between Cargo and Materials
            TabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = fontManager.VerdanaFont,
            };

            var cargoPage = CreateCargoTabPage(fontManager);
            var materialsPage = CreateMaterialsTabPage(fontManager);
            var shipPage = CreateShipTabPage(fontManager);

            TabControl.TabPages.AddRange(new[] { cargoPage, materialsPage, shipPage });
        }

        private TabPage CreateCargoTabPage(FontManager fontManager)
        {
            var cargoPage = new TabPage("Cargo");

            // Main ListView to display cargo items
            ListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                Font = fontManager.VerdanaFont,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.None,
                BackColor = SystemColors.Window, // Use standard window background
                GridLines = false // Cleaner look without grid lines
            };

            // Define columns for the ListView
            ListView.Columns.Add("Commodity", 200, HorizontalAlignment.Left);
            ListView.Columns.Add("Count", 80, HorizontalAlignment.Center);
            ListView.Columns.Add("Category", -2, HorizontalAlignment.Center);

            cargoPage.Controls.Add(ListView);
            return cargoPage;
        }

        private TabPage CreateMaterialsTabPage(FontManager fontManager)
        {
            var materialsPage = new TabPage("Materials");

            // Material TreeView
            MaterialTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = fontManager.VerdanaFont,
                BackColor = SystemColors.Window,
                ForeColor = SystemColors.WindowText,
                CheckBoxes = true,
                DrawMode = TreeViewDrawMode.OwnerDrawText,
                FullRowSelect = true,
                ShowLines = true,
            };
            MaterialTreeView.DrawNode += TreeView_DrawNode;

            // Create a container panel for the materials tab
            var materialsPanel = new Panel { Dock = DockStyle.Fill };

            // Create a panel for the top controls (checkbox and search)
            var topControlsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(3, 0, 3, 0),
                WrapContents = false
            };

            // Checkbox to toggle pinned materials view
            PinMaterialsCheckBox = new CheckBox
            {
                Text = "Show Pinned Materials Only",
                AutoSize = true,
                Margin = new Padding(0, 6, 10, 3)
            };

            // Search label and textbox
            var searchLabel = new Label { Text = "Search:", AutoSize = true, Margin = new Padding(3, 9, 0, 3) };
            MaterialSearchBox = new TextBox
            {
                Width = 150,
                Margin = new Padding(3, 6, 3, 3),
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.CustomSource
            };

            topControlsPanel.Controls.Add(PinMaterialsCheckBox);
            topControlsPanel.Controls.Add(searchLabel);
            topControlsPanel.Controls.Add(MaterialSearchBox);

            // Add the fill-docked control first to place it at the back of the Z-order.
            materialsPanel.Controls.Add(MaterialTreeView);
            // Add other docked controls. They will be placed on top and will claim their space.
            materialsPanel.Controls.Add(topControlsPanel);
            materialsPage.Controls.Add(materialsPanel);

            return materialsPage;
        }

        private TabPage CreateShipTabPage(FontManager fontManager)
        {
            var shipPage = new TabPage("Ship");
            var shipPanel = new Panel { Dock = DockStyle.Fill };

            // Create the individual info and stats panels
            var shipInfoPanel = CreateShipInfoPanel(fontManager);
            var shipStatsPanel = CreateShipStatsPanel(fontManager);
            shipStatsPanel.Dock = DockStyle.Fill; // Override the default DockStyle.Top
            shipStatsPanel.Padding = new Padding(5, 10, 10, 5); // Tweak padding for the new layout

            // Create a container to hold the info and stats panels side-by-side
            var topContainerPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0) // No padding on the container itself
            };
            topContainerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            topContainerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            topContainerPanel.Controls.Add(shipInfoPanel, 0, 0);
            topContainerPanel.Controls.Add(shipStatsPanel, 1, 0);

            ShipModulesTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                ItemHeight = 38, // Taller rows for more info
                Font = fontManager.VerdanaFont,
                BorderStyle = BorderStyle.None,
                DrawMode = TreeViewDrawMode.OwnerDrawText,
                FullRowSelect = true,
                ShowLines = true,
                ShowNodeToolTips = true,
            };
            // Use a dedicated drawing handler for the ship loadout
            ShipModulesTreeView.DrawNode += ShipModulesTreeView_DrawNode;

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
                BorderStyle = BorderStyle.FixedSingle
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
            string engineeringText = Services.ModuleDataService.GetEngineeringInfo(module);

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
            if (!string.IsNullOrEmpty(engineeringText))
            {
                // Use the full available width for the second line and add ellipsis if needed.
                var engRect = new Rectangle(left, nameRect.Bottom, availableWidth, detailFont.Height);
                TextRenderer.DrawText(e.Graphics, engineeringText, detailFont, engRect, isSelected ? Color.LightSkyBlue : Color.DodgerBlue, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
            }
        }

        private void TreeView_DrawNode(object? sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node == null) return;

            // We are taking full control of drawing for all nodes to ensure consistent spacing and appearance,
            // as the default drawing behavior is unreliable with OwnerDrawText and FullRowSelect.
            e.DrawDefault = false;

            var tree = e.Node.TreeView;
            Rectangle rowBounds = e.Bounds;

            bool isSelected = (e.State & TreeNodeStates.Selected) == TreeNodeStates.Selected;

            // 1. Determine colors and draw the background for the entire row.
            Color backColor = isSelected ? Color.FromArgb(0, 120, 215) : tree.BackColor;
            Color foreColor = isSelected ? Color.White : e.Node.ForeColor;
            if (foreColor.IsEmpty) // If no color is set on the node, use the tree's default.
            {
                foreColor = tree.ForeColor;
            }

            using (var backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, rowBounds);
            }

            // 2. Calculate the correct indented position for the node's content (checkbox and text).
            // The offset of '20' accounts for the root-level expand/collapse glyph.
            int nodeContentLeft = (e.Node.Level * tree.Indent) + 20;
            // Use the TreeView's ClientSize.Width to determine the available width for content,
            // preventing it from being drawn under a vertical scrollbar. e.Bounds.Width is unreliable
            // when a scrollbar is visible.
            int contentWidth = tree.ClientSize.Width - nodeContentLeft;
            Rectangle nodeContentBounds = new Rectangle(nodeContentLeft, rowBounds.Top, contentWidth, rowBounds.Height);

            Rectangle textBounds = nodeContentBounds;

            // 3. Draw the checkbox, if enabled.
            if (tree.CheckBoxes)
            {
                const int checkboxSize = 14;
                const int checkboxPadding = 4;
                int checkboxY = nodeContentBounds.Top + (nodeContentBounds.Height - checkboxSize) / 2;
                Point checkboxLocation = new Point(nodeContentBounds.Left, checkboxY);
                var checkboxRect = new Rectangle(checkboxLocation, new Size(checkboxSize, checkboxSize));

                var state = e.Node.Checked ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal;

                // If selected, we must draw a standard background behind the checkbox so it's visible against the highlight.
                if (isSelected)
                {
                    using (var checkboxBackBrush = new SolidBrush(tree.BackColor))
                    {
                        e.Graphics.FillRectangle(checkboxBackBrush, checkboxRect);
                    }
                }
                CheckBoxRenderer.DrawCheckBox(e.Graphics, checkboxLocation, state);

                // Adjust the text bounds to be to the right of the checkbox, adding padding.
                textBounds = new Rectangle(nodeContentBounds.Left + checkboxSize + checkboxPadding, nodeContentBounds.Top, nodeContentBounds.Width - (checkboxSize + checkboxPadding), nodeContentBounds.Height);
            }

            // 4. Draw the node's text in the calculated bounds.
            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
            TextRenderer.DrawText(e.Graphics, e.Node.Text, tree.Font, textBounds, foreColor, flags);
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

        private void DisposeTabControls()
        {
            TabControl.Dispose();
            MaterialTreeView.DrawNode -= TreeView_DrawNode;
            MaterialTreeView.Dispose();
            MaterialSearchBox.Dispose();
            ShipIconPictureBox.Dispose();
            ShipNameLabel.Dispose();
            ShipIdentLabel.Dispose();
            ShipModulesTreeView.DrawNode -= ShipModulesTreeView_DrawNode;
            ShipModulesTreeView.Dispose();
            PinMaterialsCheckBox.Dispose();
            ListView.Dispose();
            HullHealthValueLabel.Dispose();
            MassValueLabel.Dispose();
            FuelValueLabel.Dispose();
            CargoValueLabel.Dispose();
            JumpRangeValueLabel.Dispose();
            RebuyValueLabel.Dispose();
        }
    }
}