using EliteDataRelay.Configuration;
using EliteDataRelay.Services;
using System;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class CargoFormUI
    {
        private void OnShowApplication(object? sender, EventArgs e) => ShowForm();

        private void OnPinMaterialsCheckBoxChanged(object? sender, EventArgs e)
        {
            // If the user changes the pin setting, refresh the list using the cached data.
            if (_materialServiceCache != null)
            {
                UpdateMaterialList(_materialServiceCache);
            }
        }

        private void OnMaterialNodeChecked(object? sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is not string materialName || e.Action == TreeViewAction.Unknown)
            {
                return;
            }

            var pinnedMaterials = AppConfiguration.PinnedMaterials.ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            if (e.Node.Checked)
            {
                pinnedMaterials.Add(materialName);
            }
            else
            {
                pinnedMaterials.Remove(materialName);
            }

            AppConfiguration.PinnedMaterials = pinnedMaterials.ToList();
            AppConfiguration.Save();

            // If we are in "pinned only" view, unchecking an item should make it disappear.
            if (_controlFactory?.PinMaterialsCheckBox.Checked == true && _materialServiceCache != null)
            {
                UpdateMaterialList(_materialServiceCache);
            }
        }
    }
}