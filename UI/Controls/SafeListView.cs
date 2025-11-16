using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace EliteDataRelay.UI.Controls
{
    /// <summary>
    /// Wraps ListView handle creation to avoid the WinForms state-image bug that
    /// throws ArgumentOutOfRangeException when items exist before the handle.
    /// </summary>
    internal class SafeListView : ListView
    {
        protected override void OnHandleCreated(EventArgs e)
        {
            EnsureStateImages();
            base.OnHandleCreated(e);
        }

        private void EnsureStateImages()
        {
            foreach (ListViewItem? item in Items)
            {
                if (item == null) continue;
                if (item.StateImageIndex < 0)
                {
                    item.StateImageIndex = 0;
                }
            }
        }
    }
}
