using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

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
            UpdateGrid(_rawMaterialsGrid, materials, MaterialDataService.GetAllRawMaterials());
        }

        public void UpdateManufacturedMaterials(List<MaterialItem> materials)
        {
            UpdateGrid(_manufacturedMaterialsGrid, materials, MaterialDataService.GetAllManufacturedMaterials());
        }

        public void UpdateEncodedData(List<MaterialItem> materials)
        {
            UpdateGrid(_encodedDataGrid, materials, MaterialDataService.GetAllEncodedMaterials());
        }

        private void UpdateGrid(DataGridView grid, List<MaterialItem> currentMaterials, List<MaterialDefinition> allMaterials)
        {
            grid.Rows.Clear();
            if (allMaterials == null || !allMaterials.Any()) return;

            var currentMaterialsDict = currentMaterials?.ToDictionary(m => m.Name.ToLowerInvariant(), m => m.Count)
                                       ?? new Dictionary<string, int>();

            foreach (var materialDef in allMaterials)
            {
                currentMaterialsDict.TryGetValue(materialDef.Name.ToLowerInvariant(), out int count);
                grid.Rows.Add(MaterialDataService.GetLocalisedName(materialDef.Name), count);
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