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

            var snapshot = new List<ListViewItem>(Items.Count);
            foreach (ListViewItem item in Items)
            {
                if (item == null) continue;
                snapshot.Add(CloneItem(item));
            }

            BeginUpdate();
            try
            {
                Items.Clear();
                base.OnHandleCreated(e);
                Items.AddRange(snapshot.ToArray());
            }
            finally
            {
                EndUpdate();
            }
        }

        private static ListViewItem CloneItem(ListViewItem source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var clone = new ListViewItem(source.Text)
            {
                ImageIndex = source.ImageIndex,
                ImageKey = source.ImageKey,
                StateImageIndex = source.StateImageIndex,
                Tag = source.Tag,
                BackColor = source.BackColor,
                ForeColor = source.ForeColor,
                Font = source.Font,
                Name = source.Name,
                ToolTipText = source.ToolTipText
            };

            for (var i = 1; i < source.SubItems.Count; i++)
            {
                clone.SubItems.Add(source.SubItems[i].Text);
            }

            return clone;
        }
    }
}
