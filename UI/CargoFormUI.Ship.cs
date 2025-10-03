using EliteDataRelay.Models;
using EliteDataRelay.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class CargoFormUI
    {
        private ShipLoadout? _currentLoadout;
        private string _activeModuleTab = "hardpoints";
        private StatusFile? _currentStatus;

        // Elite Orange color scheme
        private readonly Color _orangeColor = Color.FromArgb(255, 102, 0);
        private readonly Color _darkOrangeColor = Color.FromArgb(153, 61, 0);
        private readonly Color _lightOrangeColor = Color.FromArgb(255, 153, 68);

        private void InitializeShipTab()
        {
            // The ShipWireframeDrawer is no longer used.
            // The PictureBox is now updated directly with an image.
            CreateModuleTabButtons();
        }

        public ShipLoadout? GetCurrentLoadout() => _currentLoadout;

        public void UpdateShipStatus(StatusFile status)
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
            statsPanel.Controls.Clear();

            // Find the power plant to get its capacity.
            var powerPlant = loadout.Modules.FirstOrDefault(m => m.Slot.Equals("PowerPlant", StringComparison.OrdinalIgnoreCase));
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
                ("CARGO", $"{loadout.CargoCapacity}T"),
                ("JUMP", jumpText),
                ("REBUY", $"{loadout.Rebuy:N0} CR"),
                ("POWER", $"{powerCapacity:F2} MW")
            };

            for (int i = 0; i < stats.Length; i++)
            {
                var statPanel = new StatPanel(stats[i].Item1, stats[i].Item2, _lightOrangeColor);
                System.Diagnostics.Debug.WriteLine($"[CargoFormUI] Creating stat item: {stats[i].Item1} = {stats[i].Item2}");
                statsPanel.Controls.Add(statPanel, i % 3, i / 3);
            }

            statsPanel.ResumeLayout();
        }

        /// <summary>
        /// A custom-drawn panel to display a single ship statistic, avoiding complex control nesting.
        /// </summary>
        private class StatPanel : Panel
        {
            private readonly string _label;
            private readonly string _value;
            private readonly Color _labelColor;
            
            // Re-usable drawing resources
            private readonly Font _labelFont;
            private readonly Font _valueFont;
            private readonly SolidBrush _labelBrush;
            private readonly SolidBrush _valueBrush;

            public StatPanel(string label, string value, Color labelColor)
            {
                _label = label;
                _value = value;
                _labelColor = labelColor;
                
                _labelFont = new Font("Consolas", 9);
                _valueFont = new Font("Consolas", 9, FontStyle.Bold);
                _labelBrush = new SolidBrush(_labelColor);
                _valueBrush = new SolidBrush(Color.White);

                Dock = DockStyle.Fill;
                Margin = new Padding(2);
                BackColor = Color.FromArgb(20, 20, 20);
                DoubleBuffered = true; // Prevents flicker
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                // Use high-quality rendering for text
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                string labelText = _label + ": ";
                string valueText = _value;

                // Measure the size of both parts of the text
                SizeF labelSize = e.Graphics.MeasureString(labelText, _labelFont);
                SizeF valueSize = e.Graphics.MeasureString(valueText, _valueFont);

                // Calculate the total width and the starting X position to center the combined text
                float totalWidth = labelSize.Width + valueSize.Width;
                float startX = (ClientRectangle.Width - totalWidth) / 2;

                // Calculate the Y position to center the text vertically
                float startY = (ClientRectangle.Height - _labelFont.Height) / 2;

                // Draw the label part
                e.Graphics.DrawString(labelText, _labelFont, _labelBrush, new PointF(startX, startY));

                // Draw the value part immediately after the label
                e.Graphics.DrawString(valueText, _valueFont, _valueBrush, new PointF(startX + labelSize.Width, startY));
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    // Dispose of the GDI resources we created
                    _labelFont.Dispose();
                    _valueFont.Dispose();
                    _labelBrush.Dispose();
                    _valueBrush.Dispose();
                }
                base.Dispose(disposing);
            }
        }
        
        private void CreateModuleTabButtons()
        {
            var tabPanel = _controlFactory!.ModuleTabPanel;
            if (tabPanel == null) return;

            tabPanel.Controls.Clear();
            var tabs = new[] { "core", "hardpoints", "utility", "optional" };

            foreach (var tab in tabs)
            {
                var button = new Button
                {
                    Text = tab.ToUpper(),
                    Tag = tab,
                    Font = new Font("Consolas", 9),
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Padding = new Padding(8),
                    Margin = new Padding(0, 0, 1, 0),
                    FlatStyle = FlatStyle.Flat,
                };
                button.FlatAppearance.BorderSize = 0;
                button.Click += OnModuleTabClicked;
                tabPanel.Controls.Add(button);
            }
            UpdateTabStyles();
        }

        private void OnModuleTabClicked(object? sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is string tabName)
            {
                _activeModuleTab = tabName;
                UpdateTabStyles();
                UpdateModuleList();
            }
        }

        private void UpdateTabStyles()
        {
            var tabPanel = _controlFactory!.ModuleTabPanel;
            if (tabPanel == null) return;

            foreach (Control ctrl in tabPanel.Controls)
            {
                if (ctrl is Button button && button.Tag is string tabName)
                {
                    bool isActive = (tabName == _activeModuleTab);
                    button.BackColor = isActive ? _orangeColor : Color.Transparent;
                    button.ForeColor = isActive ? Color.Black : _darkOrangeColor;
                }
            }
        }

        private void UpdateModuleList()
        {
            var listView = _controlFactory?.ModulesListView;
            if (listView is null || _currentLoadout is null) return;

            listView.BeginUpdate();
            listView.Items.Clear();

            var modules = GetModulesForCurrentTab();

            foreach (var module in modules)
            {
                var item = new ListViewItem
                {
                    Tag = module.Slot, // Store the slot name, not the stale object
                    // Text is set via custom drawing, but we can set it for accessibility
                    Text = ItemNameService.TranslateModuleName(module.Item) ?? "Empty"
                };
                listView.Items.Add(item);
            }

            listView.EndUpdate();
        }

        private IEnumerable<ShipModule> GetModulesForCurrentTab()
        {
            if (_currentLoadout == null) return Enumerable.Empty<ShipModule>();

            return _activeModuleTab switch
            {
                "core" => _currentLoadout.Modules.Where(m => IsCoreModule(m.Slot)).OrderBy(m => GetSlotSortOrder(m.Slot)),
                "hardpoints" => _currentLoadout.Modules.Where(m => m.Slot.Contains("Hardpoint")).OrderBy(m => m.Slot),
                "utility" => _currentLoadout.Modules.Where(m => m.Slot.Contains("Utility")).OrderBy(m => m.Slot),
                "optional" => _currentLoadout.Modules.Where(m => m.Slot.Contains("Slot") || m.Slot.Contains("Military")).OrderBy(m => GetSlotSortOrder(m.Slot)).ThenBy(m => m.Slot),
                _ => Enumerable.Empty<ShipModule>()
            };
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