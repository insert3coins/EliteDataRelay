using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
        private CustomScrollBar? _cargoScrollBar;
        public DataGridView? CargoGridView { get; private set; }
        public Panel? CargoWelcomePanel { get; private set; }

        // Light theme color scheme to match Materials tab
        private readonly Color _cargoBgPrimary = Color.FromArgb(249, 250, 251); // Light gray page background
        private readonly Color _cargoBgSecondary = Color.White; // White content background
        private readonly Color _cargoAccentColor = Color.FromArgb(0, 120, 215); // Standard blue accent
        private readonly Color _cargoTextPrimary = Color.FromArgb(31, 41, 55); // Dark gray text
        private readonly Color _cargoTextSecondary = Color.FromArgb(107, 114, 128); // Medium gray text
        private readonly Color _cargoBorderColor = Color.FromArgb(229, 231, 235); // Light gray border

        private TabPage CreateCargoTabPage(FontManager fontManager)
        {
            var cargoPage = new TabPage("Cargo")
            {
                BackColor = _cargoBgPrimary,
                Padding = new Padding(20) // Uniform padding
            };

            // Content Panel with border effect
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _cargoBgSecondary,
                Padding = new Padding(1)
            };
            contentPanel.Paint += (s, e) =>
            {
                if (s is Panel panel)
                {
                    using Pen pen = new Pen(_cargoBorderColor, 1); // Use a subtle border
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            };

            // DataGridView Setup
            CargoGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = _cargoBgSecondary,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,                
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                GridColor = _cargoBorderColor,
                Font = new Font("Segoe UI", 10),
                RowTemplate = { Height = 40 },
                ScrollBars = ScrollBars.None, // We will use a custom scrollbar
                Visible = false // Initially hidden
            };

            // Column Header Styling
            CargoGridView.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = _cargoBgSecondary,
                ForeColor = _cargoTextSecondary,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                SelectionBackColor = _cargoBgSecondary,
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(15, 10, 15, 10)
            };
            CargoGridView.ColumnHeadersHeight = 40;

            // Cell Styling
            CargoGridView.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = _cargoBgSecondary,
                ForeColor = _cargoTextPrimary,
                SelectionBackColor = Color.FromArgb(219, 234, 254), // Light blue selection
                SelectionForeColor = _cargoTextPrimary,
                Padding = new Padding(15, 5, 15, 5),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            CargoGridView.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = _cargoBgPrimary, // Use the slightly off-white for alternating rows
                ForeColor = _cargoTextPrimary,
                SelectionBackColor = Color.FromArgb(219, 234, 254),
                SelectionForeColor = _cargoTextPrimary,
                Padding = new Padding(15, 5, 15, 5)
            };

            // Add columns
            var colCommodity = new DataGridViewTextBoxColumn { Name = "Commodity", HeaderText = "COMMODITY", FillWeight = 60 };
            var colQuantity = new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "QUANTITY", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells };
            var colCategory = new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "CATEGORY", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
            CargoGridView.Columns.AddRange(colCommodity, colQuantity, colCategory);

            // Events
            CargoGridView!.CellPainting += CargoGrid_CellPainting;

            // Custom ScrollBar Setup
            _cargoScrollBar = new CustomScrollBar
            {
                Dock = DockStyle.Right,
                Visible = false
            };
            _cargoScrollBar.SetTheme(_cargoAccentColor, _cargoBgPrimary);
            _cargoScrollBar.Scroll += (s, e) =>
            {
                if (CargoGridView.Rows.Count > 0)
                {
                    CargoGridView.FirstDisplayedScrollingRowIndex = e.NewValue;
                }
            };

            // Welcome Panel Setup
            CargoWelcomePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _cargoBgSecondary,
                Visible = true // Initially visible
            };

            var welcomeLabel = new Label
            {
                Text = "Ready to monitor your cargo hold.\n\nPress 'Start' to begin.",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = _cargoTextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(20)
            };

            CargoWelcomePanel.Controls.Add(welcomeLabel);

            // Container for Grid and Scrollbar
            var gridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _cargoBgSecondary
            };
            gridContainer.Controls.Add(_cargoScrollBar);
            gridContainer.Controls.Add(CargoGridView);

            // Add controls to the content panel
            // The welcome panel will be on top of the grid view initially.
            contentPanel.Controls.Add(CargoWelcomePanel);
            contentPanel.Controls.Add(gridContainer);
            cargoPage.Controls.Add(contentPanel);

            return cargoPage;
        }

        private void CargoGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics is null) return;

            // Draw selection indicator on the left
            if (e.State.HasFlag(DataGridViewElementStates.Selected))
            {
                using var accentBrush = new SolidBrush(_cargoAccentColor);
                e.Graphics.FillRectangle(accentBrush, e.CellBounds.Left, e.CellBounds.Top, 4, e.CellBounds.Height);
            }
        }

        public void UpdateCargoScrollBar()
        {
            if (CargoGridView == null || _cargoScrollBar == null) return;

            var dgv = CargoGridView;
            var scroll = _cargoScrollBar;

            if (dgv.RowCount == 0)
            {
                scroll.Visible = false;
                return;
            }

            int displayedRows = dgv.DisplayedRowCount(false);
            bool needsScroll = dgv.RowCount > displayedRows;

            scroll.Visible = needsScroll;

            if (needsScroll)
            {
                scroll.Minimum = 0;
                scroll.Maximum = dgv.RowCount - 1;
                scroll.LargeChange = displayedRows;
                scroll.Value = dgv.FirstDisplayedScrollingRowIndex;
            }
        }

        private void DisposeCargoTabControls()
        {
            CargoGridView?.Dispose();
            _cargoScrollBar?.Dispose();
        }
    }
}
