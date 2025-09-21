using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class CargoFormUI
    {
        private void OnPinMaterialsCheckBoxChanged(object? sender, EventArgs e)
        {
            if (_controlFactory == null || _materialServiceCache == null) return;

            _controlFactory.MaterialTreeView.Enabled = !_controlFactory.PinMaterialsCheckBox.Checked;
            AppConfiguration.PinMaterialsMode = _controlFactory.PinMaterialsCheckBox.Checked;
            AppConfiguration.Save();

            // Refresh the material list to apply the new filter.
            UpdateMaterialList(_materialServiceCache);
            _overlayService?.UpdateMaterials(_materialServiceCache);
        }

        private void OnMaterialNodeChecked(object? sender, TreeViewEventArgs e)
        {
            // This event can fire when we programmatically clear and repopulate the tree.
            // The e.Node can be null in some edge cases.
            if (e.Node == null || e.Action == TreeViewAction.Unknown) return;

            // We only care about user-initiated checks/unchecks.
            if (e.Action != TreeViewAction.ByMouse && e.Action != TreeViewAction.ByKeyboard) return;

            var pinnedMaterials = new List<string>();
            var treeView = _controlFactory?.MaterialTreeView;
            if (treeView == null) return;

            // Iterate through all nodes to build the complete list of pinned materials.
            foreach (TreeNode categoryNode in treeView.Nodes)
            {
                foreach (TreeNode materialNode in categoryNode.Nodes)
                {
                    if (materialNode.Checked && materialNode.Tag is string materialName)
                    {
                        pinnedMaterials.Add(materialName);
                    }
                }
            }

            // Convert the List<string> to a HashSet<string> to fix the type mismatch error.
            AppConfiguration.PinnedMaterials = new HashSet<string>(pinnedMaterials);

            if (_controlFactory == null) return;
            // If the "Show Pinned" checkbox is checked, we need to refresh the list to reflect the change.
            if (_controlFactory.PinMaterialsCheckBox.Checked && _materialServiceCache != null)
            {
                UpdateMaterialList(_materialServiceCache);
                _overlayService?.UpdateMaterials(_materialServiceCache);
            }
        }
    }
}