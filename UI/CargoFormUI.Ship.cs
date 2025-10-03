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
        private ShipModule? _selectedModule;

        // Elite Orange color scheme
        private readonly Color _orangeColor = Color.FromArgb(255, 102, 0);
        private readonly Color _darkOrangeColor = Color.FromArgb(153, 61, 0);
        private readonly Color _lightOrangeColor = Color.FromArgb(255, 153, 68);

        private void InitializeShipTab()
        {
            if (_controlFactory?.ShipWireframePictureBox != null)
            {
                _shipWireframeDrawer = new ShipWireframeDrawer(_controlFactory.ShipWireframePictureBox);
                _shipWireframeDrawer.HardpointClicked += OnHardpointClicked;
            }
            CreateModuleTabButtons();
        }

        public void UpdateShipStatus(StatusFile status)
        {
            if (_controlFactory == null) return;

            // This method can be used to update live stats on the ship panel in the future.
            // For now, most data comes from the Loadout event.
        }

        public void UpdateShipLoadout(ShipLoadout loadout)
        {
            if (_controlFactory == null) return;
            _currentLoadout = loadout;

            UpdateShipStatsPanel(loadout);
            UpdateModuleList();
            _controlFactory.ShipWireframePictureBox.Invalidate(); // Force a repaint
        }

        private void UpdateShipStatsPanel(ShipLoadout loadout)
        {
            var statsPanel = _controlFactory.ShipStatsPanel;
            if (statsPanel == null) return;

            statsPanel.SuspendLayout();
            statsPanel.Controls.Clear();

            // Calculate total power usage
            double totalPowerDraw = loadout.Modules
                .Where(m => m.Engineering?.Modifiers != null)
                .SelectMany(m => m.Engineering.Modifiers)
                .Where(mod => mod.Label.Equals("PowerDraw", StringComparison.OrdinalIgnoreCase))
                .Sum(mod => mod.Value);

            var stats = new[]
            {
                ("MASS", $"{loadout.UnladenMass:N1}T"),
                ("SHIELDS", "N/A"), // This info isn't in ShipLoadout model
                ("ARMOR", "N/A"), // This info isn't in ShipLoadout model
                ("CARGO", $"{loadout.CargoCapacity}T"),
                ("JUMP", $"{loadout.MaxJumpRange:F2} LY"),
                ("SPEED", "N/A"), // This info isn't in ShipLoadout model
                ("BOOST", "N/A"), // This info isn't in ShipLoadout model
                ("POWER", $"{totalPowerDraw:F2} MW")
            };

            for (int i = 0; i < stats.Length; i++)
            {
                var statPanel = CreateStatItem(stats[i].Item1, stats[i].Item2);
                statsPanel.Controls.Add(statPanel, i % 4, i / 4);
            }

            statsPanel.ResumeLayout();
        }

        private Panel CreateStatItem(string label, string value)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            var lbl = new Label { Text = label, Dock = DockStyle.Top, Font = new Font("Consolas", 8), ForeColor = _darkOrangeColor };
            var val = new Label { Text = value, Dock = DockStyle.Fill, Font = new Font("Consolas", 12, FontStyle.Bold), ForeColor = _lightOrangeColor, TextAlign = ContentAlignment.MiddleCenter };
            panel.Controls.Add(val);
            panel.Controls.Add(lbl);
            return panel;
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

        private void OnHardpointClicked(object? sender, int index)
        {
            if (_currentLoadout == null) return;

            var hardpoints = _currentLoadout.Modules.Where(m => m.Slot.Contains("Hardpoint")).OrderBy(m => m.Slot).ToList();
            if (index < hardpoints.Count)
            {
                _selectedModule = hardpoints[index];
                _activeModuleTab = "hardpoints";
                UpdateTabStyles();
                UpdateModuleList();

                // Also select the item in the list view for visual feedback
                var listView = _controlFactory!.ModulesListView;
                if (listView != null)
                {
                    foreach (ListViewItem item in listView.Items)
                    {
                        if (item.Tag == _selectedModule) { item.Selected = true; listView.Focus(); break; }
                    }
                }
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
            var listView = _controlFactory!.ModulesListView;
            if (listView == null || _currentLoadout == null) return;

            listView.BeginUpdate();
            listView.Items.Clear();

            var modules = GetModulesForCurrentTab();

            foreach (var module in modules)
            {
                var item = new ListViewItem
                {
                    Tag = module,
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