using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.UI

{
    public partial class OverlayForm
    {
        private void OnCargoListPanelPaint(object? sender, PaintEventArgs e)
        {
            // Use higher quality text rendering for clarity in-game.
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using (var textBrush = new SolidBrush(AppConfiguration.OverlayTextColor))
            using (var grayBrush = new SolidBrush(SystemColors.GrayText))
            {
                // 1. Draw the header elements at the top of the panel.
                // "Cargo:" (Header Color)
                e.Graphics.DrawString(_cargoHeaderLabel.Text, _listFont, grayBrush, 10, 8);
                var cargoHeaderTextSize = e.Graphics.MeasureString(_cargoHeaderLabel.Text, _listFont);

                // "▰▰▱▱..." (Header Color, right-aligned)
                var cargoBarTextSize = e.Graphics.MeasureString(_cargoBarLabel.Text, _listFont);
                e.Graphics.DrawString(_cargoBarLabel.Text, _listFont, grayBrush, e.ClipRectangle.Width - cargoBarTextSize.Width - 10, 8);

                // "128/256" (User-configurable Color, centered between the other two elements)
                var cargoSizeTextSize = e.Graphics.MeasureString(_cargoSizeLabel.Text, _listFont);
                float leftEdge = 10 + cargoHeaderTextSize.Width;
                float rightEdge = e.ClipRectangle.Width - cargoBarTextSize.Width - 10;
                float centeredX = leftEdge + ((rightEdge - leftEdge - cargoSizeTextSize.Width) / 2);
                e.Graphics.DrawString(_cargoSizeLabel.Text, _listFont, textBrush, centeredX, 8);

                // 2. Draw the cargo list, starting below the header area.
                float y = 35.0f;
                const float xName = 10.0f;
                const float xCount = 200.0f;

                var itemsToDraw = _cargoItems.ToList();

                if (!itemsToDraw.Any())
                {
                    e.Graphics.DrawString("Cargo hold is empty.", _listFont, grayBrush, xName, y);
                    return;
                }

                foreach (var item in itemsToDraw)
                {
                    string displayName = !string.IsNullOrEmpty(item.Localised) ? item.Localised : item.Name;
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        displayName = char.ToUpperInvariant(displayName[0]) + displayName.Substring(1);
                    }

                    if (displayName != null)
                    {
                        e.Graphics.DrawString(displayName, _listFont, textBrush, xName, y);
                        e.Graphics.DrawString(item.Count.ToString(), _listFont, textBrush, xCount, y);
                    }

                    y += _listFont.GetHeight(e.Graphics);
                }
            }
        }

    }
}