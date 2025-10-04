using EliteDataRelay.Configuration;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI

{
    public partial class SettingsForm : Form
    {
        // The event, constructor, InitializeComponent, LoadSettings, and SaveSettings
        // methods are already defined in other partial class files (e.g., SettingsForm.Designer.cs).
        //
        // To add the Inara API Key setting:
        // 1. Add a TextBox named 'inaraApiKeyTextBox' to the form in the designer.
        // 2. In the existing LoadSettings() method, add:
        //    inaraApiKeyTextBox.Text = AppConfiguration.InaraApiKey;
        //
        // 3. In the existing SaveSettings() method, add:
        //    AppConfiguration.InaraApiKey = inaraApiKeyTextBox.Text.Trim();
        //
        // The AppConfiguration.Save() call will handle persisting the new setting.
    }
}