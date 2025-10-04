namespace EliteDataRelay.UI
{
    public partial class CargoFormUI
    {
        /// <summary>
        /// Sets the enabled state of the main start/stop buttons and the tray icon menu items.
        /// </summary>
        /// <param name="startEnabled">Whether the start button should be enabled.</param>
        /// <param name="stopEnabled">Whether the stop button should be enabled.</param>
        public void SetButtonStates(bool startEnabled, bool stopEnabled)
        {
            if (_controlFactory == null) return;

            _controlFactory.StartBtn.Enabled = startEnabled;
            _controlFactory.StopBtn.Enabled = stopEnabled;

            _trayIconManager?.SetMonitoringState(startEnabled, stopEnabled);
        }
    }
}