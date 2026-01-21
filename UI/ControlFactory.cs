using EliteDataRelay.Services;
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
        private readonly JournalHistoryService _journalHistoryService;
        public Label ShipNameLabel { get; private set; } = null!;
        public Label ShipIdentLabel { get; private set; } = null!;
        public TreeView ShipModulesTreeView { get; private set; } = null!;
        public TabControl TabControl { get; private set; } = null!;
        public HistoryTab? HistoryTab { get; private set; }
        public SessionTab? SessionTab { get; private set; }
        public MiningTab? MiningTab { get; private set; }
        public Button StartBtn { get; private set; } = null!;
        public Button StopBtn { get; private set; } = null!;
        public Button ExitBtn { get; private set; } = null!;        
        public Button AboutBtn { get; private set; } = null!;        
        public Label WatchingLabel { get; private set; } = null!;
        public Button SettingsBtn { get; private set; } = null!;
        public Button CargoHeaderLabel { get; private set; } = null!;
        public Button CargoSizeLabel { get; private set; } = null!;
        public Button CommanderLabel { get; private set; } = null!;
        public Button ShipLabel { get; private set; } = null!;
        public Button BalanceLabel { get; private set; } = null!;
        public Label EdsmStatusLabel { get; private set; } = null!;
        public ToolTip ToolTip { get; private set; } = null!;

        public ControlFactory(FontManager fontManager, SessionTrackingService sessionTracker, MiningTrackerService miningTracker, JournalHistoryService journalHistoryService)
        {
            _journalHistoryService = journalHistoryService ?? throw new ArgumentNullException(nameof(journalHistoryService));
            CreateTabControls(fontManager, sessionTracker, miningTracker, journalHistoryService);
            CreateActionButtons(fontManager);
            CreateInfoLabels(fontManager);
            CreateToolTips(fontManager);
        }

        public void Dispose()
        {
            ToolTip.Dispose();
            DisposeButtons();
            DisposeTabControls();
            DisposeCargoTabControls();
            DisposeLabels();
        }
    }
}
