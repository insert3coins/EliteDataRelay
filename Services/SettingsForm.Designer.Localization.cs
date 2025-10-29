using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class SettingsForm
    {
        private ComboBox _cmbLanguage = null!;

        private void InitializeLocalizationTab(TabPage tab)
        {
            tab.BackColor = Color.White;

            var grp = new GroupBox
            {
                Text = Properties.Strings.Settings_Localization_GroupTitle,
                Location = new Point(12, 12),
                Size = new Size(520, 120),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(31, 41, 55)
            };

            var lbl = new Label
            {
                Text = Properties.Strings.Settings_Localization_LanguageLabel,
                Location = new Point(15, 30),
                AutoSize = true
            };
            _cmbLanguage = new ComboBox
            {
                Location = new Point(15, 55),
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Languages: system default + main ones
            (string code, string name)[] items = new[]
            {
                ("", "System Default"),
                ("en", "English"),
                ("fr", "Français"),
                ("de", "Deutsch"),
                ("es", "Español"),
                ("it", "Italiano"),
                ("pt-BR", "Português (Brasil)"),
                ("ru", "Русский"),
                ("zh-Hans", "中文(简体)"),
                ("ja", "日本語")
            };
            _cmbLanguage.Items.AddRange(items.Select(i => (object)i.name).ToArray());
            int idx = System.Array.FindIndex(items, i => i.code.Equals(AppConfiguration.UICulture ?? string.Empty, System.StringComparison.OrdinalIgnoreCase));
            _cmbLanguage.SelectedIndex = idx >= 0 ? idx : 0;

            _cmbLanguage.SelectedIndexChanged += (s, e) =>
            {
                var sel = _cmbLanguage.SelectedIndex;
                var code = sel >= 0 && sel < items.Length ? items[sel].code : string.Empty;
                AppConfiguration.UICulture = code;
                AppConfiguration.Save();

                try
                {
                    if (string.IsNullOrWhiteSpace(code))
                    {
                        Properties.Strings.Culture = null;
                    }
                    else
                    {
                        var culture = new CultureInfo(code);
                        System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                        System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                        Properties.Strings.Culture = culture;
                    }
                }
                catch { }

                // Notify that settings have changed; some text updates may require reopening windows.
                LiveSettingsChanged?.Invoke(this, System.EventArgs.Empty);

                MessageBox.Show(this,
                    Properties.Strings.Settings_Localization_RestartNote,
                    Properties.Strings.Diagnostics_Title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };

            grp.Controls.Add(lbl);
            grp.Controls.Add(_cmbLanguage);
            tab.Controls.Add(grp);
        }
    }
}

