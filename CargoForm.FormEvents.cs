using EliteDataRelay.Configuration;
using System;
using System.Windows.Forms;

namespace EliteDataRelay
{
    public partial class CargoForm
    {
        private void CargoForm_Load(object? sender, EventArgs e)
        {
            // Load settings when the form loads
            AppConfiguration.Load();
            RegisterHotkeys();
        }

        private void CargoForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Unregister hotkeys to clean up system resources.
            UnregisterHotkeys();

            // Save settings on exit, unless the user is canceling out of a prompt.
            if (e.CloseReason != CloseReason.None && e.CloseReason != CloseReason.TaskManagerClosing)
            {
                SaveOnExit();
            }
        }

        private void SaveOnExit()
        {
            // Save the window's current state and location before closing.
            AppConfiguration.WindowState = this.WindowState;
            AppConfiguration.WindowLocation = this.Location;

            // Persist all settings to the settings.json file.
            AppConfiguration.Save();
        }
    }
}