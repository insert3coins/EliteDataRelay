using System;
using System.Windows.Forms;
using EliteCargoMonitor.Configuration;

namespace EliteCargoMonitor.UI
{
    /// <summary>
    /// Manages the layout of controls on the main form.
    /// </summary>
    public class LayoutManager : IDisposable
    {
        private readonly Form _form;
        private readonly ControlFactory _controls;
        private TableLayoutPanel? _bottomPanel;
        private FlowLayoutPanel? _buttonFlowPanel;
        private FlowLayoutPanel? _rightPanel;

        public LayoutManager(Form form, ControlFactory controls)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _controls = controls ?? throw new ArgumentNullException(nameof(controls));
        }

        public void ApplyLayout()
        {
            // Create a FlowLayoutPanel for the buttons
            _buttonFlowPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                Anchor = AnchorStyles.Left, // Vertically center and align left
            };

            // Add controls to the button panel
            _buttonFlowPanel.Controls.Add(_controls.WatchingLabel);
            _buttonFlowPanel.Controls.Add(_controls.StartBtn);
            _buttonFlowPanel.Controls.Add(_controls.StopBtn);
            _buttonFlowPanel.Controls.Add(_controls.AboutBtn);
            _buttonFlowPanel.Controls.Add(_controls.SettingsBtn);
            _buttonFlowPanel.Controls.Add(_controls.ExitBtn);

            // Create a panel for the right-aligned items
            _rightPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                Anchor = AnchorStyles.Right, // Vertically center and align right
            };
            _rightPanel.Controls.Add(_controls.CargoHeaderLabel);
            _rightPanel.Controls.Add(_controls.CargoSizeLabel);

            // Use a TableLayoutPanel to hold the buttons at the bottom, which helps with vertical alignment.
            _bottomPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = AppConfiguration.ButtonPanelHeight,
                ColumnCount = 2,
                RowCount = 1,
            };
            _bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _bottomPanel.Controls.Add(_buttonFlowPanel, 0, 0);
            _bottomPanel.Controls.Add(_rightPanel, 1, 0);

            // Add controls to form
            _form.Controls.Add(_controls.ListView);
            _form.Controls.Add(_bottomPanel);
        }

        public void Dispose()
        {
            _bottomPanel?.Dispose();
            _buttonFlowPanel?.Dispose();
            _rightPanel?.Dispose();
        }
    }
}