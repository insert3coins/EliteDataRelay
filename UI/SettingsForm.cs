using EliteDataRelay.Configuration;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class SettingsForm : Form
    {
        // The constructor is defined in the other partial class file (SettingsForm.Designer.cs).
        // We just need to add our new initialization logic to it.
        // In the existing constructor:
        //   InitializeComponent();
        //   InitializeMiningSettings(); // Add this line
        //   LoadSettings();
        private void InitializeMiningSettings()
        {
            // Assuming there's a TabControl named 'tabControl' on the form.
            // Find the main TabControl to add our new settings group.
            var mainTabControl = this.Controls.OfType<TabControl>().FirstOrDefault();
            if (mainTabControl == null) return; // Or handle error appropriately

            // Find a suitable TabPage, e.g., the first one.
            var generalTabPage = mainTabControl.TabPages.Count > 0 ? mainTabControl.TabPages[0] : null;
            if (generalTabPage == null) return;

            // Create a GroupBox for mining settings
            var miningGroupBox = new GroupBox
            {
                Text = "Mining Settings",
                Location = new Point(12, 377), // Positioned below the Session Tracking group
                Size = new Size(440, 85),
                Padding = new Padding(10),
            };

            var chkEnableMiningAnnouncements = new CheckBox
            {
                Text = "Enable mining session announcements in the UI",
                Checked = AppConfiguration.EnableMiningAnnouncements,
                AutoSize = true,
                Location = new Point(15, 25)
            };
            chkEnableMiningAnnouncements.CheckedChanged += (s, e) =>
            {
                AppConfiguration.EnableMiningAnnouncements = chkEnableMiningAnnouncements.Checked;
                LiveSettingsChanged?.Invoke(this, EventArgs.Empty);
            };

            var chkNotifyOnCargoFull = new CheckBox
            {
                Text = "Show tray notification when cargo is full",
                Checked = AppConfiguration.NotifyOnCargoFull,
                AutoSize = true,
                Location = new Point(15, 50)
            };
            chkNotifyOnCargoFull.CheckedChanged += (s, e) =>
            {
                AppConfiguration.NotifyOnCargoFull = chkNotifyOnCargoFull.Checked;
                LiveSettingsChanged?.Invoke(this, EventArgs.Empty);
            };

            miningGroupBox.Controls.Add(chkEnableMiningAnnouncements);
            miningGroupBox.Controls.Add(chkNotifyOnCargoFull);

            generalTabPage.Controls.Add(miningGroupBox);
        }
    }
}