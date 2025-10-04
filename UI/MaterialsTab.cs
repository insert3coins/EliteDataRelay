using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Models;

namespace EliteDataRelay.UI
{
    public class MaterialsTab : TabPage
    {
        private readonly DataGridView _rawMaterialsGrid;
        private readonly DataGridView _manufacturedMaterialsGrid;
        private readonly DataGridView _encodedDataGrid;

        public MaterialsTab() : base("Materials")
        {
            var subTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10)
            };

            // Raw Materials
            var rawMaterialsTab = new TabPage("Raw");
            _rawMaterialsGrid = CreateDataGridView();
            rawMaterialsTab.Controls.Add(_rawMaterialsGrid);
            subTabControl.TabPages.Add(rawMaterialsTab);

            // Manufactured Materials
            var manufacturedMaterialsTab = new TabPage("Manufactured");
            _manufacturedMaterialsGrid = CreateDataGridView();
            manufacturedMaterialsTab.Controls.Add(_manufacturedMaterialsGrid);
            subTabControl.TabPages.Add(manufacturedMaterialsTab);

            // Encoded Data
            var encodedDataTab = new TabPage("Encoded");
            _encodedDataGrid = CreateDataGridView();
            encodedDataTab.Controls.Add(_encodedDataGrid);
            subTabControl.TabPages.Add(encodedDataTab);

            this.Controls.Add(subTabControl);

            InitializeColumns();
        }

        public void UpdateRawMaterials(List<MaterialItem> materials)
        {
            UpdateGrid(_rawMaterialsGrid, materials);
        }

        public void UpdateManufacturedMaterials(List<MaterialItem> materials)
        {
            UpdateGrid(_manufacturedMaterialsGrid, materials);
        }

        public void UpdateEncodedData(List<MaterialItem> materials)
        {
            UpdateGrid(_encodedDataGrid, materials);
        }

        private void UpdateGrid(DataGridView grid, List<MaterialItem> materials)
        {
            grid.Rows.Clear();
            if (materials == null || !materials.Any()) return;

            var sortedMaterials = materials.OrderBy(m => m.Localised ?? m.Name);

            foreach (var material in sortedMaterials)
            {
                grid.Rows.Add(material.Localised ?? material.Name, material.Count);
            }
        }

        private void InitializeColumns()
        {
            _rawMaterialsGrid.Columns.Add("Material", "Material");
            _rawMaterialsGrid.Columns.Add("Count", "Count");

            _manufacturedMaterialsGrid.Columns.Add("Material", "Material");
            _manufacturedMaterialsGrid.Columns.Add("Count", "Count");

            _encodedDataGrid.Columns.Add("Data", "Data");
            _encodedDataGrid.Columns.Add("Count", "Count");

            // Adjust column fill weights
            _rawMaterialsGrid.Columns["Material"].FillWeight = 300;
            _rawMaterialsGrid.Columns["Count"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            _manufacturedMaterialsGrid.Columns["Material"].FillWeight = 300;
            _manufacturedMaterialsGrid.Columns["Count"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            _encodedDataGrid.Columns["Data"].FillWeight = 300;
            _encodedDataGrid.Columns["Count"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }

        private DataGridView CreateDataGridView()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9),
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(243, 244, 246),
                    ForeColor = Color.FromArgb(31, 41, 55),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Padding = new Padding(6, 0, 6, 0)
                },
                ColumnHeadersHeight = 40,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(249, 250, 251)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(31, 41, 55),
                    Padding = new Padding(6, 0, 6, 0)
                },
                RowTemplate = new DataGridViewRow
                {
                    Height = 35
                }
            };
        }
    }
}