using EliteDataRelay.Models;
using EliteDataRelay.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
            System.Diagnostics.Debug.WriteLine($"[CargoFormUI] UpdateShipLoadout called for ship: {loadout.Ship}, Mass: {loadout.UnladenMass}");

            UpdateShipStatsPanel(loadout);
            UpdateModuleList();
            UpdateShipFuelSummary();
            // No longer need to invalidate, as the image is set directly.
        }

        private void UpdateShipStatsPanel(ShipLoadout loadout)
        {
            var statsPanel = _controlFactory?.ShipStatsPanel;
            if (statsPanel is null || _fontManager == null) return;

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
            if (statsPanel.Controls.Count != stats.Length)
            {
                statsPanel.Controls.Clear();
                statsPanel.RowStyles.Clear();
                int columns = Math.Max(1, statsPanel.ColumnCount);
                for (int i = 0; i < stats.Length; i++)
                {
                    int row = i / columns;
                    while (statsPanel.RowStyles.Count <= row)
                    {
                        statsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    }

                    var statPanel = new ControlFactory.StatPanel(stats[i].Item1, stats[i].Item2, _fontManager.ConsolasFont)
                    {
                        Margin = new Padding(6),
                        Dock = DockStyle.Fill
                    };
                    _controlFactory?.ToolTip.SetToolTip(statPanel, toolTipTexts[i]);
                    statsPanel.Controls.Add(statPanel, i % columns, row);
                }
            }
            else
            {
                for (int i = 0; i < stats.Length; i++)
                {
                    if (statsPanel.Controls[i] is ControlFactory.StatPanel statPanel)
                    {
                        statPanel.SetValue(stats[i].Item2);
                        _controlFactory?.ToolTip.SetToolTip(statPanel, toolTipTexts[i]);
                    }
                }
            }
            statsPanel.ResumeLayout();

            // Update the value label
            if (_controlFactory?.ShipValueLabel != null)
            {
                long totalValue = loadout.HullValue + loadout.ModulesValue;
                string valueText = $"Value: {totalValue:N0} CR";
                _controlFactory.ShipValueLabel.Text = valueText;
                _controlFactory.ToolTip.SetToolTip(_controlFactory.ShipValueLabel, $"Hull: {loadout.HullValue:N0} CR\nModules: {loadout.ModulesValue:N0} CR");
            }
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
                if (tabPage.Controls.Count > 0 && tabPage.Controls[0] is FlowLayoutPanel listPanel)
                {
                    listPanel.SuspendLayout();
                    listPanel.Controls.Clear();
                    var modules = GetModulesForTab(tabPage.Text);
                    foreach (var module in modules)
                    {
                        var moduleControl = new ModulePanel(module, _currentLoadout?.Ship ?? string.Empty, _fontManager);
                        moduleControl.Tag = module;

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
                            _controlFactory.ToolTip.SetToolTip(moduleControl, string.Empty);
                        }

                        listPanel.Controls.Add(moduleControl);
                    }

                    ResizeModuleCards(listPanel);
                    listPanel.ResumeLayout();
                }
            }
        }
        private TabPage CreateModuleListPage(string title)
        {
            var tabPage = new TabPage(title)
            {
                BackColor = Color.FromArgb(42, 50, 60),
                Padding = Padding.Empty
            };

            var moduleListPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(42, 50, 60),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(8)
            };
            moduleListPanel.Resize += (s, _) =>
            {
                if (s is FlowLayoutPanel panelRef)
                {
                    ResizeModuleCards(panelRef);
                }
            };

            tabPage.Controls.Add(moduleListPanel);

            return tabPage;
        }

        private static void ResizeModuleCards(FlowLayoutPanel panel)
        {
            if (panel == null) return;
            int adjustment = panel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
            var width = Math.Max(220, panel.ClientSize.Width - panel.Padding.Horizontal - adjustment - 4);
            foreach (Control child in panel.Controls)
            {
                child.Width = width;
            }
        }

        /// <summary>
        /// A custom control to display a single ship module, replacing the owner-drawn list item.
        /// </summary>
        public class ModulePanel : Panel
        {
            private readonly ShipModule _module;
            private readonly string _shipInternalName;
            private readonly Font _titleFont;
            private readonly Font _metaFont;
            private readonly StringFormat _leftAlign = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter };
            private readonly StringFormat _centerAlign = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            public ModulePanel(ShipModule module, string shipInternalName, FontManager? fontManager)
            {
                _module = module ?? throw new ArgumentNullException(nameof(module));
                _shipInternalName = shipInternalName;

                DoubleBuffered = true;
                BackColor = Color.FromArgb(34, 37, 48);
                Font = fontManager?.SegoeUIFont ?? new Font("Segoe UI", 8.5f);
                _titleFont = new Font(Font.FontFamily, Font.Size + 1f, FontStyle.Bold);
                _metaFont = new Font("Segoe UI", 8f);
                Height = 112;
                Margin = new Padding(0, 0, 0, 10);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                string slotText = FormatSlot(_module.Slot);
                string moduleText = ModuleDataService.GetModuleDisplayName(_module, _shipInternalName);
                if (string.IsNullOrWhiteSpace(moduleText)) return;

                string integrityText = $"Integrity {_module.Health * 100:0}%";
                string valueText = _module.Value > 0 ? $"{_module.Value:N0} CR" : $"{_module.Mass:N1} T";
                string engineeringText = BuildEngineeringText(_module);

                var rect = ClientRectangle;
                rect.Inflate(-2, -2);

                using (var path = DrawingUtils.CreateRoundedRectPath(rect, 10))
                using (var bg = new System.Drawing.Drawing2D.LinearGradientBrush(rect, Color.FromArgb(38, 43, 58), Color.FromArgb(24, 26, 32), 90f))
                using (var border = new Pen(Color.FromArgb(65, 134, 210), 1.2f))
                {
                    e.Graphics.FillPath(bg, path);
                    e.Graphics.DrawPath(border, path);
                }

                var content = Rectangle.Inflate(rect, -12, -10);
                var slotRect = new RectangleF(content.X, content.Y, content.Width, 18);
                var moduleRect = new RectangleF(content.X, slotRect.Bottom + 6, content.Width, 22);
                var infoRect = new RectangleF(content.X, moduleRect.Bottom + 4, content.Width, 18);
                var secondInfoRect = new RectangleF(content.X, infoRect.Bottom + 3, content.Width, 18);

                using var slotBrush = new SolidBrush(Color.FromArgb(158, 167, 187));
                e.Graphics.DrawString($"{slotText}  •  P{_module.Priority}", Font, slotBrush, slotRect, _leftAlign);

                using var moduleBrush = new SolidBrush(Color.FromArgb(232, 236, 245));
                e.Graphics.DrawString(moduleText, _titleFont, moduleBrush, moduleRect, _leftAlign);

                using var infoBrush = new SolidBrush(Color.FromArgb(182, 191, 208));
                e.Graphics.DrawString($"{integrityText}  •  {valueText}", _metaFont, infoBrush, infoRect, _leftAlign);

                if (!string.IsNullOrEmpty(engineeringText))
                {
                    using var engBrush = new SolidBrush(Color.FromArgb(94, 234, 212));
                    e.Graphics.DrawString(engineeringText, _metaFont, engBrush, secondInfoRect, _leftAlign);
                }
                else
                {
                    e.Graphics.DrawString($"Mass {_module.Mass:N1} T", _metaFont, infoBrush, secondInfoRect, _leftAlign);
                }
            }
            private static string BuildEngineeringText(ShipModule module)
            {
                if (module.Engineering == null) return string.Empty;
                var blueprint = BlueprintDataService.GetBlueprintName(module.Engineering.BlueprintName);
                var experimental = string.IsNullOrEmpty(module.Engineering.ExperimentalEffect_Localised)
                    ? string.Empty
                    : BlueprintDataService.GetBlueprintName(module.Engineering.ExperimentalEffect_Localised);

                return string.IsNullOrEmpty(experimental)
                    ? $"G{module.Engineering.Level} {blueprint}"
                    : $"G{module.Engineering.Level} {blueprint}  •  {experimental}";
            }

            private static string FormatSlot(string slot)
            {
                if (string.IsNullOrWhiteSpace(slot)) return "Slot";
                return slot.Replace("_", " ").Replace("Slot", "Slot ").Replace(" ", " ").Trim();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _titleFont.Dispose();
                    _metaFont.Dispose();
                    _leftAlign.Dispose();
                    _centerAlign.Dispose();
                }
                base.Dispose(disposing);
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






