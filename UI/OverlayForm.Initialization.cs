using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm
    {
        private const int OverlayCornerRadius = 12;

        private void ApplyRoundedRegion()
        {
            try
            {
                using (var path = DrawingUtils.CreateRoundedRectPath(new Rectangle(0, 0, this.Width, this.Height), OverlayCornerRadius))
                {
                    this.Region?.Dispose();
                    this.Region = new Region(path);
                }
            }
            catch
            {
                // If shaping fails, fall back to normal rectangular region.
            }
        }

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
                ApplyRoundedRegion();
            }
            else if (_position == OverlayPosition.Cargo)
            {
                this.Size = new Size(280, 200);

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
                // Autosize to current content (empty state) so it isn't oversized on start
                ResizeCargoToContent();
                ApplyRoundedRegion();
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
                ApplyRoundedRegion();
            }
            else if (_position == OverlayPosition.Exploration)
            {
                // Exploration overlay with FSS tracking: increased to fit completion/signals/codex rows
                // Previous: 340x195. New: 360x255 for improved readability.
                this.Size = new Size(360, 225);

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
                ApplyRoundedRegion();
            }
            // JumpInfo overlay removed
            
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
