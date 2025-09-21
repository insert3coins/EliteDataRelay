using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Models;

namespace EliteDataRelay.UI
{
    public partial class CargoFormUI
    {
        public void UpdateShipLoadout(ShipLoadout loadout)
        {
            if (_controlFactory == null) return;

            // Update ship icon and basic info
            _controlFactory.ShipIconPictureBox.Image = ShipIconService.GetShipIcon(loadout.Ship);
            _controlFactory.ShipNameLabel.Text = loadout.ShipName;
            _controlFactory.ShipIdentLabel.Text = loadout.ShipIdent;

            // Update modules tree
            var treeView = _controlFactory.ShipModulesTreeView;
            treeView.BeginUpdate();
            treeView.Nodes.Clear();

            var hardpointNode = treeView.Nodes.Add("Hardpoints");
            var utilityNode = treeView.Nodes.Add("Utility Mounts");
            var coreNode = treeView.Nodes.Add("Core Internals");
            var optionalNode = treeView.Nodes.Add("Optional Internals");
            var otherNode = treeView.Nodes.Add("Other");

            var modulesBySlot = loadout.Modules.GroupBy(m => GetSlotCategory(m.Slot)).ToDictionary(g => g.Key, g => g.ToList());

            PopulateModuleCategory(hardpointNode, modulesBySlot.GetValueOrDefault("Hardpoint"));
            PopulateModuleCategory(utilityNode, modulesBySlot.GetValueOrDefault("Utility"));
            PopulateModuleCategory(coreNode, modulesBySlot.GetValueOrDefault("Core"));
            PopulateModuleCategory(optionalNode, modulesBySlot.GetValueOrDefault("Optional"));
            PopulateModuleCategory(otherNode, modulesBySlot.GetValueOrDefault("Other"));

            hardpointNode.Expand();
            utilityNode.Expand();
            coreNode.Expand();
            optionalNode.Expand();
            otherNode.Expand();

            treeView.EndUpdate();
        }

        private string GetSlotCategory(string slotName)
        {
            if (slotName.Contains("Hardpoint")) return "Hardpoint";
            if (slotName.Contains("Utility")) return "Utility";
            if (IsCoreSlot(slotName)) return "Core";
            if (slotName.StartsWith("Slot")) return "Optional";
            return "Other";
        }

        private bool IsCoreSlot(string slotName)
        {
            var coreSlots = new[] { "PowerPlant", "MainEngines", "FrameShiftDrive", "LifeSupport", "PowerDistributor", "Radar", "FuelTank" };
            return coreSlots.Any(s => slotName.Contains(s));
        }

        private void PopulateModuleCategory(TreeNode categoryNode, System.Collections.Generic.List<ShipModule>? modules)
        {
            if (modules == null || !modules.Any())
            {
                categoryNode.Nodes.Add("Empty").ForeColor = SystemColors.GrayText;
                return;
            }

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

            foreach (var module in modules.OrderBy(m => m.Slot))
            {
                var moduleNode = categoryNode.Nodes.Add($"{module.Slot}: {textInfo.ToTitleCase(module.Item.Replace("_", " ").ToLowerInvariant())}");
                if (!module.IsOn)
                {
                    moduleNode.ForeColor = Color.Gray;
                }

                if (module.Engineering != null)
                {
                    moduleNode.ForeColor = Color.DodgerBlue;
                    var engineeringNode = moduleNode.Nodes.Add($"Engineering: {module.Engineering.BlueprintName} (G{module.Engineering.Level})");
                    engineeringNode.ForeColor = Color.DodgerBlue;

                    foreach (var modifier in module.Engineering.Modifiers)
                    {
                        string change = modifier.Value > modifier.OriginalValue ? "▲" : "▼";
                        Color changeColor = (modifier.Value > modifier.OriginalValue) ^ (modifier.LessIsGood == 1) ? Color.Green : Color.Red;

                        var modNode = engineeringNode.Nodes.Add($"{modifier.Label}: {modifier.Value:F2} ({change})");
                        modNode.ForeColor = changeColor;
                    }
                }
            }
        }
    }
}