using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
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

            // Use a TableLayoutPanel for a more robust, responsive layout.
            var shipInfoPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10)
            };
            shipInfoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140)); // For image
            shipInfoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // For text

            ShipIconPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Use a TableLayoutPanel to stack and center the labels.
            var shipLabelsPanel = CreateShipLabelsPanel(fontManager);

            shipInfoPanel.Controls.Add(ShipIconPictureBox, 0, 0);
            shipInfoPanel.Controls.Add(shipLabelsPanel, 1, 0);

            ShipModulesTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = fontManager.VerdanaFont,
                BorderStyle = BorderStyle.None,
                DrawMode = TreeViewDrawMode.OwnerDrawText,
                FullRowSelect = true,
                ShowLines = true,
            };
            ShipModulesTreeView.DrawNode += TreeView_DrawNode;

            // Add the fill-docked control first to place it at the back of the Z-order.
            // Other docked controls will then claim their space from the edges, and the
            // Fill control will resize to occupy the remaining area.
            shipPanel.Controls.Add(ShipModulesTreeView);
            shipPanel.Controls.Add(shipInfoPanel);
            shipPage.Controls.Add(shipPanel);

            return shipPage;
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
                Font = new Font(fontManager.VerdanaFont.FontFamily, 12f, FontStyle.Bold),
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
            ShipModulesTreeView.DrawNode -= TreeView_DrawNode;
            ShipModulesTreeView.Dispose();
            PinMaterialsCheckBox.Dispose();
            ListView.Dispose();
        }
    }
}