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
        public void UpdateShipLoadout(ShipLoadout loadout)
        {
            if (_controlFactory == null) return;

            var treeView = _controlFactory.ShipModulesTreeView;

            // Store the path of the top-most visible node to prevent the view from scrolling on update.
            string? topNodePath = treeView.TopNode?.FullPath;

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

            // Group modules by slot type using a more efficient and readable GroupBy
            var groupedModules = loadout.Modules.GroupBy(m =>
            {
                if (m.Slot.Contains("Hardpoint", StringComparison.OrdinalIgnoreCase)) return "Hardpoints";
                if (m.Slot.Contains("Utility", StringComparison.OrdinalIgnoreCase)) return "Utility";
                if (m.Slot.StartsWith("Slot", StringComparison.OrdinalIgnoreCase)) return "Optional";
                return "Core";
            });

            foreach (var group in groupedModules)
            {
                var sortedModules = group.OrderBy(m => m.Slot);
                switch (group.Key)
                {
                    case "Hardpoints":
                        PopulateModuleCategory(hardpointsNode, sortedModules);
                        break;
                    case "Utility":
                        PopulateModuleCategory(utilityNode, sortedModules);
                        break;
                    case "Core":
                        PopulateModuleCategory(coreNode, sortedModules);
                        break;
                    case "Optional":
                        PopulateModuleCategory(optionalNode, sortedModules);
                        break;
                }
            }

            // Expand all categories
            hardpointsNode.Expand();
            utilityNode.Expand();
            coreNode.Expand();
            optionalNode.Expand();

            // Restore the previous scroll position by finding the node that was at the top.
            if (topNodePath != null)
            {
                var nodeToRestore = FindNodeByFullPath(treeView.Nodes, topNodePath);
                if (nodeToRestore != null)
                {
                    treeView.TopNode = nodeToRestore;
                }
            }

            treeView.EndUpdate();
        }

        private void PopulateModuleCategory(TreeNode categoryNode, IEnumerable<ShipModule> modules)
        {
            if (!modules.Any())
            {
                categoryNode.Nodes.Add("Empty").ForeColor = SystemColors.GrayText;
                return;
            }

            foreach (var module in modules)
            {
                string moduleName = ItemNameService.TranslateModuleName(module.Item);
                string displayText = $"{module.Slot}: {moduleName}";

                // Append a summary of the engineering to the main display text for better visibility.
                if (module.Engineering != null)
                {
                    var engineeringSummary = new List<string>();
                    string blueprintName = ItemNameService.TranslateBlueprintName(module.Engineering.BlueprintName);
                    engineeringSummary.Add($"{blueprintName} G{module.Engineering.Level}");

                    if (!string.IsNullOrEmpty(module.Engineering.ExperimentalEffectLocalised))
                    {
                        engineeringSummary.Add(module.Engineering.ExperimentalEffectLocalised);
                    }

                    displayText += $" ({string.Join(", ", engineeringSummary)})";
                }
                var node = categoryNode.Nodes.Add(displayText);

                // Add Engineering details as expandable child nodes
                if (module.Engineering != null)
                {
                    string blueprintName = ItemNameService.TranslateBlueprintName(module.Engineering.BlueprintName);
                    node.Nodes.Add($"Blueprint: {blueprintName} G{module.Engineering.Level}").ForeColor = SystemColors.GrayText;
                    node.Nodes.Add($"Engineer: {module.Engineering.Engineer}").ForeColor = SystemColors.GrayText;
                    if (!string.IsNullOrEmpty(module.Engineering.ExperimentalEffectLocalised))
                    {
                        node.Nodes.Add($"Effect: {module.Engineering.ExperimentalEffectLocalised}").ForeColor = SystemColors.GrayText;
                    }

                    node.Nodes.Add($"Quality: {module.Engineering.Quality:P1}").ForeColor = SystemColors.GrayText;
                    node.Nodes.Add($"Integrity: {module.Health:P1}").ForeColor = SystemColors.GrayText;

                    if (module.Engineering.Modifiers.Any())
                    {
                        var modsNode = node.Nodes.Add("Modifications");
                        modsNode.ForeColor = SystemColors.GrayText;

                        foreach (var mod in module.Engineering.Modifiers)
                        {
                            bool isGood = (mod.Value > mod.OriginalValue && mod.LessIsGood == 0) || (mod.Value < mod.OriginalValue && mod.LessIsGood == 1);
                            bool isBad = (mod.Value < mod.OriginalValue && mod.LessIsGood == 0) || (mod.Value > mod.OriginalValue && mod.LessIsGood == 1);

                            string indicator = "";
                            Color modColor = SystemColors.ControlText;

                            if (isGood) { indicator = " ▲"; modColor = Color.FromArgb(0, 150, 0); } // Dark Green
                            if (isBad) { indicator = " ▼"; modColor = Color.FromArgb(192, 0, 0); } // Dark Red

                            var modNode = modsNode.Nodes.Add($"{mod.Label}: {mod.Value:N2}{indicator}");
                            modNode.ForeColor = modColor;
                        }
                    }
                }
                else
                {
                    // For non-engineered modules, still show the integrity.
                    node.Nodes.Add($"Integrity: {module.Health:P1}").ForeColor = SystemColors.GrayText;
                }

                // Add a basic tooltip with non-engineering info.
                var tooltip = new StringBuilder();
                tooltip.AppendLine($"Slot: {module.Slot}");
                tooltip.AppendLine($"Item: {module.Item}");
                tooltip.AppendLine($"Powered: {(module.IsOn ? "On" : "Off")}");
                tooltip.AppendLine($"Priority: {module.Priority}");
                node.ToolTipText = tooltip.ToString();
            }
        }

        private TreeNode? FindNodeByFullPath(TreeNodeCollection nodes, string path)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }
                TreeNode? foundNode = FindNodeByFullPath(node.Nodes, path);
                if (foundNode != null)
                {
                    return foundNode;
                }
            }
            return null;
        }
    }
}
