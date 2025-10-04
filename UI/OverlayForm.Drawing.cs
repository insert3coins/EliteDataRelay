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

            // Define constants for layout to improve readability and maintainability.
            const float PADDING = 10.0f;
            const float HEADER_Y = 8.0f;
            const float LIST_START_Y = 35.0f;
            const float LIST_X_NAME = PADDING;
            const float LIST_X_COUNT = 200.0f;

            // --- 1. Draw Header ---
            // "Cargo:" (Header Color, left-aligned)
            e.Graphics.DrawString(_cargoHeaderLabel.Text, _listFont, _grayBrush, PADDING, HEADER_Y);
            var cargoHeaderTextSize = e.Graphics.MeasureString(_cargoHeaderLabel.Text, _listFont);

            // "▰▰▱▱..." (Header Color, right-aligned)
            var cargoBarTextSize = e.Graphics.MeasureString(_cargoBarLabel.Text, _listFont);
            e.Graphics.DrawString(_cargoBarLabel.Text, _listFont, _grayBrush, e.ClipRectangle.Width - cargoBarTextSize.Width - PADDING, HEADER_Y);

            // "128/256" (User-configurable Color, centered between the other two elements)
            var cargoSizeTextSize = e.Graphics.MeasureString(_cargoSizeLabel.Text, _listFont);
            float leftEdge = PADDING + cargoHeaderTextSize.Width;
            float rightEdge = e.ClipRectangle.Width - cargoBarTextSize.Width - PADDING;
            float centeredX = leftEdge + ((rightEdge - leftEdge - cargoSizeTextSize.Width) / 2);
            e.Graphics.DrawString(_cargoSizeLabel.Text, _listFont, _textBrush, centeredX, HEADER_Y);


            // --- 2. Draw Cargo List ---
            float y = LIST_START_Y;

            // The .ToList() call is redundant as _cargoItems is already a list.
            // We can iterate over it directly.
            if (!_cargoItems.Any())
            {
                e.Graphics.DrawString("Cargo hold is empty.", _listFont, _grayBrush, LIST_X_NAME, y);
                return;
            }

            foreach (var item in _cargoItems)
            {
                string displayName = !string.IsNullOrEmpty(item.Localised) ? item.Localised : item.Name;
                if (!string.IsNullOrEmpty(displayName))
                {
                    displayName = char.ToUpperInvariant(displayName[0]) + displayName.Substring(1);
                }

                e.Graphics.DrawString(displayName ?? string.Empty, _listFont, _textBrush, LIST_X_NAME, y);
                e.Graphics.DrawString(item.Count.ToString(), _listFont, _textBrush, LIST_X_COUNT, y);

                y += _listFont.GetHeight(e.Graphics);
            }
        }

    }
}