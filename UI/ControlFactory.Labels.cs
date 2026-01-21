using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
        private void CreateInfoLabels(FontManager fontManager)
        {
            // Create a "label" for the watching animation.
            // Calculate the max width needed for the animation to prevent layout shifts.
            int animationWidth = WatchingAnimationManager.CalculateMaxWidth(fontManager.AnimationFont);

            WatchingLabel = new Label
            {
                Text = "",
                Font = fontManager.AnimationFont,
                AutoSize = false, // Must be false to set a fixed size and prevent resizing
                Width = animationWidth > 0 ? animationWidth : 20, // Set fixed width, with a fallback
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray,
                Margin = new Padding(3),
            };

            // Create a "label" for the cargo meter using a styled, disabled button for alignment.
            CargoSizeLabel = new Button
            {
                Text = UIConstants.CargoSize[0],
                Font = fontManager.ConsolasFont,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlatStyle = FlatStyle.Flat,
                Enabled = true, // Keep enabled to preserve color
                Cursor = Cursors.Default, // Make it look non-interactive
                FlatAppearance = {
                    BorderSize = 0,
                    MouseDownBackColor = Color.Transparent,
                    MouseOverBackColor = Color.Transparent
                },
                Margin = new Padding(5, 3, 3, 3),
            };

            // Create a "label" for the cargo count header using a styled, disabled button.
            CargoHeaderLabel = new Button
            {
                Text = "Cargo: 0",
                Font = fontManager.VerdanaFont, // Use more readable font
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlatStyle = FlatStyle.Flat,
                Enabled = true, // Keep enabled to preserve color
                Cursor = Cursors.Default, // Make it look non-interactive
                FlatAppearance = {
                    BorderSize = 0,
                    MouseDownBackColor = Color.Transparent,
                    MouseOverBackColor = Color.Transparent
                },
                Margin = new Padding(0, 3, 0, 3),
            };

            // Create info labels for commander, ship, and balance
            CommanderLabel = CreateInfoLabel("CMDR: Unknown", fontManager.VerdanaFont);
            ShipLabel = CreateInfoLabel("Ship: Unknown", fontManager.VerdanaFont);
            BalanceLabel = CreateInfoLabel("Balance: Unknown", fontManager.VerdanaFont);
            EdsmStatusLabel = CreateStatusLabel("EDSM: Idle", fontManager.VerdanaFont);

            // Configure properties for the flexible layout
            ShipLabel.AutoSize = false;
            ShipLabel.Dock = DockStyle.Fill;
        }

        private void DisposeLabels()
        {
            // Break font references before disposing controls to avoid GDI+ issues
            try { WatchingLabel.Font = SystemFonts.DefaultFont; } catch { }
            try { CargoHeaderLabel.Font = SystemFonts.DefaultFont; } catch { }
            try { CargoSizeLabel.Font = SystemFonts.DefaultFont; } catch { }
            try { CommanderLabel.Font = SystemFonts.DefaultFont; } catch { }
            try { ShipLabel.Font = SystemFonts.DefaultFont; } catch { }
            try { BalanceLabel.Font = SystemFonts.DefaultFont; } catch { }
            try { EdsmStatusLabel.Font = SystemFonts.DefaultFont; } catch { }

            WatchingLabel.Dispose();
            CargoHeaderLabel.Dispose();
            CargoSizeLabel.Dispose();
            CommanderLabel.Dispose();
            ShipLabel.Dispose();
            BalanceLabel.Dispose();
            EdsmStatusLabel.Dispose();
        }

        /// <summary>
        /// Creates a styled, non-interactive button to be used as a label.
        /// </summary>
        private static Button CreateInfoLabel(string text, Font font)
        {
            return new Button
            {
                Text = text,
                Font = font,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                Enabled = true, // Keep enabled to preserve color
                Cursor = Cursors.Default, // Make it look non-interactive
                FlatAppearance = {
                    BorderSize = 0,
                    MouseDownBackColor = Color.Transparent,
                    MouseOverBackColor = Color.Transparent
                },
                Margin = new Padding(5, 3, 5, 3),
            };
        }

        private static Label CreateStatusLabel(string text, Font font)
        {
            return new Label
            {
                Text = text,
                Font = font,
                AutoSize = true,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(5, 3, 5, 3),
                BackColor = UIConstants.DefaultButtonBackColor,
                Padding = new Padding(4, 4, 4, 4),
                TabStop = false
            };
        }

        private void CreateToolTips(FontManager fontManager)
        {
            ToolTip = new ToolTip 
            { 
                OwnerDraw = true, 
                AutoPopDelay = 30000,
                // Set the colors for our custom-drawn tooltips
                BackColor = Color.FromArgb(45, 45, 50),
                ForeColor = Color.FromArgb(220, 220, 230)
            };
            var customDrawer = new CustomToolTipDrawer(fontManager.ConsolasFont);
            ToolTip.Popup += customDrawer.ToolTip_Popup;
            ToolTip.Draw += customDrawer.ToolTip_Draw;

            // Assign tooltips to the main action buttons
            ToolTip.SetToolTip(StartBtn, "Start monitoring for cargo changes");
            ToolTip.SetToolTip(StopBtn, "Stop monitoring for cargo changes");
            ToolTip.SetToolTip(ExitBtn, "Exit the application");
            ToolTip.SetToolTip(SettingsBtn, "Configure application settings");
            ToolTip.SetToolTip(AboutBtn, "Show information about the application");
        }
    }
}
