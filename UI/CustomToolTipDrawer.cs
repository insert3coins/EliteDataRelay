using EliteDataRelay.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI

{
    /// <summary>
    /// Handles the custom owner-drawing logic for tooltips, providing color-coded
    /// information for engineered ship modules.
    /// </summary>
    public class CustomToolTipDrawer
    {
        private readonly Font _font;
        private readonly TreeView _shipModulesTreeView;

        public CustomToolTipDrawer(Font font, TreeView shipModulesTreeView)
        {
            _font = font;
            _shipModulesTreeView = shipModulesTreeView;
        }

        /// <summary>
        /// Handles the Popup event to correctly size the custom-drawn tooltip.
        /// </summary>
        public void ToolTip_Popup(object? sender, PopupEventArgs e)
        {
            if (sender is not ToolTip toolTip || e.AssociatedControl == null) return;

            string? text = toolTip.GetToolTip(e.AssociatedControl);
            if (string.IsNullOrEmpty(text))
            {
                e.Cancel = true;
                return;
            }

            using var g = e.AssociatedControl.CreateGraphics();
            var font = _font;

            var lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            int maxWidth = 0;
            int totalHeight = 0;
            foreach (var line in lines)
            {
                // Use the same flags as the DrawText method to ensure measurement is accurate.
                var size = TextRenderer.MeasureText(g, line, font, Size.Empty, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                if (size.Width > maxWidth)
                {
                    maxWidth = size.Width;
                }
                totalHeight += size.Height;
            }

            e.ToolTipSize = new Size(maxWidth + 6, totalHeight + 4);
        }

        /// <summary>
        /// Handles the Draw event to perform custom rendering of the tooltip.
        /// </summary>
        public void ToolTip_Draw(object? sender, DrawToolTipEventArgs e)
        {
            e.DrawBackground();
            e.DrawBorder();

            // Check if this is a tooltip for a ship module node
            if (e.AssociatedControl is TreeView tv && tv == _shipModulesTreeView)
            {
                var p = tv.PointToClient(Cursor.Position);
                var node = tv.GetNodeAt(p);

                // If we are over a valid module node with engineering, use our custom drawing logic
                if (node?.Tag is ShipModule module && module.Engineering != null)
                {
                    DrawModuleToolTip(e);
                    return;
                }
            }

            // For all other tooltips, use the default text drawing which handles multiline correctly.
            e.DrawText(TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        /// <summary>
        /// Draws a color-coded tooltip for an engineered ship module.
        /// </summary>
        private void DrawModuleToolTip(DrawToolTipEventArgs e)
        {
            if (string.IsNullOrEmpty(e.ToolTipText))
            {
                return;
            }

            // Define colors for different parts of the tooltip
            var goodColor = Color.FromArgb(139, 233, 134); // Light Green
            var badColor = Color.FromArgb(255, 121, 121);  // Light Red
            var blueprintColor = Color.FromArgb(138, 173, 255); // Light Blue
            var experimentalColor = Color.FromArgb(199, 146, 234); // Light Purple

            // Use the same font as in ToolTip_Popup for consistency to ensure sizing is correct.
            var font = _font;
            // The default text color for a tooltip. e.ForeColor is not available on DrawToolTipEventArgs.
            var defaultColor = SystemColors.InfoText;
            var lines = e.ToolTipText.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            float currentY = e.Bounds.Y + 2;

            // Remove VerticalCenter as we are manually positioning each line by its Y-coordinate.
            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

            foreach (var line in lines)
            {
                var lineSize = TextRenderer.MeasureText(line, font);
                var trimmedLine = line.Trim();
                float currentX = e.Bounds.X + 3;

                if (trimmedLine.EndsWith("▲") || trimmedLine.EndsWith("▼"))
                {
                    // This is a modification line, draw it in parts
                    string indicator = trimmedLine.EndsWith("▲") ? " ▲" : " ▼";
                    Color indicatorColor = trimmedLine.EndsWith("▲") ? goodColor : badColor;

                    // The main text is everything before the indicator
                    string mainText = line.Substring(0, line.LastIndexOf(indicator));

                    // Draw the main text part
                    TextRenderer.DrawText(e.Graphics, mainText, font, new Point((int)currentX, (int)currentY), defaultColor, flags);

                    // Measure the main text to find where to draw the indicator
                    // Use MeasureText without NoPadding for positioning, as it's more consistent with DrawText.
                    var mainPartSize = TextRenderer.MeasureText(e.Graphics, mainText, font, Size.Empty, TextFormatFlags.Left | TextFormatFlags.SingleLine);
                    currentX += mainPartSize.Width;

                    // Draw the colored indicator
                    TextRenderer.DrawText(e.Graphics, indicator, font, new Point((int)currentX, (int)currentY), indicatorColor, flags);
                }
                else if (trimmedLine.StartsWith("Blueprint:"))
                {
                    TextRenderer.DrawText(e.Graphics, line, font, new Point((int)currentX, (int)currentY), blueprintColor, flags);
                }
                else if (trimmedLine.StartsWith("Experimental:"))
                {
                    TextRenderer.DrawText(e.Graphics, line, font, new Point((int)currentX, (int)currentY), experimentalColor, flags);
                }
                else
                {
                    // Draw a standard line
                    TextRenderer.DrawText(e.Graphics, line, font, new Point((int)currentX, (int)currentY), defaultColor, flags);
                }

                currentY += lineSize.Height;
            }
        }
    }
}