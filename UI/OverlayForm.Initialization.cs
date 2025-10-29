using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm
    {
        private void InitializeControls()
        {
            if (_position == OverlayPosition.Info)
            {
                this.Size = new Size(320, 85);

                // Custom render panel for bitmap-cached drawing
                _renderPanel = new DoubleBufferedPanel
                {
                    Location = new Point(0, 0),
                    Size = this.ClientSize,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.Transparent
                };
                _renderPanel.Paint += OnInfoPanelPaint;

                Controls.Add(_renderPanel);
            }
            else if (_position == OverlayPosition.Cargo)
            {
                this.Size = new Size(280, 600);

                // Custom render panel for bitmap-cached drawing
                _renderPanel = new DoubleBufferedPanel
                {
                    Location = new Point(0, 0),
                    Size = this.ClientSize,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.Transparent
                };
                _renderPanel.Paint += OnCargoPanelPaint;

                Controls.Add(_renderPanel);
            }
            else if (_position == OverlayPosition.ShipIcon)
            {
                this.Size = new Size(320, 320);

                // Custom render panel for bitmap-cached drawing with double buffering
                _renderPanel = new DoubleBufferedPanel
                {
                    Location = new Point(0, 0),
                    Size = this.ClientSize,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.Transparent
                };
                _renderPanel.Paint += OnShipIconPanelPaint;

                Controls.Add(_renderPanel);
            }
            else if (_position == OverlayPosition.Exploration)
            {
                // Exploration overlay with FSS tracking: 340px wide x 195px tall
                this.Size = new Size(340, 195);

                // Custom render panel for bitmap-cached drawing (SrvSurvey PlotBase2 style)
                _renderPanel = new DoubleBufferedPanel
                {
                    Location = new Point(0, 0),
                    Size = this.ClientSize,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.Transparent
                };
                _renderPanel.Paint += OnExplorationPanelPaint;

                Controls.Add(_renderPanel);
            }
        }

        private Label CreateOverlayLabel(Point location, Font? font = null)
        {
            return new Label
            {
                Location = location,
                AutoSize = true,
                Font = font ?? _labelFont,
                ForeColor = AppConfiguration.OverlayTextColor,
                BackColor = Color.Transparent,
                Text = "" // Default to empty string 
            };
        }

        private Label CreateHeaderLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = _listFont,
                ForeColor = SystemColors.GrayText, // Use a dimmer color for headers
                BackColor = Color.Transparent,
                AutoSize = true
            };
        }
    }
}



