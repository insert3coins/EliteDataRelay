using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay
{
    public partial class CargoForm
    {
        #region Hotkey P/Invoke

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_START = 1;
        private const int HOTKEY_ID_STOP = 2;
        private const int HOTKEY_ID_SHOW = 3;
        private const int HOTKEY_ID_HIDE = 4;

        #endregion

        #region Hotkey Handling

        private void RegisterHotkeys()
        {
            RegisterHotkey(HOTKEY_ID_START, AppConfiguration.StartMonitoringHotkey);
            RegisterHotkey(HOTKEY_ID_STOP, AppConfiguration.StopMonitoringHotkey);
            RegisterHotkey(HOTKEY_ID_SHOW, AppConfiguration.ShowOverlayHotkey);
            RegisterHotkey(HOTKEY_ID_HIDE, AppConfiguration.HideOverlayHotkey);
        }

        private void UnregisterHotkeys()
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID_START);
            UnregisterHotKey(this.Handle, HOTKEY_ID_STOP);
            UnregisterHotKey(this.Handle, HOTKEY_ID_SHOW);
            UnregisterHotKey(this.Handle, HOTKEY_ID_HIDE);
        }

        private void RegisterHotkey(int id, Keys key)
        {
            if (key == Keys.None) return;

            uint modifiers = 0;
            if ((key & Keys.Alt) == Keys.Alt) modifiers |= 1;
            if ((key & Keys.Control) == Keys.Control) modifiers |= 2;
            if ((key & Keys.Shift) == Keys.Shift) modifiers |= 4;

            Keys keyCode = key & ~Keys.Modifiers;

            RegisterHotKey(this.Handle, id, modifiers, (uint)keyCode);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                switch (id)
                {
                    case HOTKEY_ID_START:
                        if (!_fileMonitoringService.IsMonitoring) StartMonitoring();
                        break;
                    case HOTKEY_ID_STOP:
                        if (_fileMonitoringService.IsMonitoring) OnStopClicked(null, EventArgs.Empty);
                        break;
                    case HOTKEY_ID_SHOW:
                        _cargoFormUI.ShowOverlays();
                        break;
                    case HOTKEY_ID_HIDE:
                        _cargoFormUI.HideOverlays();
                        break;
                }
            }
        }

        #endregion
    }
}