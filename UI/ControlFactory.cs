using System;
using System.Drawing;
using System.Windows.Forms;

namespace EliteCargoMonitor.UI
{
    /// <summary>
    /// Creates and manages the UI controls for the main form.
    /// </summary>
    public class ControlFactory : IDisposable
    {
        public ListView ListView { get; }
        public Button StartBtn { get; }
        public Button StopBtn { get; }
        public Button ExitBtn { get; }
        public Button AboutBtn { get; }
        public Button SettingsBtn { get; }
        public Button WatchingLabel { get; }
        public Button CargoHeaderLabel { get; }
        public Button CargoSizeLabel { get; }
        public Button CommanderLabel { get; }
        public Button ShipLabel { get; }
        public Button BalanceLabel { get; }
        public ToolTip ToolTip { get; }

        public ControlFactory(FontManager fontManager)
        {
            // Main ListView to display cargo items
            ListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                Font = fontManager.VerdanaFont,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.None,
                BackColor = SystemColors.Window, // Use standard window background
                GridLines = false // Cleaner look without grid lines
            };

            // Define columns for the ListView
            ListView.Columns.Add("Commodity", 200, HorizontalAlignment.Left);
            ListView.Columns.Add("Count", 80, HorizontalAlignment.Right);
            ListView.Columns.Add("Category", -2, HorizontalAlignment.Left);

            // Create control buttons
            StartBtn = new Button { Text = "Start", Font = fontManager.ConsolasFont, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            StopBtn = new Button { Text = "Stop", Enabled = false, Font = fontManager.ConsolasFont, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            ExitBtn = new Button { Text = "Exit", Font = fontManager.ConsolasFont, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            SettingsBtn = new Button { Text = "Settings", Font = fontManager.ConsolasFont, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            AboutBtn = new Button { Text = "About", Font = fontManager.ConsolasFont, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };

            // Apply a modern, flat style to the buttons to make them "pop"
            var buttonsToStyle = new[] { StartBtn, StopBtn, ExitBtn, SettingsBtn, AboutBtn };
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

            // Create ToolTip and assign to buttons
            ToolTip = new ToolTip();
            ToolTip.SetToolTip(StartBtn, "Start monitoring for cargo changes");
            ToolTip.SetToolTip(StopBtn, "Stop monitoring for cargo changes");
            ToolTip.SetToolTip(ExitBtn, "Exit the application");
            ToolTip.SetToolTip(SettingsBtn, "Configure application settings");
            ToolTip.SetToolTip(AboutBtn, "Show information about the application");

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

            // Create a "label" for the watching animation using a styled, disabled button.
            // Calculate the max width needed for the animation to prevent layout shifts.
            int animationWidth = WatchingAnimationManager.CalculateMaxWidth(fontManager.AnimationFont);

            WatchingLabel = new Button
            {
                Text = "",
                Font = fontManager.AnimationFont,
                AutoSize = false, // Must be false to set a fixed size and prevent resizing
                Width = animationWidth > 0 ? animationWidth : 20, // Set fixed width, with a fallback
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                Enabled = true, // Keep enabled to preserve color
                Cursor = Cursors.Default, // Make it look non-interactive
                FlatAppearance = {
                    BorderSize = 0,
                    MouseDownBackColor = Color.Transparent,
                    MouseOverBackColor = Color.Transparent
                },
                Margin = new Padding(3),
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

            // Configure properties for the flexible layout
            ShipLabel.AutoSize = false;
            ShipLabel.Dock = DockStyle.Fill;

            CommanderLabel.TextAlign = ContentAlignment.MiddleLeft;
            BalanceLabel.TextAlign = ContentAlignment.MiddleRight;
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

        public void Dispose()
        {
            StartBtn.Paint -= Button_Paint;
            StopBtn.Paint -= Button_Paint;
            ExitBtn.Paint -= Button_Paint;
            AboutBtn.Paint -= Button_Paint;
            SettingsBtn.Paint -= Button_Paint;

            ListView.Dispose();
            StartBtn.Dispose();
            StopBtn.Dispose();
            ExitBtn.Dispose();
            AboutBtn.Dispose();
            SettingsBtn.Dispose();
            WatchingLabel.Dispose();
            CargoHeaderLabel.Dispose();
            CargoSizeLabel.Dispose();
            CommanderLabel.Dispose();
            ShipLabel.Dispose();
            BalanceLabel.Dispose();
            ToolTip.Dispose();
        }
    }
}