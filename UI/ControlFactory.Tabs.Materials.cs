using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
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

        private void DisposeMaterialsTabControls()
        {
            MaterialTreeView.DrawNode -= TreeView_DrawNode;
            MaterialTreeView.Dispose();
            MaterialSearchBox.Dispose();
            PinMaterialsCheckBox.Dispose();
        }
    }
}