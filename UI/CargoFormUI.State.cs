using EliteDataRelay.Configuration;
using System.Windows.Forms;

namespace EliteDataRelay.UI

{
    public partial class CargoFormUI
    {
        public void SetButtonStates(bool startEnabled, bool stopEnabled)
        {
            if (_controlFactory == null) return;

            if (_controlFactory.StartBtn != null)
            {
                _controlFactory.StartBtn.Enabled = startEnabled;
                // Use a different background color to indicate this is the primary action.
                _controlFactory.StartBtn.BackColor = startEnabled ? UIConstants.StartButtonActiveColor : UIConstants.DefaultButtonBackColor;
            }

            if (_controlFactory.StopBtn != null)
            {
                _controlFactory.StopBtn.Enabled = stopEnabled;
                // Use a different background color to indicate this is the primary action.
                _controlFactory.StopBtn.BackColor = stopEnabled ? UIConstants.StopButtonActiveColor : UIConstants.DefaultButtonBackColor;
            }

            // The Session button should always be visible, but only enabled when monitoring is
            // active and the feature is enabled in settings.
            if (_controlFactory.SessionBtn != null)
            {
                _controlFactory.SessionBtn.Enabled = stopEnabled && AppConfiguration.EnableSessionTracking;
            }

        }

        public void UpdateMonitoringVisuals(bool isMonitoring)
        {
            _trayIconManager?.SetMonitoringState(startEnabled: !isMonitoring, stopEnabled: isMonitoring);

            if (_watchingAnimationManager == null) return;

            if (isMonitoring)
            {
                _watchingAnimationManager.Start();
                // Only start the overlay service if at least one of the overlays is enabled.
                if (AppConfiguration.EnableInfoOverlay || AppConfiguration.EnableCargoOverlay || AppConfiguration.EnableMaterialsOverlay)
                {
                    _overlayService?.Start();
                }
            }
            else // Monitoring is stopped
            {
                _watchingAnimationManager.Stop();
                // When monitoring stops, just hide the overlay. It will be destroyed on exit.
                _overlayService?.Hide();
            }
        }
    }
}