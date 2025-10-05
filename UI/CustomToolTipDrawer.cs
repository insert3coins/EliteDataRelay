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

        public CustomToolTipDrawer(Font font)
        {
            _font = font;
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
            if (sender is not ToolTip toolTip) return;

            // Manually draw the background using the BackColor we set on the ToolTip control.
            e.Graphics.FillRectangle(new SolidBrush(toolTip.BackColor), e.Bounds);
            e.DrawBorder(); // Keep the standard border

            // Check if this is a tooltip for our custom ModulePanel
            if (e.AssociatedControl is CargoFormUI.ModulePanel modulePanel && modulePanel.Tag is ShipModule module && module.Engineering != null)
            {
                DrawModuleToolTip(e, toolTip.ForeColor);
                return;
            }

            // For all other tooltips, use the default text drawing which handles multiline correctly.
            // We pass the ForeColor from the event args to ensure light text on our dark background.
            TextRenderer.DrawText(e.Graphics, e.ToolTipText, _font, e.Bounds, toolTip.ForeColor, 
                                  TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        /// <summary>
        /// Draws a color-coded tooltip for an engineered ship module.
        /// </summary>
        /// <param name="e">The event arguments for drawing.</param>
        /// <param name="defaultColor">The default text color to use for non-highlighted lines.</param>
        private void DrawModuleToolTip(DrawToolTipEventArgs e, Color defaultColor)
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
                    Color indicatorColor = trimmedLine.EndsWith("▲") ? goodColor : badColor;
                    
                    // Draw the entire line with the appropriate color
                    TextRenderer.DrawText(e.Graphics, line, font, new Point((int)currentX, (int)currentY), indicatorColor, flags);
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