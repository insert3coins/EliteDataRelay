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
            else if (_position == OverlayPosition.Session)
            {
                this.Size = new Size(260, 120);

                _renderPanel = new DoubleBufferedPanel
                {
                    Location = new Point(0, 0),
                    Size = this.ClientSize,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.Transparent
                };
                _renderPanel.Paint += OnSessionPanelPaint;

                Controls.Add(_renderPanel);
                ResizeSessionOverlay();
                ApplyRoundedRegion();
            }
            else if (_position == OverlayPosition.Exploration)
            {
                // Exploration overlay with FSS tracking: give generous fixed canvas for readability.
                this.Size = new Size(345, 230);

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
            else if (_position == OverlayPosition.Mining)
            {
                this.Size = new Size(520, 250);

                _renderPanel = new DoubleBufferedPanel
                {
                    Location = new Point(0, 0),
                    Size = this.ClientSize,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.Transparent
                };
                _renderPanel.Paint += OnMiningPanelPaint;

                Controls.Add(_renderPanel);
                ApplyRoundedRegion();
            }
            else if (_position == OverlayPosition.Prospector)
            {
                this.Size = new Size(320, 200);

                _renderPanel = new DoubleBufferedPanel
                {
                    Location = new Point(0, 0),
                    Size = this.ClientSize,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.Transparent
                };
                _renderPanel.Paint += OnProspectorPanelPaint;

                Controls.Add(_renderPanel);
                ApplyRoundedRegion();
            }
            else if (_position == OverlayPosition.JumpInfo)
            {
                // Slightly tighter canvas to reduce vertical whitespace.
                this.Size = new Size(435, 180);

                _renderPanel = new DoubleBufferedPanel
                {
                    Location = new Point(0, 0),
                    Size = this.ClientSize,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.Transparent
                };
                _renderPanel.Paint += OnJumpPanelPaint;

                Controls.Add(_renderPanel);
                ApplyRoundedRegion();
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
