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
            if (Items.Count == 0)
            {
                base.OnHandleCreated(e);
                return;
            }

            var buffer = new ListViewItem[Items.Count];
            Items.CopyTo(buffer, 0);

            BeginUpdate();
            try
            {
                Items.Clear();
                base.OnHandleCreated(e);
                foreach (var item in buffer)
                {
                    if (item != null)
                    {
                        Items.Add(item);
                    }
                }
            }
            finally
            {
                EndUpdate();
            }
        }
    }
}
