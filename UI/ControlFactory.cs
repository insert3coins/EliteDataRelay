using System;
using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Creates and manages the UI controls for the main form.
    /// </summary>
    public partial class ControlFactory : IDisposable
    {
        public Label ShipNameLabel { get; private set; } = null!;
        public Label ShipIdentLabel { get; private set; } = null!;
        public TreeView ShipModulesTreeView { get; private set; } = null!;
        public TabControl TabControl { get; private set; } = null!;
        public Button StartBtn { get; private set; } = null!;
        public Button StopBtn { get; private set; } = null!;
        public Button ExitBtn { get; private set; } = null!;        
        public Button AboutBtn { get; private set; } = null!;        
        public Button SessionBtn { get; private set; } = null!;
        public Button SettingsBtn { get; private set; } = null!;
        public Button WatchingLabel { get; private set; } = null!;
        public Button CargoHeaderLabel { get; private set; } = null!;
        public Button CargoSizeLabel { get; private set; } = null!;
        public Button CommanderLabel { get; private set; } = null!;
        public Button ShipLabel { get; private set; } = null!;
        public Button BalanceLabel { get; private set; } = null!;
        public ToolTip ToolTip { get; private set; } = null!;
        public MiningStatsControl MiningStatsControl { get; private set; } = null!;

        public ControlFactory(FontManager fontManager, Services.SessionTrackingService sessionTracker)
        {
            CreateTabControls(fontManager, sessionTracker);
            CreateActionButtons(fontManager);
            CreateInfoLabels(fontManager);
            CreateToolTips();
        }

        public void Dispose()
        {
            DisposeButtons();
            DisposeTabControls();
            DisposeLabels();
        }
    }
}