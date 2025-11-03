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

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private const int HOTKEY_ID_START = 1;
        private const int HOTKEY_ID_STOP = 2;
        private const int HOTKEY_ID_SHOW = 3;
        private const int HOTKEY_ID_HIDE = 4;

        // Modifier flags for RegisterHotKey
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;

        // Virtual key constants used for state checking
        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12;     // Alt
        private const int VK_RMENU = 0xA5;    // Right Alt (AltGr)
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

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
            if ((key & Keys.Alt) == Keys.Alt) modifiers |= MOD_ALT;
            if ((key & Keys.Control) == Keys.Control) modifiers |= MOD_CONTROL;
            if ((key & Keys.Shift) == Keys.Shift) modifiers |= MOD_SHIFT;
            if ((key & Keys.LWin) == Keys.LWin || (key & Keys.RWin) == Keys.RWin) modifiers |= MOD_WIN;

            Keys keyCode = key & ~Keys.Modifiers;

            // Add MOD_NOREPEAT to avoid key-repeat firing when the key is held
            RegisterHotKey(this.Handle, id, modifiers | MOD_NOREPEAT, (uint)keyCode);
        }

        private static bool IsKeyDown(int vk) => (GetKeyState(vk) & 0x8000) != 0;

        private static uint CurrentModifierState()
        {
            uint mods = 0;
            if (IsKeyDown(VK_CONTROL)) mods |= MOD_CONTROL;
            if (IsKeyDown(VK_MENU)) mods |= MOD_ALT;
            if (IsKeyDown(VK_SHIFT)) mods |= MOD_SHIFT;
            if (IsKeyDown(VK_LWIN) || IsKeyDown(VK_RWIN)) mods |= MOD_WIN;
            return mods;
        }

        private static uint RequiredModifierState(Keys key)
        {
            uint mods = 0;
            if ((key & Keys.Control) == Keys.Control) mods |= MOD_CONTROL;
            if ((key & Keys.Alt) == Keys.Alt) mods |= MOD_ALT;
            if ((key & Keys.Shift) == Keys.Shift) mods |= MOD_SHIFT;
            if ((key & Keys.LWin) == Keys.LWin || (key & Keys.RWin) == Keys.RWin) mods |= MOD_WIN;
            return mods;
        }

        private static bool IsFunctionKey(Keys keyCode)
        {
            return keyCode >= Keys.F1 && keyCode <= Keys.F24;
        }

        // Ignore hotkey if:
        // - More modifiers are pressed than configured (enforce exact match)
        // - Or AltGr is being used (Right Alt acts as Ctrl+Alt) to avoid collisions while typing,
        //   except when the configured key is a function key (F1..F24) where AltGr isn't used.
        private static bool ShouldIgnoreHotkey(Keys configured)
        {
            var required = RequiredModifierState(configured);
            var current = CurrentModifierState();

            // Enforce exact modifier match
            if (current != required) return true;

            // If Ctrl+Alt combo is configured and Right-Alt (AltGr) is down, ignore for non-function keys
            var keyCode = configured & ~Keys.Modifiers;
            bool requiresCtrlAlt = (required & (MOD_CONTROL | MOD_ALT)) == (MOD_CONTROL | MOD_ALT);
            if (requiresCtrlAlt && IsKeyDown(VK_RMENU) && !IsFunctionKey(keyCode))
            {
                return true;
            }

            return false;
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
                        if (ShouldIgnoreHotkey(AppConfiguration.StartMonitoringHotkey)) return;
                        if (!_fileMonitoringService.IsMonitoring) StartMonitoring();
                        break;
                    case HOTKEY_ID_STOP:
                        if (ShouldIgnoreHotkey(AppConfiguration.StopMonitoringHotkey)) return;
                        if (_fileMonitoringService.IsMonitoring) OnStopClicked(null, EventArgs.Empty);
                        break;
                    case HOTKEY_ID_SHOW:
                        if (ShouldIgnoreHotkey(AppConfiguration.ShowOverlayHotkey)) return;
                        _cargoFormUI.ShowOverlays();
                        break;
                    case HOTKEY_ID_HIDE:
                        if (ShouldIgnoreHotkey(AppConfiguration.HideOverlayHotkey)) return;
                        _cargoFormUI.HideOverlays();
                        break;
                }
            }
        }

        #endregion
    }
}
