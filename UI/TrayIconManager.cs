using System;
using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Manages the application's system tray icon and context menu.
    /// </summary>
    public class TrayIconManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _trayMenu;
        private readonly ToolStripMenuItem _trayMenuShow;
        private readonly ToolStripMenuItem _trayMenuStart;
        private readonly ToolStripMenuItem _trayMenuStop;
        private readonly ToolStripMenuItem _trayMenuExit;

        public event EventHandler? ShowApplicationClicked;
        public event EventHandler? StartClicked;
        public event EventHandler? StopClicked;
        public event EventHandler? ExitClicked;

        public TrayIconManager(Icon? appIcon)
        {
            _trayMenuShow = new ToolStripMenuItem("Show");
            _trayMenuStart = new ToolStripMenuItem("Start");
            _trayMenuStop = new ToolStripMenuItem("Stop") { Enabled = false };
            _trayMenuExit = new ToolStripMenuItem("Exit");

            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.AddRange(new ToolStripItem[] {
                _trayMenuShow,
                new ToolStripSeparator(),
                _trayMenuStart,
                _trayMenuStop,
                new ToolStripSeparator(),
                _trayMenuExit
            });

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Text = "Elite Data Relay";
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenuStrip = _trayMenu;
            _notifyIcon.Icon = appIcon;

            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            _notifyIcon.DoubleClick += (s, e) => ShowApplicationClicked?.Invoke(s, e);
            _trayMenuShow.Click += (s, e) => ShowApplicationClicked?.Invoke(s, e);
            _trayMenuStart.Click += (s, e) => StartClicked?.Invoke(s, e);
            _trayMenuStop.Click += (s, e) => StopClicked?.Invoke(s, e);
            _trayMenuExit.Click += (s, e) => ExitClicked?.Invoke(s, e);
        }

        /// <summary>
        /// Updates the enabled state of the Start and Stop menu items.
        /// </summary>
        public void SetMonitoringState(bool startEnabled, bool stopEnabled)
        {
            _trayMenuStart.Enabled = startEnabled;
            _trayMenuStop.Enabled = stopEnabled;
        }

        /// <summary>
        /// Shows a balloon tip from the tray icon.
        /// </summary>
        public void ShowBalloonTip(int timeout, string title, string text, ToolTipIcon icon)
        {
            _notifyIcon.ShowBalloonTip(timeout, title, text, icon);
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
            _trayMenu?.Dispose();
        }
    }
}