using EliteDataRelay.Models;
using EliteDataRelay.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
            AttachShipNameLink();
        }

        public ShipLoadout? GetCurrentLoadout() => _currentLoadout;

        public void UpdateShipStatus(Status status)
        {
            if (_controlFactory == null) return;
            _currentStatus = status;
            UpdateShipFuelSummary();
        }

        public void UpdateShipLoadout(ShipLoadout loadout)
        {
            if (_controlFactory == null) return;
            _currentLoadout = loadout;
            Debug.WriteLine($"[CargoFormUI] UpdateShipLoadout called for ship: {loadout.Ship}, Mass: {loadout.UnladenMass}");

            UpdateShipStatsPanel(loadout);
            UpdateModuleList();
            UpdateShipFuelSummary();
        }

        private void UpdateShipStatsPanel(ShipLoadout loadout)
        {
            var factory = _controlFactory;
            if (factory is null || _fontManager == null) return;

            Debug.WriteLine("[CargoFormUI] Updating ship stats panel...");

            string jumpText;
            var jumpRangeResult = JumpRangeCalculator.Calculate(loadout, _currentStatus);
            if (loadout.MaxJumpRange > 0)
            {
                // Trust journal-provided max jump range when available.
                jumpText = $"{loadout.MaxJumpRange:F2} LY";
            }
            else if (jumpRangeResult != null)
            {
                jumpText = $"{jumpRangeResult.Current:F2} / {jumpRangeResult.Laden:F2} LY";
            }
            else
            {
                jumpText = "N/A";
            }

            long totalValue = loadout.HullValue + loadout.ModulesValue;
            factory.ShipValueLabel.Text = $"{totalValue:N0} CR";
            factory.ToolTip.SetToolTip(factory.ShipValueLabel, $"Hull: {loadout.HullValue:N0} CR\nModules: {loadout.ModulesValue:N0} CR");

            factory.BottomMassLabel.Text = $"{loadout.UnladenMass:N1} T";
            factory.ToolTip.SetToolTip(factory.BottomMassLabel, $"Unladen Mass: {loadout.UnladenMass:N1} T");

            factory.BottomArmorLabel.Text = $"{(loadout.HullHealth * 100):N0}%";
            factory.ToolTip.SetToolTip(factory.BottomArmorLabel, $"Hull Health: {(loadout.HullHealth * 100):N0}%");

            factory.BottomCargoLabel.Text = $"{loadout.CargoCapacity} T";
            factory.ToolTip.SetToolTip(factory.BottomCargoLabel, $"Cargo Capacity: {loadout.CargoCapacity} T");

            factory.BottomJumpLabel.Text = jumpText;
            factory.ToolTip.SetToolTip(factory.BottomJumpLabel, $"Jump Range (Current / Laden): {jumpText}");

            factory.BottomRebuyLabel.Text = $"{loadout.Rebuy:N0} CR";
            factory.ToolTip.SetToolTip(factory.BottomRebuyLabel, $"Insurance Rebuy Cost: {loadout.Rebuy:N0} CR");
        }

        private void UpdateShipFuelSummary()
        {
            if (_controlFactory?.ShipFuelLabel == null)
            {
                return;
            }

            double currentMain = _currentStatus?.Fuel?.FuelMain ?? 0d;
            double currentReserve = _currentStatus?.Fuel?.FuelReservoir ?? 0d;

            double? capacityMain = _currentLoadout?.FuelCapacity?.Main;
            double? capacityReserve = _currentLoadout?.FuelCapacity?.Reserve;

            string mainDisplay = capacityMain.HasValue
                ? $"{currentMain:F1} / {capacityMain.Value:F1} T"
                : $"{currentMain:F1} T";

            string reserveDisplay = capacityReserve.HasValue
                ? $"{currentReserve:F1} / {capacityReserve.Value:F1} T"
                : $"{currentReserve:F1} T";

            _controlFactory.ShipFuelLabel.Text = $"Main: {mainDisplay}  |  Res: {reserveDisplay}";
            _controlFactory.ToolTip.SetToolTip(_controlFactory.ShipFuelLabel, $"Main Tank: {mainDisplay}\nReserve Tank: {reserveDisplay}");
        }

        private double CalculatePowerCapacity(ShipLoadout loadout)
        {
            var powerPlant = loadout.Modules.FirstOrDefault(m => m.Slot.Contains("PowerPlant"));
            if (powerPlant?.Engineering?.Modifiers != null)
            {
                var powerCapModifier = powerPlant.Engineering.Modifiers.FirstOrDefault(mod => mod.Label.Equals("PowerCapacity", StringComparison.OrdinalIgnoreCase));
                if (powerCapModifier != null)
                {
                    return powerCapModifier.Value;
                }
            }
            else if (powerPlant != null)
            {
                return ModuleDataService.GetPowerPlantCapacity(powerPlant.Item);
            }

            return 0;
        }

        private void AttachShipNameLink()
        {
            if (_controlFactory?.ShipTabNameLabel == null)
            {
                return;
            }

            _controlFactory.ShipTabNameLabel.Cursor = Cursors.Hand;
            _controlFactory.ShipTabNameLabel.Click -= OnShipNameClicked;
            _controlFactory.ShipTabNameLabel.Click += OnShipNameClicked;
        }

        private void OnShipNameClicked(object? sender, EventArgs e)
        {
            OpenShipyardLink();
        }

        private void OnShipLabelClicked(object? sender, EventArgs e)
        {
            OpenShipyardLink();
        }

        private void OpenShipyardLink()
        {
            var url = BuildEdsyUrl(_currentLoadout);

            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[CargoFormUI] Failed to open EDSY link: {ex.Message}");
            }
        }

        private static string? BuildEdsyUrl(ShipLoadout? loadout)
        {
            if (loadout == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(loadout.RawJson))
            {
                try
                {
                    var payload = Encoding.UTF8.GetBytes(loadout.RawJson);
                    using var memory = new MemoryStream();
                    using (var gzip = new GZipStream(memory, CompressionLevel.Optimal, leaveOpen: true))
                    {
                        gzip.Write(payload, 0, payload.Length);
                    }
                    var gzipped = memory.ToArray();
                    var base64 = Convert.ToBase64String(gzipped)
                        .TrimEnd('=')
                        .Replace('+', '-')
                        .Replace('/', '_');

                    return $"https://edsy.org/#/I={base64}";
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"[CargoFormUI] Failed to build EDSY payload: {ex.Message}");
                }
            }

            string slug = loadout.Ship;
            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = loadout.ShipName ?? "ship";
            }
            slug = new string(slug.Where(char.IsLetterOrDigit).ToArray());
            return $"https://edsy.org/#/l/{slug}";
        }

        private void UpdateModuleList()
        {
            if (_currentLoadout is null) return;
            if (_controlFactory == null) return;

            PopulateSidebar(_controlFactory.SidebarHardpointsPanel, GetModulesForTab("hardpoints"));
            PopulateSidebar(_controlFactory.SidebarUtilitiesPanel, GetModulesForTab("utility"));

            PopulateSection(_controlFactory.HardpointListPanel, GetModulesForTab("hardpoints"));
            PopulateSection(_controlFactory.UtilityListPanel, GetModulesForTab("utility"));
            PopulateSection(_controlFactory.CoreListPanel, GetModulesForTab("core"));
            PopulateSection(_controlFactory.OptionalListPanel, GetModulesForTab("optional"));
        }

        private void PopulateSidebar(FlowLayoutPanel? target, IEnumerable<ShipModule> modules)
        {
            if (target == null) return;

            target.SuspendLayout();
            target.Controls.Clear();
            bool attached = target.Tag is bool flag && flag;
            if (!attached)
            {
                target.Resize += (_, __) => ResizeSidebarChips(target);
                target.Tag = true;
            }

            var moduleList = modules.ToList();
            if (moduleList.Count == 0)
            {
                target.Controls.Add(CreateEmptyChip());
            }
            else
            {
                foreach (var module in moduleList)
                {
                    var chip = CreateSlotChip(module);
                    var tooltip = BuildEngineeringTooltip(module);
                    _controlFactory?.ToolTip.SetToolTip(chip, tooltip);
                    target.Controls.Add(chip);
                }
            }
            ResizeSidebarChips(target);
            target.ResumeLayout();
        }

        private void PopulateSection(FlowLayoutPanel? target, IEnumerable<ShipModule> modules)
        {
            if (target == null) return;
            if (_fontManager == null) return;

            target.SuspendLayout();
            target.Controls.Clear();

            bool attached = target.Tag is bool flag && flag;
            if (!attached)
            {
                target.Resize += (_, __) => ResizeLoadoutRows(target);
                target.Tag = true;
            }

            var moduleList = modules.ToList();
            if (moduleList.Count == 0)
            {
                var placeholder = new Label
                {
                    Text = "[EMPTY]",
                    AutoSize = false,
                    Width = Math.Max(220, target.ClientSize.Width - target.Padding.Horizontal),
                    Height = 34,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = Color.FromArgb(120, 120, 120),
                    BackColor = Color.FromArgb(24, 24, 24),
                    Padding = new Padding(6)
                };
                target.Controls.Add(placeholder);
            }
            else
            {
                foreach (var module in moduleList)
                {
                    var row = CreateLoadoutRow(module);
                    var tooltip = BuildEngineeringTooltip(module);
                    _controlFactory?.ToolTip.SetToolTip(row, tooltip);
                    target.Controls.Add(row);
                }
            }

            ResizeLoadoutRows(target);
            target.ResumeLayout();
        }

        private Control CreateSlotChip(ShipModule module)
        {
            var chip = new Panel
            {
                Width = 90,
                Height = 38,
                Margin = new Padding(4),
                BackColor = module.On ? Color.FromArgb(255, 102, 0) : Color.FromArgb(32, 28, 28),
                Padding = new Padding(6, 4, 6, 4)
            };

            var title = ModuleDataService.GetModuleDisplayName(module, _currentLoadout?.Ship ?? string.Empty);
            var label = new Label
            {
                Text = $"{FormatSlotName(module.Slot)}\n{title}",
                Dock = DockStyle.Fill,
                Font = _fontManager?.SegoeUIFont ?? new Font("Segoe UI", 8.5f),
                ForeColor = module.On ? Color.Black : Color.FromArgb(240, 196, 120),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.TopLeft
            };

            chip.Controls.Add(label);
            return chip;
        }

        private Control CreateEmptyChip()
        {
            var chip = new Panel
            {
                Width = 90,
                Height = 38,
                Margin = new Padding(4),
                BackColor = Color.FromArgb(40, 36, 32),
                Padding = new Padding(6, 4, 6, 4)
            };

            var label = new Label
            {
                Text = "EMPTY",
                Dock = DockStyle.Fill,
                Font = _fontManager?.SegoeUIFont ?? new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(200, 160, 120),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            chip.Controls.Add(label);
            return chip;
        }

        private static void ResizeSidebarChips(FlowLayoutPanel panel)
        {
            if (panel == null) return;
            int available = Math.Max(0, panel.ClientSize.Width - panel.Padding.Horizontal);
            int perChip = available > 0 ? (available / 2) - 6 : panel.ClientSize.Width - 6;
            perChip = Math.Max(70, perChip);
            foreach (Control child in panel.Controls)
            {
                child.Width = perChip;
            }
        }

        private Control CreateLoadoutRow(ShipModule module)
        {
            var row = new Panel
            {
                Width = 260,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(6, 4, 6, 4),
                BackColor = module.Engineering != null ? Color.FromArgb(255, 102, 0) : Color.FromArgb(24, 24, 24),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var nameLabel = new Label
            {
                Text = BuildModuleLabel(module),
                Dock = DockStyle.Fill,
                Font = _fontManager?.SegoeUIFontBold ?? new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = module.Engineering != null ? Color.Black : Color.FromArgb(235, 235, 235),
                BackColor = Color.Transparent,
                AutoSize = true,
                MaximumSize = new Size(4000, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };

            string meta = $"P{module.Priority}" + (module.On ? " • ON" : " • OFF");
            if (module.Engineering != null)
            {
                meta += " • ENG";
            }

            var metaLabel = new Label
            {
                Text = meta,
                Dock = DockStyle.Fill,
                Font = _fontManager?.SegoeUIFont ?? new Font("Segoe UI", 8f),
                ForeColor = module.Engineering != null ? Color.Black : Color.FromArgb(210, 180, 120),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
                AutoSize = true
            };

            string engSummary = BuildEngineeringSummary(module);
            var engLabel = new Label
            {
                Text = engSummary,
                Dock = DockStyle.Fill,
                Font = _fontManager?.SegoeUIFont ?? new Font("Segoe UI", 8f),
                ForeColor = module.Engineering != null ? Color.FromArgb(40, 30, 0) : Color.FromArgb(160, 160, 160),
                BackColor = Color.Transparent,
                AutoSize = true,
                MaximumSize = new Size(4000, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 0, 0, 0),
                Visible = !string.IsNullOrWhiteSpace(engSummary)
            };

            content.Controls.Add(nameLabel, 0, 0);
            content.Controls.Add(metaLabel, 1, 0);
            content.Controls.Add(engLabel, 0, 1);
            content.SetColumnSpan(engLabel, 2);

            row.Controls.Add(content);
            row.Resize += (_, __) =>
            {
                int available = row.ClientSize.Width - row.Padding.Horizontal - metaLabel.PreferredSize.Width - 6;
                if (available > 0)
                {
                    nameLabel.MaximumSize = new Size(available, 0);
                    engLabel.MaximumSize = new Size(row.ClientSize.Width - row.Padding.Horizontal, 0);
                }
            };

            return row;
        }

        private string BuildModuleLabel(ShipModule module)
        {
            string slotLabel = FormatSlotName(module.Slot);
            string moduleText = ModuleDataService.GetModuleDisplayName(module, _currentLoadout?.Ship ?? string.Empty);
            return $"{slotLabel}  {moduleText}";
        }

        private string BuildEngineeringTooltip(ShipModule module)
        {
            if (module.Engineering == null)
            {
                return string.Empty;
            }

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
                string indicator = isGood ? "\u001e" : "\u001f";
                sb.AppendLine($"{modifier.Label}: {modifier.Value:N2} (was {modifier.OriginalValue:N2}) {indicator}");
            }

            if (!string.IsNullOrEmpty(module.Engineering.ExperimentalEffect_Localised))
            {
                sb.AppendLine("--------------------");
                sb.AppendLine($"Experimental: {BlueprintDataService.GetBlueprintName(module.Engineering.ExperimentalEffect_Localised)}");
            }

            return sb.ToString();
        }

        private static string BuildEngineeringSummary(ShipModule module)
        {
            if (module.Engineering == null) return string.Empty;
            string blueprint = BlueprintDataService.GetBlueprintName(module.Engineering.BlueprintName);
            string experimental = string.IsNullOrEmpty(module.Engineering.ExperimentalEffect_Localised)
                ? string.Empty
                : BlueprintDataService.GetBlueprintName(module.Engineering.ExperimentalEffect_Localised);

            return string.IsNullOrEmpty(experimental)
                ? $"G{module.Engineering.Level} {blueprint}"
                : $"G{module.Engineering.Level} {blueprint} • {experimental}";
        }

        private static void ResizeLoadoutRows(FlowLayoutPanel panel)
        {
            if (panel == null) return;
            int adjustment = panel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
            var width = Math.Max(220, panel.ClientSize.Width - panel.Padding.Horizontal - adjustment - 4);
            foreach (Control child in panel.Controls)
            {
                child.Width = width;
            }
        }

        private static string FormatSlotName(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot)) return "Slot";
            return slot.Replace("_", " ").Replace("Slot", "Slot ").Replace(" ", " ").Trim();
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
