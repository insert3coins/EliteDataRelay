using EliteDataRelay.Models;
using System.Collections.Generic;
using System.Windows.Forms;

namespace EliteDataRelay.UI

{
    public class MaterialsTab : TabPage
    {
        // These would be your DataGridViews or other controls
        public void UpdateRawMaterials(List<MaterialItem> items) { /* Existing logic */ }
        public void UpdateManufacturedMaterials(List<MaterialItem> items) { /* Existing logic */ }
        public void UpdateEncodedData(List<MaterialItem> items) { /* Existing logic */ }

        public void UpdateAllMaterials(List<MaterialItem> raw, List<MaterialItem> manufactured, List<MaterialItem> encoded)
        {
            // You can add BeginUpdate/EndUpdate calls here if your controls support it
            // e.g., rawMaterialsGrid.BeginUpdate();

            UpdateRawMaterials(raw);
            UpdateManufacturedMaterials(manufactured);
            UpdateEncodedData(encoded);

            // e.g., rawMaterialsGrid.EndUpdate();
        }
    }
}