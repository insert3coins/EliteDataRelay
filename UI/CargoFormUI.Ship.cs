using EliteDataRelay.Models;
using EliteDataRelay.Services;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class CargoFormUI
    {
        private double? _maxFuel;
        private int? _maxCargo;
        private ShipLoadout? _lastLoadout;
        private StatusFile? _lastStatus;

        public void UpdateShipLoadout(ShipLoadout loadout)
        {
            if (_controlFactory == null) return;

            // Update the ship stats panel
            if (loadout != null)
            {
                SetHullHealthVisuals(loadout.HullHealth);
                _controlFactory.RebuyValueLabel.Text = $"{loadout.Rebuy:N0} CR";

                if (loadout.FuelCapacity != null)
                {
                    _maxFuel = loadout.FuelCapacity.Main + loadout.FuelCapacity.Reserve;
                    _controlFactory.FuelValueLabel.Text = $"? / {_maxFuel.Value:F2} T";
                }
                else
                {
                    _maxFuel = null;
                    _controlFactory.FuelValueLabel.Text = "N/A";
                }

                _maxCargo = loadout.CargoCapacity;
                _controlFactory.CargoValueLabel.Text = $"? / {_maxCargo} T";
            }

            _lastLoadout = loadout;
            UpdateJumpRangeDisplay();
            UpdateMassDisplay();


            var treeView = _controlFactory.ShipModulesTreeView;

            treeView.BeginUpdate();
            treeView.Nodes.Clear();

            if (loadout?.Modules == null || !loadout.Modules.Any())
            {
                treeView.Nodes.Add("No loadout data available.").ForeColor = SystemColors.GrayText;
                treeView.EndUpdate();
                return;
            }

            // Create category nodes
            var hardpointsNode = treeView.Nodes.Add("Hardpoints");
            var utilityNode = treeView.Nodes.Add("Utility Mounts");
            var coreNode = treeView.Nodes.Add("Core Internals");
            var optionalNode = treeView.Nodes.Add("Optional Internals");

            var moduleGroups = loadout.Modules
                .GroupBy(GetModuleCategory)
                .ToDictionary(g => g.Key, g => g.ToList());

            PopulateModuleCategory(hardpointsNode, moduleGroups.GetValueOrDefault("Hardpoints"));
            PopulateModuleCategory(utilityNode, moduleGroups.GetValueOrDefault("Utility"));
            PopulateModuleCategory(coreNode, moduleGroups.GetValueOrDefault("Core"));
            PopulateModuleCategory(optionalNode, moduleGroups.GetValueOrDefault("Optional"));

            treeView.ExpandAll();
            treeView.EndUpdate();
        }

        public void UpdateShipStatus(StatusFile status)
        {
            if (_controlFactory == null) return;

            _lastStatus = status;
            UpdateMassDisplay();
            // UpdateJumpRangeDisplay(); // Jump range is now only updated on Loadout.

            if (status.HullHealth.HasValue)
            {
                SetHullHealthVisuals(status.HullHealth.Value);
            }

            if (status.Fuel != null && _maxFuel.HasValue)
            {
                var currentFuel = status.Fuel.FuelMain + status.Fuel.FuelReservoir;
                _controlFactory.FuelValueLabel.Text = $"{currentFuel:F2} / {_maxFuel.Value:F2} T";
            }

            if (_maxCargo.HasValue)
            {
                // The status file provides current cargo tonnage, which is what we want to display.
                _controlFactory.CargoValueLabel.Text = $"{status.Cargo:F0} / {_maxCargo.Value} T";
            }
        }

        private void UpdateJumpRangeDisplay()
        {
            if (_controlFactory == null) return;

            var label = _controlFactory.JumpRangeValueLabel;

            if (_lastLoadout != null)
            {
                // We are removing the complex calculations and only displaying
                // the MaxJumpRange value provided directly by the game's Loadout event.
                // This value represents the unladen jump range with a full main tank.
                label.Text = $"{_lastLoadout.MaxJumpRange:F2} LY";
                var tooltip = $"Max (Unladen): {_lastLoadout.MaxJumpRange:F2} LY";
                _controlFactory.ToolTip.SetToolTip(label, tooltip);
            }
            else
            {
                label.Text = "N/A";
            }
        }

        private void UpdateMassDisplay()
        {
            if (_controlFactory == null) return;

            var label = _controlFactory.MassValueLabel;
            if (_lastLoadout == null)
            {
                label.Text = "N/A";
                return;
            }

            // The journal's 'UnladenMass' is the total mass of the ship including hull and all modules (with empty tanks).
            double shipBaseMass = _lastLoadout.UnladenMass;

            // Current mass starts with the ship's base mass (hull + modules).
            double currentMass = shipBaseMass;
            if (_lastStatus != null)
            {
                // The in-game UI mass display includes both main and reserve fuel tanks.
                currentMass += (_lastStatus.Fuel?.FuelMain ?? 0) + (_lastStatus.Fuel?.FuelReservoir ?? 0) + _lastStatus.Cargo;
            }

            label.Text = $"{currentMass:F1} T";
            _controlFactory.ToolTip.SetToolTip(label, null);
        }

        private void SetHullHealthVisuals(double hullHealth)
        {
            if (_controlFactory == null) return;

            var label = _controlFactory.HullHealthValueLabel;
            label.Text = $"{hullHealth:P1}";

            if (hullHealth > 0.75)
                label.ForeColor = Color.FromArgb(0, 128, 0); // Dark Green
            else if (hullHealth > 0.35)
                label.ForeColor = Color.Orange;
            else
                label.ForeColor = Color.DarkRed;
        }

        private void PopulateModuleCategory(TreeNode categoryNode, List<ShipModule>? modules)
        {
            if (modules == null || !modules.Any())
            {
                categoryNode.Nodes.Add("Empty").ForeColor = SystemColors.GrayText;
            }
            else
            {
                // Sort modules by their slot name for a consistent order
                foreach (var module in modules.OrderBy(m => m.Slot))
                {
                    // The node's text is just a placeholder; all rendering is done in the DrawNode event.
                    // We store the full module object in the Tag for the drawing routine to use.
                    var node = categoryNode.Nodes.Add(module.Item);
                    node.Tag = module;
                    node.ToolTipText = BuildModuleTooltip(module);
                }
            }
            categoryNode.Text = $"{categoryNode.Text} ({modules?.Count ?? 0})";
        }

        private string BuildModuleTooltip(ShipModule module)
        {
            var tooltip = new StringBuilder();
            tooltip.AppendLine($"Slot: {module.Slot}");
            tooltip.AppendLine($"Powered: {(module.On ? "On" : "Off")}");
            tooltip.AppendLine($"Priority: {module.Priority}");
            tooltip.AppendLine($"Health: {module.Health:P1}");

            if (module.Engineering != null)
            {
                tooltip.AppendLine("---");
                tooltip.AppendLine($"Engineer: {module.Engineering.Engineer}");
                tooltip.AppendLine($"Blueprint: {module.Engineering.BlueprintName} (G{module.Engineering.Level})");
                if (!string.IsNullOrEmpty(module.Engineering.ExperimentalEffect_Localised))
                {
                    tooltip.AppendLine($"Experimental: {module.Engineering.ExperimentalEffect_Localised}");
                }

                if (module.Engineering.Modifiers.Any())
                {
                    tooltip.AppendLine("---");
                    tooltip.AppendLine("Modifications:");
                    foreach (var mod in module.Engineering.Modifiers.OrderBy(m => m.Label))
                    {
                        if (mod.Label.Equals("DamageType", StringComparison.OrdinalIgnoreCase)) continue;

                        bool isGood = (mod.Value > mod.OriginalValue && mod.LessIsGood == 0) || (mod.Value < mod.OriginalValue && mod.LessIsGood == 1);
                        bool isBad = (mod.Value < mod.OriginalValue && mod.LessIsGood == 0) || (mod.Value > mod.OriginalValue && mod.LessIsGood == 1);

                        string indicator = isGood ? " ▲" : (isBad ? " ▼" : "");
                        tooltip.AppendLine($"  • {mod.Label}: {mod.Value:N2}{indicator}");
                    }
                }
            }
            return tooltip.ToString();
        }

        private string GetModuleCategory(ShipModule module)
        {
            string slot = module.Slot.ToLowerInvariant();

            if (slot.StartsWith("hardpoint")) return "Hardpoints";
            if (slot.StartsWith("utility")) return "Utility";
            if (slot.StartsWith("slot") || slot.StartsWith("military")) return "Optional";

            switch (module.Slot)
            {
                case "Armour":
                case "PowerPlant":
                case "MainEngines":
                case "FrameShiftDrive":
                case "LifeSupport":
                case "PowerDistributor":
                case "Radar":
                case "FuelTank":
                    return "Core";
                default:
                    return "Optional"; // Fallback for any other slots
            }
        }
    }
}
