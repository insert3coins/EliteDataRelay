using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI

{
    public partial class ControlFactory
    {
        private Font _toolTipFont = null!;
        private CustomToolTipDrawer _toolTipDrawer = null!;

        private void CreateActionButtons(FontManager fontManager)
        {
            // Create control buttons
            StartBtn = new Button { Text = "Start", Font = fontManager.ConsolasFont, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            StopBtn = new Button { Text = "Stop", Enabled = false, Font = fontManager.ConsolasFont, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            ExitBtn = new Button { Text = "Exit", Font = fontManager.ConsolasFont, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            SettingsBtn = new Button { Text = "Settings", Font = fontManager.ConsolasFont, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };            
            SessionBtn = new Button { Text = "Session", Font = fontManager.ConsolasFont, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            AboutBtn = new Button { Text = "About", Font = fontManager.ConsolasFont, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };

            // Apply a modern, flat style to the buttons to make them "pop"
            var buttonsToStyle = new[] { StartBtn, StopBtn, ExitBtn, SettingsBtn, SessionBtn, AboutBtn };
            foreach (var btn in buttonsToStyle)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0; // We'll draw our own border to keep it consistent
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(229, 241, 251); // Light blue hover
                btn.BackColor = UIConstants.DefaultButtonBackColor;
                btn.Paint += Button_Paint;
            }

            // Set the initial "active" color for the Start button to guide the user.
            StartBtn.BackColor = UIConstants.StartButtonActiveColor;
        }

        private void CreateToolTips()
        {
            _toolTipFont = new Font(SystemFonts.DefaultFont.FontFamily, 10f);
            // Create ToolTip and assign to buttons
            ToolTip = new ToolTip();

            // The ShipModulesTreeView is created in CreateTabControls. We assume that method
            // has been called before this one.
            _toolTipDrawer = new CustomToolTipDrawer(_toolTipFont, ShipModulesTreeView);

            ToolTip.OwnerDraw = true;
            ToolTip.Popup += _toolTipDrawer.ToolTip_Popup;
            ToolTip.Draw += _toolTipDrawer.ToolTip_Draw;
            ToolTip.SetToolTip(StartBtn, "Start monitoring for cargo changes");
            ToolTip.SetToolTip(StopBtn, "Stop monitoring for cargo changes");
            ToolTip.SetToolTip(ExitBtn, "Exit the application");
            ToolTip.SetToolTip(SettingsBtn, "Configure application settings");
            ToolTip.SetToolTip(SessionBtn, "Show session summary statistics");            
            ToolTip.SetToolTip(AboutBtn, "Show information about the application");
        }

        private void Button_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn) return;

            Color borderColor = Color.FromArgb(0, 120, 215);
            ControlPaint.DrawBorder(e.Graphics, btn.ClientRectangle,
                                    borderColor, 1, ButtonBorderStyle.Solid,
                                    borderColor, 1, ButtonBorderStyle.Solid,
                                    borderColor, 1, ButtonBorderStyle.Solid,
                                    borderColor, 1, ButtonBorderStyle.Solid);
        }

        private void DisposeButtons()
        {
            var buttonsToUnsubscribe = new[] { StartBtn, StopBtn, ExitBtn, SettingsBtn, SessionBtn, AboutBtn};
            foreach (var btn in buttonsToUnsubscribe)
            {
                ToolTip.SetToolTip(btn, null);
                btn.Paint -= Button_Paint;
                btn.Dispose();
            }
            ToolTip.Popup -= _toolTipDrawer.ToolTip_Popup;
            ToolTip.Draw -= _toolTipDrawer.ToolTip_Draw;
            ToolTip.Dispose();
            _toolTipFont.Dispose();
        }
    }
}