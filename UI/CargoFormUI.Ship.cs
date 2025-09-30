using EliteDataRelay.Models;
using EliteDataRelay.Services;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.UI

{
    public partial class CargoFormUI
    {
        public void UpdateShipStatus(StatusFile status)
        {
            if (_controlFactory == null) return;

            // Update hull health from real-time status file
            _controlFactory.HullHealthValueLabel.Text = $"{status.HullHealth:P0}";
        }

        public void UpdateShipLoadout(ShipLoadout loadout)
        {
            if (_controlFactory == null) return;

            var modulesListView = _controlFactory.ShipModulesTreeView;

            modulesListView.BeginUpdate(); // Use TreeView's BeginUpdate
            modulesListView.Nodes.Clear(); // Use Nodes collection instead of Items

            // Group modules by their slot name for organized display
            var groupedModules = loadout.Modules
                                        .OrderBy(m => GetSlotSortOrder(m.Slot))
                                        .ThenBy(m => m.Slot)
                                        .GroupBy(m => GetFriendlySlotName(m.Slot));
            
            foreach (var group in groupedModules)
            {
                // Create a top-level node for the group (e.g., "Hardpoints")
                var groupNode = new TreeNode($"{group.Key} ({group.Count()})");
                modulesListView.Nodes.Add(groupNode);

                foreach (var module in group)
                {
                    // Create a child node for each module in the group.
                    // The actual text will be drawn by the OwnerDraw event, but we set it for clarity.
                    var moduleNode = new TreeNode(ItemNameService.TranslateModuleName(module.Item))
                    {
                        // Store the full module object in the Tag for the custom drawing and tooltip logic.
                        Tag = module,
                        ToolTipText = CreateModuleToolTip(module)
                    };
                    groupNode.Nodes.Add(moduleNode);
                }
                // Automatically expand the group nodes to show the modules.
                groupNode.Expand();
            }

            modulesListView.EndUpdate();

            // Update ship stats
            _controlFactory.HullHealthValueLabel.Text = $"{loadout.HullHealth:P0}";
            _controlFactory.MassValueLabel.Text = $"{loadout.UnladenMass:N2} T";
            _controlFactory.CargoValueLabel.Text = $"{loadout.CargoCapacity:N0} T";

            // Correctly handle nullable double for jump range
            double? maxJumpRange = loadout.MaxJumpRange;
            if (maxJumpRange.HasValue)
            {
                _controlFactory.JumpRangeValueLabel.Text = $"{maxJumpRange.Value:N2} LY";
                _controlFactory.RebuyValueLabel.Text = $"{loadout.Rebuy:N0} CR";
                if (loadout.FuelCapacity != null)
                {
                    _controlFactory.FuelValueLabel.Text = $"{loadout.FuelCapacity.Main:N2} T / {loadout.FuelCapacity.Reserve:N2} T";
                }
                else
                {
                    _controlFactory.FuelValueLabel.Text = "N/A";
                }
            }
            else
            {
                _controlFactory.JumpRangeValueLabel.Text = "N/A";
                _controlFactory.RebuyValueLabel.Text = "N/A";
                _controlFactory.FuelValueLabel.Text = "N/A";
            }
        }

        private string CreateModuleToolTip(ShipModule module)
        {
            if (module.Engineering == null)
            {
                return ItemNameService.TranslateModuleName(module.Item);
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Blueprint: {ItemNameService.TranslateBlueprintName(module.Engineering.BlueprintName)} (G{module.Engineering.Level})");

            if (!string.IsNullOrEmpty(module.Engineering.ExperimentalEffect_Localised))
            {
                sb.AppendLine($"Experimental: {module.Engineering.ExperimentalEffect_Localised}");
            }

            if (module.Engineering.Modifiers.Any())
            {
                sb.AppendLine(); // Add a blank line for spacing
                foreach (var mod in module.Engineering.Modifiers.OrderBy(m => m.Label))
                {
                    string indicator = "";
                    // The 'LessIsGood' property is an integer (0 or 1), not a nullable bool.
                    // We check if it's 0 or 1 to determine the logic.
                    if (mod.LessIsGood == 0 || mod.LessIsGood == 1)
                    {
                        bool isGood = mod.Value > mod.OriginalValue ? (mod.LessIsGood == 0) : (mod.LessIsGood == 1);
                        indicator = isGood ? " ▲" : " ▼";
                    }
                    sb.AppendLine($"{mod.Label}: {mod.Value:N2}{indicator}");
                }
            }

            return sb.ToString().Trim();
        }

        private string GetFriendlySlotName(string slot)
        {
            if (slot.Contains("Hardpoint")) return "Hardpoints";
            if (slot.Contains("Slot")) return "Optional Internals";
            if (slot.Contains("Military")) return "Military Slots";
            if (slot.Contains("Armour")) return "Armour";
            if (slot.Contains("PowerPlant")) return "Power Plant";
            if (slot.Contains("MainEngines")) return "Thrusters";
            if (slot.Contains("FrameShiftDrive")) return "Frame Shift Drive";
            if (slot.Contains("LifeSupport")) return "Life Support";
            if (slot.Contains("PowerDistributor")) return "Power Distributor";
            if (slot.Contains("Radar")) return "Sensors";
            if (slot.Contains("FuelTank")) return "Fuel Tank";
            return "Other";
        }

        private int GetSlotSortOrder(string slot)
        {
            if (slot.Contains("Hardpoint")) return 1;
            if (slot.Contains("Armour")) return 2;
            if (slot.Contains("PowerPlant")) return 3;
            if (slot.Contains("MainEngines")) return 4;
            if (slot.Contains("FrameShiftDrive")) return 5;
            if (slot.Contains("LifeSupport")) return 6;
            if (slot.Contains("PowerDistributor")) return 7;
            if (slot.Contains("Radar")) return 8;
            if (slot.Contains("FuelTank")) return 9;
            if (slot.Contains("Military")) return 10;
            if (slot.Contains("Slot")) return 11;
            return 99;
        }
    }
}