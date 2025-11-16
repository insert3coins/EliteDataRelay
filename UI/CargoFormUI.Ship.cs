using EliteDataRelay.Models;
using EliteDataRelay.Services;
using System;
using System.Text;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class CargoFormUI
    {
        private ShipLoadout? _currentLoadout;
        private Status? _currentStatus;

        public void InitializeShipTab()
        {
            CreateModuleTabs();
        }

        public ShipLoadout? GetCurrentLoadout() => _currentLoadout;

        public void UpdateShipStatus(Status status)
        {
            if (_controlFactory == null) return;
            _currentStatus = status;

            // This method is now only responsible for caching the status.
            // All stats that depend on the loadout are updated in UpdateShipLoadout.
        }

        public void UpdateShipLoadout(ShipLoadout loadout)
        {
            if (_controlFactory == null) return;
            _currentLoadout = loadout;
            System.Diagnostics.Debug.WriteLine($"[CargoFormUI] UpdateShipLoadout called for ship: {loadout.Ship}, Mass: {loadout.UnladenMass}");

            UpdateShipStatsPanel(loadout);
            UpdateModuleList();
            // No longer need to invalidate, as the image is set directly.
        }

        private void UpdateShipStatsPanel(ShipLoadout loadout)
        {
            var statsPanel = _controlFactory?.ShipStatsPanel;
            if (statsPanel is null) return;

            System.Diagnostics.Debug.WriteLine("[CargoFormUI] Updating ship stats panel...");
            statsPanel.SuspendLayout();


            // Find the power plant to get its capacity.
            var powerPlant = loadout.Modules.FirstOrDefault(m => m.Slot.Contains("PowerPlant"));
            double powerCapacity = 0;
            if (powerPlant?.Engineering?.Modifiers != null)
            {
                // For engineered modules, the 'PowerCapacity' modifier is the source of truth.
                var powerCapModifier = powerPlant.Engineering.Modifiers.FirstOrDefault(mod => mod.Label.Equals("PowerCapacity", StringComparison.OrdinalIgnoreCase));
                if (powerCapModifier != null)
                {
                    powerCapacity = powerCapModifier.Value;
                }
            }
            else if (powerPlant != null)
            {
                // For stock modules, we get the base capacity from our static data service.
                powerCapacity = ModuleDataService.GetPowerPlantCapacity(powerPlant.Item);
            }

            // Calculate current and laden jump ranges using the dedicated calculator service.
            var jumpRangeResult = JumpRangeCalculator.Calculate(loadout, _currentStatus);
            string jumpText = jumpRangeResult != null
                ? $"{jumpRangeResult.Current:F2} / {jumpRangeResult.Laden:F2} LY"
                : $"{loadout.MaxJumpRange:F2} LY";

            var stats = new[]
            {
                ("MASS", $"{loadout.UnladenMass:N1} T"),
                ("ARMOR", $"{(loadout.HullHealth * 100):N0}%"),
                ("CARGO", $"{loadout.CargoCapacity} T"),
                ("JUMP", jumpText),
                ("REBUY", $"{loadout.Rebuy:N0} CR"),
                ("POWER", $"{powerCapacity:F2} MW")
            };

            var toolTipTexts = new[]
            {
                $"Unladen Mass: {loadout.UnladenMass:N1} T",
                $"Hull Health: {(loadout.HullHealth * 100):N0}%",
                $"Cargo Capacity: {loadout.CargoCapacity} T",
                $"Jump Range (Current / Laden): {jumpText}",
                $"Insurance Rebuy Cost: {loadout.Rebuy:N0} CR",
                $"Power Plant Capacity: {powerCapacity:F2} MW"
            };

            // If first time, create panels. Otherwise, update them.
            if (statsPanel.Controls.Count == 0 && _fontManager != null)
            {
                for (int i = 0; i < stats.Length; i++)
                {
                    var statPanel = new ControlFactory.StatPanel(stats[i].Item1, stats[i].Item2, _fontManager.ConsolasFont);
                    _controlFactory?.ToolTip.SetToolTip(statPanel, toolTipTexts[i]);
                    statsPanel.Controls.Add(statPanel, 0, i);
                }
            }
            else
            {
                for (int i = 0; i < Math.Min(stats.Length, statsPanel.Controls.Count); i++)
                {
                    var statPanel = statsPanel.Controls[i] as ControlFactory.StatPanel;
                    if (statPanel != null)
                    {
                        statPanel.SetValue(stats[i].Item2);
                        _controlFactory?.ToolTip.SetToolTip(statPanel, toolTipTexts[i]);
                    }
                }
            }
            statsPanel.ResumeLayout();

            // Update the fuel label separately
            if (_controlFactory?.ShipFuelLabel != null && loadout.FuelCapacity != null)
            {
                string fuelText = $"Fuel: {loadout.FuelCapacity.Main:F1} / {loadout.FuelCapacity.Reserve:F1} T";
                _controlFactory.ShipFuelLabel.Text = fuelText;
                _controlFactory.ToolTip.SetToolTip(_controlFactory.ShipFuelLabel, $"Main Tank: {loadout.FuelCapacity.Main:F1} T\nReserve Tank: {loadout.FuelCapacity.Reserve:F1} T");
            }

            // Update the value label
            if (_controlFactory?.ShipValueLabel != null)
            {
                long totalValue = loadout.HullValue + loadout.ModulesValue;
                string valueText = $"Value: {totalValue:N0} CR";
                _controlFactory.ShipValueLabel.Text = valueText;
                _controlFactory.ToolTip.SetToolTip(_controlFactory.ShipValueLabel, $"Hull: {loadout.HullValue:N0} CR\nModules: {loadout.ModulesValue:N0} CR");
            }
        }

        private void CreateModuleTabs()
        {
            var tabControl = _controlFactory?.ModuleTabControl;
            if (tabControl is null) return;

            tabControl.TabPages.Clear();

            var tabs = new[] { "Core", "Hardpoints", "Utility", "Optional" };

            foreach (var tab in tabs)
            {
                tabControl.TabPages.Add(CreateModuleListPage(tab));
            }
        }

        private void UpdateModuleList()
        {
            if (_currentLoadout is null) return;
            if (_controlFactory?.ModuleTabControl is null) return;

            foreach (TabPage tabPage in _controlFactory.ModuleTabControl.TabPages)
            {
                if (tabPage.Controls.Count > 0 && tabPage.Controls[0] is Panel modulePanel)
                {
                    modulePanel.SuspendLayout();
                    modulePanel.Controls.Clear();
                    var modules = GetModulesForTab(tabPage.Text);
                    int yPos = 0;
                    foreach (var module in modules)
                    {
                        var moduleControl = new ModulePanel(module, _currentLoadout?.Ship ?? string.Empty, _fontManager);
                        moduleControl.Tag = module; // Tag the control with the module data for the tooltip drawer
                        
                        // Add tooltip for engineering details
                        if (module.Engineering != null && _controlFactory != null)
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine($"Blueprint: {BlueprintDataService.GetBlueprintName(module.Engineering.BlueprintName)} (G{module.Engineering.Level})");
                            sb.AppendLine($"Engineer: {module.Engineering.Engineer}");
                            sb.AppendLine("--------------------");

                            foreach (var modifier in module.Engineering.Modifiers.OrderBy(m => m.Label))
                            {
                                bool isGood = modifier.Value > modifier.OriginalValue;
                                if (modifier.LessIsGood == 1)
                                {
                                    isGood = !isGood;
                                }
                                string indicator = isGood ? "▲" : "▼";

                                sb.AppendLine($"{modifier.Label}: {modifier.Value:N2} (was {modifier.OriginalValue:N2}) {indicator}");
                            }

                            if (!string.IsNullOrEmpty(module.Engineering.ExperimentalEffect_Localised))
                            {
                                sb.AppendLine("--------------------");
                                sb.AppendLine($"Experimental: {BlueprintDataService.GetBlueprintName(module.Engineering.ExperimentalEffect_Localised)}");
                            }

                            _controlFactory.ToolTip.SetToolTip(moduleControl, sb.ToString());
                        }
                        else if (_controlFactory != null)
                        {
                            // Ensure no old tooltip persists if the module is not engineered
                            _controlFactory.ToolTip.SetToolTip(moduleControl, string.Empty);
                        }

                        moduleControl.Location = new Point(0, yPos);
                        modulePanel.Controls.Add(moduleControl);
                        yPos += moduleControl.Height;
                    }
                    modulePanel.ResumeLayout();
                }
            }
        }

        private TabPage CreateModuleListPage(string title)
        {
            var tabPage = new TabPage(title)
            {
                BackColor = Color.FromArgb(42, 50, 60), // Dark background for the tab page content area
                Padding = Padding.Empty
            };

            var moduleListPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(42, 50, 60),
                AutoScroll = true
            };
            
            tabPage.Controls.Add(moduleListPanel);

            return tabPage;
        }

        /// <summary>
        /// A custom control to display a single ship module, replacing the owner-drawn ListBox item.
        /// </summary>
        public class ModulePanel : Panel
        {
            private readonly ShipModule _module;
            private readonly string _shipInternalName;

            public ModulePanel(ShipModule module, string shipInternalName, FontManager? fontManager)
            {
                _module = module;
                _shipInternalName = shipInternalName;

                this.Dock = DockStyle.Top;
                this.Height = 50;
                this.BackColor = Color.FromArgb(51, 65, 85); // Slate-700
                this.Font = fontManager?.ConsolasFont ?? new Font("Consolas", 9F);
                this.DoubleBuffered = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                // Define colors from the React example
                var slotColor = Color.FromArgb(103, 232, 249); // Cyan-ish
                var moduleInfoColor = Color.FromArgb(156, 163, 175); // Gray
                var integrityColor = Color.FromArgb(52, 211, 153); // Emerald
                var integrityLabelColor = Color.FromArgb(156, 163, 175); // Gray

                // --- Prepare text for drawing ---
                string slotText = _module.Slot;
                string moduleText = ModuleDataService.GetModuleDisplayName(_module, _shipInternalName);

                if (string.IsNullOrEmpty(moduleText)) return;

                string integrityLabel = "Integrity";
                string integrityValue = $"{_module.Health * 100:F0}%";

                if (_module.Engineering != null)
                {
                    string blueprintName = BlueprintDataService.GetBlueprintName(_module.Engineering.BlueprintName);
                    string experimentalEffect = !string.IsNullOrEmpty(_module.Engineering.ExperimentalEffect_Localised) ? 
                                                BlueprintDataService.GetBlueprintName(_module.Engineering.ExperimentalEffect_Localised) : "Engineered";
                    integrityLabel = $"G{_module.Engineering.Level} {blueprintName}";
                    integrityValue = experimentalEffect;
                }

                // --- Define drawing rectangles ---
                var textFormatLeft = TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
                var textFormatRight = TextFormatFlags.Right | TextFormatFlags.NoPadding;

                var contentBounds = new Rectangle(10, 5, this.Width - 20, this.Height - 10);

                var slotRect = new Rectangle(contentBounds.Left, contentBounds.Top, contentBounds.Width / 2, contentBounds.Height / 2);
                var moduleRect = new Rectangle(contentBounds.Left, contentBounds.Top + 20, contentBounds.Width - 160, contentBounds.Height / 2);

                var integrityLabelRect = new Rectangle(contentBounds.Right - 150, contentBounds.Top, 150, contentBounds.Height / 2);
                var integrityValueRect = new Rectangle(contentBounds.Right - 150, contentBounds.Top + 20, 150, contentBounds.Height / 2);

                // --- Draw text ---
                using (var smallFont = new Font("Segoe UI", 8f))
                {
                    TextRenderer.DrawText(e.Graphics, slotText, this.Font, slotRect, slotColor, textFormatLeft);
                    TextRenderer.DrawText(e.Graphics, moduleText, smallFont, moduleRect, moduleInfoColor, textFormatLeft);

                    if (!string.IsNullOrEmpty(_module.Item))
                    {
                        TextRenderer.DrawText(e.Graphics, integrityLabel, smallFont, integrityLabelRect, integrityLabelColor, textFormatRight);
                        TextRenderer.DrawText(e.Graphics, integrityValue, this.Font, integrityValueRect, integrityColor, textFormatRight);
                    }
                }
            }
        }

        private IEnumerable<ShipModule> GetModulesForTab(string tabName)
        {
            if (_currentLoadout == null) return Enumerable.Empty<ShipModule>();

            return tabName.ToLowerInvariant() switch
            {
                "core" => _currentLoadout.Modules.Where(m => !string.IsNullOrEmpty(m.Item) && IsCoreModule(m.Slot)).OrderBy(m => GetSlotSortOrder(m.Slot)), 
                "hardpoints" => _currentLoadout.Modules.Where(m => !string.IsNullOrEmpty(m.Item) && m.Slot.Contains("Hardpoint")).OrderBy(m => m.Slot), 
                "utility" => _currentLoadout.Modules.Where(m => !string.IsNullOrEmpty(m.Item) && m.Slot.Contains("Utility")).OrderBy(m => m.Slot), 
                "optional" => _currentLoadout.Modules.Where(m => !string.IsNullOrEmpty(m.Item) && (m.Slot.Contains("Slot") || m.Slot.Contains("Military"))).OrderBy(m => GetSlotSortOrder(m.Slot)).ThenBy(m => m.Slot),
                _ => Enumerable.Empty<ShipModule>()
            };
        }

        private bool IsCoreModule(string slot)
        {
            return !slot.Contains("Hardpoint") && !slot.Contains("Utility") && !slot.Contains("Slot") && !slot.Contains("Military") && !slot.Contains("Armour");
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
