using EliteDataRelay.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public class MaterialsTab : TabPage
    {
        private readonly DataGridView _rawMaterialsGrid;
        private readonly DataGridView _manufacturedMaterialsGrid;
        private readonly DataGridView _encodedDataGrid;

        public MaterialsTab()
        {
            // Use the light theme background from the sample
            this.BackColor = Color.FromArgb(249, 250, 251);
            this.Padding = new Padding(20);

            // Main TabControl for material categories
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
            };

            // Raw Materials Tab
            var rawMaterialsTab = new TabPage("Raw Materials");
            _rawMaterialsGrid = CreateDataGridView();
            rawMaterialsTab.Controls.Add(_rawMaterialsGrid);
            tabControl.TabPages.Add(rawMaterialsTab);

            // Manufactured Materials Tab
            var manufacturedMaterialsTab = new TabPage("Manufactured Materials");
            _manufacturedMaterialsGrid = CreateDataGridView();
            manufacturedMaterialsTab.Controls.Add(_manufacturedMaterialsGrid);
            tabControl.TabPages.Add(manufacturedMaterialsTab);

            // Encoded Data Tab
            var encodedDataTab = new TabPage("Encoded Data");
            _encodedDataGrid = CreateDataGridView();
            encodedDataTab.Controls.Add(_encodedDataGrid);
            tabControl.TabPages.Add(encodedDataTab);

            this.Controls.Add(tabControl);
        }

        private DataGridView CreateDataGridView()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.None,
                ColumnHeadersVisible = true,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 9),
                RowTemplate = { Height = 35 },
            };

            // Column Header Styling from sample
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(243, 244, 246);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(31, 41, 55);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = grid.ColumnHeadersDefaultCellStyle.BackColor;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 5, 10, 5);
            grid.ColumnHeadersHeight = 40;

            // Cell Styling from sample
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(31, 41, 55);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(31, 41, 55);
            grid.DefaultCellStyle.Padding = new Padding(10, 5, 10, 5);

            // Alternating Row Styling from sample
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
            grid.AlternatingRowsDefaultCellStyle.ForeColor = grid.DefaultCellStyle.ForeColor;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = grid.DefaultCellStyle.SelectionBackColor;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = grid.DefaultCellStyle.SelectionForeColor;

            // Add columns
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Material", HeaderText = "MATERIAL", FillWeight = 75 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "COUNT", FillWeight = 25 });
            grid.Columns["Count"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            return grid;
        }

        public void UpdateAllMaterials(List<MaterialItem> raw, List<MaterialItem> manufactured, List<MaterialItem> encoded)
        {
            // Using BeginInvoke to ensure UI updates happen on the UI thread.
            this.BeginInvoke(new MethodInvoker(() =>
            {
                _rawMaterialsGrid.SuspendLayout();
                UpdateGrid(_rawMaterialsGrid, raw);
                _rawMaterialsGrid.ResumeLayout();

                _manufacturedMaterialsGrid.SuspendLayout();
                UpdateGrid(_manufacturedMaterialsGrid, manufactured);
                _manufacturedMaterialsGrid.ResumeLayout();

                _encodedDataGrid.SuspendLayout();
                UpdateGrid(_encodedDataGrid, encoded);
                _encodedDataGrid.ResumeLayout();
            }));
        }

        private void UpdateGrid(DataGridView grid, List<MaterialItem> items)
        {
            grid.Rows.Clear();
            if (items == null || items.Count == 0)
            {
                return;
            }

            // Sort by the localized display name to ensure true alphabetical order in the UI.
            items.Sort((a, b) => 
                Services.MaterialDataService.GetLocalisedName(a.Name)
                .CompareTo(Services.MaterialDataService.GetLocalisedName(b.Name)));

            var rows = new List<DataGridViewRow>();
            foreach (var item in items)
            {
                var row = new DataGridViewRow();
                string displayName = Services.MaterialDataService.GetLocalisedName(item.Name);
                row.CreateCells(grid, displayName, item.Count);
                rows.Add(row);
            }
            grid.Rows.AddRange(rows.ToArray());
            grid.ClearSelection();
        }
    }
}