using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.UI
    {
    public partial class SettingsForm
    {
        private Button _btnNavGeneral = null!;
        private Button _btnNavOverlay = null!;
        private Button _btnNavAdvanced = null!;
        private Panel _leftNavPanel = null!;
        private Panel _contentHost = null!;
        private Panel _panelGeneral = null!;
        private Panel _panelOverlay = null!;
        
        private Panel _panelAdvanced = null!;

        private void InitializeComponent()
        {
            // Form Properties
            Text = "Settings";
            ClientSize = new Size(800, 600);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;
            ForeColor = Color.FromArgb(17, 24, 39);

            var accent = Color.FromArgb(52, 152, 219); // #3498db

            // Header panel
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.White
            };
            var headerTitle = new Label
            {
                Text = "Settings",
                AutoSize = true,
                Font = new Font(Font.FontFamily, 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                Location = new Point(16, 18)
            };
            var headerSubtitle = new Label
            {
                Text = "Customize your experience",
                AutoSize = true,
                Font = new Font(Font.FontFamily, 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(107, 114, 128),
                Location = new Point(18, 38)
            };
            var headerUnderline = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(229, 231, 235)
            };
            headerPanel.Controls.Add(headerTitle);
            headerPanel.Controls.Add(headerSubtitle);
            headerPanel.Controls.Add(headerUnderline);

            // Footer panel with action buttons
            var footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White
            };

            // OK Button
            _btnOk = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK,
                Size = new Size(100, 32),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(251, 146, 60),
                ForeColor = Color.White
            };
            _btnOk.FlatAppearance.BorderColor = Color.FromArgb(251, 146, 60);
            _btnOk.FlatAppearance.BorderSize = 1;
            _btnOk.Click += (sender, e) => SaveSettings();

            // Cancel Button
            _btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Size = new Size(100, 32),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(17, 24, 39)
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            _btnCancel.FlatAppearance.BorderSize = 1;

            footerPanel.Controls.Add(_btnOk);
            footerPanel.Controls.Add(_btnCancel);

            // Left navigation panel
            _leftNavPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                BackColor = Color.FromArgb(248, 249, 250)
            };
            var leftDivider = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Color.FromArgb(229, 231, 235) };
            _leftNavPanel.Controls.Add(leftDivider);

            _btnNavGeneral = CreateNavButton("📊  General");
            _btnNavOverlay = CreateNavButton("🖥️  Overlay");
            _btnNavAdvanced = CreateNavButton("🔧  Advanced");

            // position nav buttons
            _btnNavGeneral.Location = new Point(0, 12);
            _btnNavOverlay.Location = new Point(0, 12 + 44);
            _btnNavAdvanced.Location = new Point(0, 12 + 88);

            _btnNavGeneral.Click += (s, e) => ActivateTab("general");
            _btnNavOverlay.Click += (s, e) => ActivateTab("overlay");
            _btnNavAdvanced.Click += (s, e) => ActivateTab("advanced");

            // Hotkeys nav removed; Advanced sits under Overlay

            _leftNavPanel.Controls.Add(_btnNavAdvanced);
            _leftNavPanel.Controls.Add(_btnNavOverlay);
            _leftNavPanel.Controls.Add(_btnNavGeneral);

            // Content host
            _contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            // Create panels for each section
            _panelGeneral = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoScroll = true };
            _panelOverlay = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoScroll = true };
            
            _panelAdvanced = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoScroll = true };

            // Build temporary TabPages using existing initializers, then move their controls
            var tmpGeneral = new TabPage("General");
            var tmpOverlay = new TabPage("Overlay");
            var tmpAdvanced = new TabPage("Advanced + Web");

            foreach (var page in new[] { tmpGeneral, tmpOverlay, tmpAdvanced })
            {
                page.BackColor = Color.White;
                page.ForeColor = Color.FromArgb(17, 24, 39);
            }

            // Initialize each section's content
            InitializeGeneralTab(tmpGeneral);
            InitializeOverlayTab(tmpOverlay);
            InitializeAdvancedWebTab(tmpAdvanced);

            // Move controls from temp pages into our panels
            MoveChildren(tmpGeneral, _panelGeneral);
            MoveChildren(tmpOverlay, _panelOverlay);
            MoveChildren(tmpAdvanced, _panelAdvanced);

            // Add content panels (bring active to front later)
            _contentHost.Controls.Add(_panelGeneral);
            _contentHost.Controls.Add(_panelOverlay);
            _contentHost.Controls.Add(_panelAdvanced);

            // Add Controls
            Controls.Add(_contentHost);
            Controls.Add(_leftNavPanel);
            Controls.Add(footerPanel);
            Controls.Add(headerPanel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            // Activate default tab
            ActivateTab("general");

            // Position footer buttons relative to current size
            _btnCancel.Location = new Point(ClientSize.Width - 16 - _btnCancel.Width, 14);
            _btnOk.Location = new Point(_btnCancel.Left - 10 - _btnOk.Width, 14);

            // Local helpers
            Button CreateNavButton(string text)
            {
                var btn = new Button
                {
                    Text = text,
                    Width = 200,
                    Height = 40,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(255, 255, 255),
                    ForeColor = Color.FromArgb(90, 108, 125),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(20, 0, 0, 0),
                    TabStop = false
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 247, 250);
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 240, 248);
                return btn;
            }

            void MoveChildren(Control from, Control to)
            {
                // Preserve relative layout by reparenting controls
                var list = new Control[from.Controls.Count];
                from.Controls.CopyTo(list, 0);
                foreach (var c in list)
                {
                    to.Controls.Add(c);
                }
            }

            // Stacked move helper removed (no longer used)

            void SetActiveNav(Button active)
            {
                foreach (var b in new[] { _btnNavGeneral, _btnNavOverlay, _btnNavAdvanced })
                {
                    if (b == active)
                    {
                        b.BackColor = Color.FromArgb(229, 246, 255);
                        b.ForeColor = accent;
                    }
                    else
                    {
                        b.BackColor = Color.White;
                        b.ForeColor = Color.FromArgb(90, 108, 125);
                    }
                }
            }

            void ActivateTab(string key)
            {
                // Switch visible panel
                _panelGeneral.Visible = key == "general";
                _panelOverlay.Visible = key == "overlay";
                _panelAdvanced.Visible = key == "advanced";

                if (_panelGeneral.Visible) _panelGeneral.BringToFront();
                if (_panelOverlay.Visible) _panelOverlay.BringToFront();
                if (_panelAdvanced.Visible) _panelAdvanced.BringToFront();

                // Nav button styles
                SetActiveNav(key switch
                {
                    "general" => _btnNavGeneral,
                    "overlay" => _btnNavOverlay,
                    _ => _btnNavAdvanced
                });
            }
        }

        // Hotkey input helper removed
    }
}
