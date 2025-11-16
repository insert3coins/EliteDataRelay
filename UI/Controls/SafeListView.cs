using System;
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
            var hadItems = Items.Count > 0;
            ListViewItem[]? buffer = null;
            if (hadItems)
            {
                buffer = new ListViewItem[Items.Count];
                Items.CopyTo(buffer, 0);
            }

            BeginUpdate();
            try
            {
                if (hadItems)
                {
                    Items.Clear();
                }

                try
                {
                    base.OnHandleCreated(e);
                }
                catch (ArgumentOutOfRangeException ex) when (IsStateImageIndexBug(ex))
                {
                    if (!TryCreateHandleWithoutStateImages(e))
                    {
                        throw;
                    }
                }

                if (hadItems && buffer != null)
                {
                    foreach (var item in buffer)
                    {
                        if (item != null)
                        {
                            NormalizeStateImageIndex(item);
                            Items.Add(item);
                        }
                    }
                }
            }
            finally
            {
                EndUpdate();
            }
        }

        private static bool IsStateImageIndexBug(ArgumentOutOfRangeException ex)
        {
            return string.Equals(ex.ParamName, "index", StringComparison.OrdinalIgnoreCase)
                   && ex.ActualValue is int value && value < 0;
        }

        private bool TryCreateHandleWithoutStateImages(EventArgs e)
        {
            var savedStateImages = StateImageList;
            StateImageList = null;
            try
            {
                base.OnHandleCreated(e);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
            finally
            {
                StateImageList = savedStateImages;
            }
        }

        private void NormalizeStateImageIndex(ListViewItem item)
        {
            if (StateImageList != null && item.StateImageIndex < 0)
            {
                item.StateImageIndex = 0;
            }
        }
    }
}
