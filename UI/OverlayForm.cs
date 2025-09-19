using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EliteDataRelay.Models;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm : Form
    {
        public enum OverlayPosition
        {
            Left,
            Right
        }

        // P/Invoke constants for making the form click-through
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private Label _cmdrLabel = null!;
        private Label _shipLabel = null!;
        private Label _balanceLabel = null!;
        private Label _cargoLabel = null!;
        private ListView _cargoListView = null!;

        // Fonts are IDisposable, so we should keep references to them to dispose of them later.
        private readonly Font _labelFont;
        private readonly Font _listFont;
        private readonly OverlayPosition _position;

        public OverlayForm(OverlayPosition position)
        {
            _position = position;

            // Start with zero opacity to prevent flickering/flashing during initialization.
            this.Opacity = 0;

            // Enable double buffering to reduce flicker and rendering artifacts. This is a standard
            // technique to prevent visual glitches like stray lines or bars on transparent forms.
            DoubleBuffered = true;
            // Form properties for a borderless, transparent overlay
            FormBorderStyle = FormBorderStyle.None;
            Text = "Elite Data Relay Overlay";
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.Magenta; // This color will be made transparent
            TransparencyKey = Color.Magenta;
            StartPosition = FormStartPosition.Manual;

            _labelFont = new Font("Verdana", 12, FontStyle.Bold);
            _listFont = new Font("Verdana", 12);
        }

        private Label CreateOverlayLabel(Point location, Font? font = null)
        {
            return new Label
            {
                Location = location,
                AutoSize = true,
                Font = font ?? _labelFont,
                ForeColor = Color.Orange, // High-visibility color for in-game
                BackColor = Color.Transparent,
                Text = "..."
            };
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Make the form click-through
            int extendedStyle = GetWindowLong(Handle, GWL_EXSTYLE);
            SetWindowLong(Handle, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);

            if (_position == OverlayPosition.Left)
            {
                // --- Left-aligned info ---
                this.Size = new Size(300, 110);
                _cmdrLabel = CreateOverlayLabel(new Point(10, 10), _labelFont);
                _shipLabel = CreateOverlayLabel(new Point(10, 40), _labelFont);
                _balanceLabel = CreateOverlayLabel(new Point(10, 70), _labelFont);

                Controls.Add(_cmdrLabel);
                Controls.Add(_shipLabel);
                Controls.Add(_balanceLabel);
            }
            else // Right
            {
                this.Size = new Size(280, 400);

                _cargoLabel = new Label
                {
                    Location = new Point(0, 10),
                    Size = new Size(this.Width, 25),
                    AutoSize = false,
                    Font = _labelFont,
                    ForeColor = Color.Orange,
                    BackColor = Color.Transparent,
                    Text = "...",
                    TextAlign = ContentAlignment.MiddleRight,
                    Padding = new Padding(0, 0, 10, 0) // Padding so text isn't against the very edge
                };

                // Create ListView for cargo items
                _cargoListView = new ListView
                {
                    Location = new Point(0, 40),
                    // The height is set to fill the remaining space to prevent a 1px border artifact.
                    Size = new Size(this.Width, 360),
                    View = View.Details,
                    BackColor = Color.Magenta,
                    ForeColor = Color.Orange,
                    Font = _listFont,
                    BorderStyle = BorderStyle.None,
                    HeaderStyle = ColumnHeaderStyle.None,
                    FullRowSelect = false
                };
                _cargoListView.Columns.Add("", 0, HorizontalAlignment.Left);
                _cargoListView.Columns.Add("Commodity", -2, HorizontalAlignment.Right);
                _cargoListView.Columns.Add("Count", 60, HorizontalAlignment.Right);

                Controls.Add(_cargoLabel);
                Controls.Add(_cargoListView);
            }

            // Now that the form is fully initialized and invisible, set opacity to 1 to show it.
            // This prevents the user from seeing any part of the form's construction.
            this.Opacity = 1;
        }

        public void UpdateCommander(string text) => UpdateLabel(_cmdrLabel, text);
        public void UpdateShip(string text) => UpdateLabel(_shipLabel, text);
        public void UpdateBalance(string text) => UpdateLabel(_balanceLabel, text);
        public void UpdateCargo(string text) => UpdateLabel(_cargoLabel, text);

        public void UpdateCargoList(IEnumerable<CargoItem> inventory)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateCargoList(inventory)));
                return;
            }

            _cargoListView.BeginUpdate();
            _cargoListView.Items.Clear();

            var sortedInventory = inventory.OrderBy(i => !string.IsNullOrEmpty(i.Localised) ? i.Localised : i.Name);
            foreach (var item in sortedInventory)
            {
                string displayName = !string.IsNullOrEmpty(item.Localised) ? item.Localised : item.Name;
                if (!string.IsNullOrEmpty(displayName))
                {
                    displayName = char.ToUpperInvariant(displayName[0]) + displayName.Substring(1);
                }

                var listViewItem = new ListViewItem(new[] { "", displayName, item.Count.ToString() });
                _cargoListView.Items.Add(listViewItem);
            }

            _cargoListView.EndUpdate();
        }

        private void UpdateLabel(Label label, string text)
        {
            if (InvokeRequired) { Invoke(new Action(() => label.Text = text)); }
            else { label.Text = text; }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing) { _labelFont?.Dispose(); _listFont?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}