using System;
using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Manages the arrangement of controls on the main form.
    /// </summary>
    public class LayoutManager : IDisposable
    {
        private readonly Form _form;
        private readonly ControlFactory _controlFactory;
        private TableLayoutPanel? _bottomLayout;
        private FlowLayoutPanel? _buttonFlowPanel;
        private TableLayoutPanel? _infoPanel;
        private Label? _separator;
        private FlowLayoutPanel? _rightPanel;

        public LayoutManager(Form form, ControlFactory controlFactory)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _controlFactory = controlFactory ?? throw new ArgumentNullException(nameof(controlFactory));
        }

        public void ApplyLayout()
        {
            _form.SuspendLayout();
            _form.Controls.Clear(); // Clear existing layout to prevent conflicts

            // Create a FlowLayoutPanel for the buttons
            _buttonFlowPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
            };

            // Add controls to the button panel
            _buttonFlowPanel.Controls.Add(_controlFactory.WatchingLabel);
            _buttonFlowPanel.Controls.Add(_controlFactory.StartBtn);
            _buttonFlowPanel.Controls.Add(_controlFactory.StopBtn);
            _buttonFlowPanel.Controls.Add(_controlFactory.SessionBtn);
            _buttonFlowPanel.Controls.Add(_controlFactory.SettingsBtn);
            _buttonFlowPanel.Controls.Add(_controlFactory.AboutBtn);
            _buttonFlowPanel.Controls.Add(_controlFactory.ExitBtn);

            // Create a panel for the right-aligned items
            _rightPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
            };
            _rightPanel.Controls.Add(_controlFactory.CargoHeaderLabel);
            _rightPanel.Controls.Add(_controlFactory.CargoSizeLabel);

            // Use a TableLayoutPanel to hold the buttons at the bottom.
            _bottomLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(3)
            };
            _bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _bottomLayout.Controls.Add(_buttonFlowPanel, 0, 0);
            _bottomLayout.Controls.Add(_rightPanel, 1, 0);

            // Create a panel for the commander/ship/balance info
            _infoPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(5, 0, 5, 0),
            };
            _infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _infoPanel.Controls.Add(_controlFactory.CommanderLabel, 0, 0);
            _infoPanel.Controls.Add(_controlFactory.ShipLabel, 1, 0);
            _infoPanel.Controls.Add(_controlFactory.BalanceLabel, 2, 0);

            // Create a separator line
            _separator = new Label
            {
                Height = 2,
                Dock = DockStyle.Bottom,
                BorderStyle = BorderStyle.Fixed3D,
                Margin = Padding.Empty,
            };

            // Add controls to form. Order matters for DockStyle.Bottom.
            _controlFactory.TabControl.Dock = DockStyle.Fill;
            _form.Controls.Add(_controlFactory.TabControl);
            _form.Controls.Add(_infoPanel);
            _form.Controls.Add(_separator);
            _form.Controls.Add(_bottomLayout);
            _form.ResumeLayout(true);
        }

        public void Dispose()
        {
            _bottomLayout?.Dispose();
            _buttonFlowPanel?.Dispose();
            _infoPanel?.Dispose();
            _separator?.Dispose();
            _rightPanel?.Dispose();
        }
    }
}