using EliteDataRelay.Services;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
        private TabPage CreateHistoryTabPage(FontManager fontManager, JournalHistoryService historyService)
        {
            HistoryTab?.Dispose();
            HistoryTab = new HistoryTab(historyService, fontManager);
            return HistoryTab;
        }
    }
}
