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
                // 1. Draw the header text first at the top of the panel.
                e.Graphics.DrawString(_cargoLabel.Text, _labelFont, textBrush, 10, 5);
                var cargoTextSize = e.Graphics.MeasureString(_cargoLabel.Text, _labelFont);
                e.Graphics.DrawString(_cargoSizeLabel.Text, _listFont, textBrush, 10 + cargoTextSize.Width + 10, 8);

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