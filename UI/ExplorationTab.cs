using EliteDataRelay.Models;
using EliteDataRelay.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public class ExplorationTab : TabPage
    {
        private DataGridView _bodiesGrid = null!;
        private Label _systemNameLabel = null!;
        private Label _systemStatsLabel = null!;
        private Panel _sessionPanel = null!;
        private Label _sessionLabel = null!;
        private Label _sessionStatsLabel = null!;
        private readonly ExplorationLogControl? _logControl;
    private readonly TabControl _subTabControl;
        private SystemExplorationData? _currentSystemData;
        private ExplorationSessionData _sessionData = new ExplorationSessionData();

        public ExplorationTab(ExplorationDatabaseService? database = null)
        {
            this.Text = "Exploration";
            this.Name = "Exploration";
            this.BackColor = Color.White;
            this.Padding = new Padding(0);

        // Create sub-tab control
        _subTabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F)
        };

        // Create "Current System" tab
        var currentSystemTab = new TabPage("Current System")
        {
            BackColor = Color.White,
            Padding = new Padding(0)
        };

        // Move all existing UI into the current system tab
            var currentSystemPanel = CreateCurrentSystemPanel();
        currentSystemTab.Controls.Add(currentSystemPanel);

        _subTabControl.TabPages.Add(currentSystemTab);

        // Create "Exploration Log" tab if database is available
        if (database != null)
        {
            var logTab = new TabPage("Exploration Log")
            {
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            _logControl = new ExplorationLogControl(database)
            {
                Dock = DockStyle.Fill // Ensure the control fills the tab page
            };
            logTab.Controls.Add(_logControl);

            // Refresh the log data when the tab becomes visible
            logTab.Enter += (s, e) => _logControl.Refresh();

            _subTabControl.TabPages.Add(logTab);
        }

        this.Controls.Add(_subTabControl);
        }

        private Panel CreateCurrentSystemPanel()
        {

            // Main container with proper margins
            var containerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            // Main layout - vertical stack
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); // System header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Bodies grid
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));  // Session stats

            // Add spacing between rows
            mainLayout.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;

            // === SYSTEM HEADER CARD ===
            var systemCard = CreateCard();
            systemCard.Padding = new Padding(20, 16, 20, 16);

            // System name - large, bold
            _systemNameLabel = new Label
            {
                Text = "No System Selected",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(0, 10)
            };

            // System stats - smaller, below name
            _systemStatsLabel = new Label
            {
                Text = "Start monitoring to see exploration data",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(0, 42)
            };

            systemCard.Controls.Add(_systemNameLabel);
            systemCard.Controls.Add(_systemStatsLabel);

            mainLayout.Controls.Add(systemCard, 0, 0);

            // Add margin below system card
            mainLayout.SetRow(systemCard, 0);
            systemCard.Margin = new Padding(0, 0, 0, 12);

            // === BODIES GRID CARD ===
            var gridCard = CreateCard();
            gridCard.Padding = new Padding(0);

            _bodiesGrid = CreateBodiesGrid();
            gridCard.Controls.Add(_bodiesGrid);

            mainLayout.Controls.Add(gridCard, 0, 1);
            gridCard.Margin = new Padding(0, 0, 0, 12);

            // === SESSION STATS CARD ===
            _sessionPanel = CreateCard();
            _sessionPanel.Padding = new Padding(20, 14, 20, 14);

            var sessionLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            sessionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
            sessionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

            _sessionLabel = new Label
            {
                Text = "SESSION STATS",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _sessionStatsLabel = new Label
            {
                Text = "0 systems explored",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            sessionLayout.Controls.Add(_sessionLabel, 0, 0);
            sessionLayout.Controls.Add(_sessionStatsLabel, 0, 1);

            _sessionPanel.Controls.Add(sessionLayout);
            mainLayout.Controls.Add(_sessionPanel, 0, 2);
            _sessionPanel.Margin = new Padding(0);

            containerPanel.Controls.Add(mainLayout);
            return containerPanel;
        }

        private Panel CreateCard()
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            // Add a subtle border to mimic a card
            card.Paint += (s, e) =>
            {
                var rect = card.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                using (var pen = new Pen(Color.FromArgb(226, 232, 240)))
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }
            };

            return card;
        }

        private DataGridView CreateBodiesGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(241, 245, 249),
                ColumnHeadersVisible = true,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 9F),
                RowTemplate = { Height = 40 },
                Margin = new Padding(0)
            };

            // Modern column header styling
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = grid.ColumnHeadersDefaultCellStyle.BackColor;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(16, 8, 16, 8);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersHeight = 44;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

            // Modern cell styling
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 245, 249);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            grid.DefaultCellStyle.Padding = new Padding(16, 6, 16, 6);
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

            // No alternating rows - cleaner look with just the horizontal lines
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.White;

            // Add columns with better proportions
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BodyName",
                HeaderText = "Body Name",
                FillWeight = 35
            });

            // Icon column for body type
            var iconColumn = new DataGridViewImageColumn
            {
                Name = "BodyIcon",
                HeaderText = "",
                Width = 30,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                ImageLayout = DataGridViewImageCellLayout.Zoom,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    NullValue = null,
                    Padding = new Padding(2)
                }
            };
            grid.Columns.Add(iconColumn);

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BodyType",
                HeaderText = "Type",
                FillWeight = 12
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Distance",
                HeaderText = "Distance",
                FillWeight = 13,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Landable",
                HeaderText = "Land",
                FillWeight = 7,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Signals",
                HeaderText = "Signals",
                FillWeight = 13
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Status",
                FillWeight = 12
            });

            return grid;
        }

        public void UpdateSystemData(SystemExplorationData systemData)
        {
            _currentSystemData = systemData;

            this.BeginInvoke(new MethodInvoker(() =>
            {
                // Update system header
                _systemNameLabel.Text = systemData.SystemName;
                var statsParts = new List<string>();
                // Display scanned count excluding barycentres and belt clusters
                int scannedDisplay = systemData.Bodies.Count(b =>
                    (b.BodyType?.IndexOf("bary", StringComparison.OrdinalIgnoreCase) ?? -1) < 0 &&
                    (b.BodyName?.IndexOf("belt cluster", StringComparison.OrdinalIgnoreCase) ?? -1) < 0 &&
                    (b.BodyName?.IndexOf(" ring", StringComparison.OrdinalIgnoreCase) ?? -1) < 0);
                if (systemData.TotalBodies > 0)
                {
                    int shown = Math.Min(scannedDisplay, systemData.TotalBodies);
                    statsParts.Add($"{shown}/{systemData.TotalBodies} bodies scanned");
                }
                else
                {
                    statsParts.Add($"{scannedDisplay} bodies scanned");
                }

                if (systemData.MappedBodies > 0)
                {
                    statsParts.Add($"{systemData.MappedBodies} mapped");
                }

                // Completion cues
                if (systemData.TotalBodies > 0 && systemData.ScannedBodies >= systemData.TotalBodies)
                {
                    statsParts.Add("All scanned");
                }

                // Determine mappable bodies using planet-class whitelist
                int mappable = systemData.Bodies.Count(b => MappabilityService.IsMappable(b));
                if (mappable > 0)
                {
                    int mapped = systemData.Bodies.Count(b => b.IsMapped && MappabilityService.IsMappable(b));
                    if (mapped >= mappable)
                    {
                        statsParts.Add("All mapped");
                    }
                }

                // Non-body signals summary
                int totalSignals = systemData.NonBodySignalsDetected > 0 ? systemData.NonBodySignalsDetected : (systemData.SystemSignals?.Count ?? 0);
                if (totalSignals > 0)
                {
                    statsParts.Add($"Signals: {totalSignals}");
                }

                // Codex bio entries
                int bioCodex = systemData.CodexBiologicalEntries?.Count ?? 0;
                if (bioCodex > 0)
                {
                    statsParts.Add($"Codex: {bioCodex} bio");
                }

                if (systemData.FSSProgress > 0 && systemData.FSSProgress < 100)
                {
                    statsParts.Add($"FSS: {systemData.FSSProgress:F1}%");
                }
                else if (systemData.FSSProgress >= 100)
                {
                    statsParts.Add("FSS: Complete");
                }

                _systemStatsLabel.Text = string.Join(" • ", statsParts);

                // Update bodies grid
                _bodiesGrid.SuspendLayout();
                _bodiesGrid.Rows.Clear();

                var rows = new List<DataGridViewRow>();
                foreach (var body in systemData.Bodies.OrderBy(b => b.DistanceFromArrival ?? double.MaxValue))
                {
                    var row = new DataGridViewRow();

                    // Generate icon for body type
                    var bodyIcon = BodyIconGenerator.GetIconForBodyType(body.BodyType);

                    // Body type with terraform indicator
                    string bodyType = body.BodyType;
                    if (!string.IsNullOrEmpty(body.TerraformState) && body.TerraformState != "Not Terraformable")
                    {
                        bodyType += " ⚡"; // Lightning bolt for terraformable
                    }

                    // Distance formatting
                    string distance = body.DistanceFromArrival.HasValue
                        ? $"{body.DistanceFromArrival.Value:N0}"
                        : "—";

                    // Landable indicator
                    string landable = body.Landable.HasValue
                        ? (body.Landable.Value ? "✓" : "—")
                        : "—";

                    // Signals summary
                    string signals = "—";
                    if (body.BiologicalSignals.Any())
                    {
                        var bioCount = body.BiologicalSignals.Count;
                        signals = $"🧬 {bioCount} bio";
                    }
                    else if (body.Signals.Any())
                    {
                        var totalSigCount = body.Signals.Sum(s => s.Count);
                        signals = totalSigCount.ToString();
                    }

                    // Status with icon
                    string status = "Scanned";
                    if (body.FirstFootfall)
                    {
                        status = "👣 First Footfall!";
                    }
                    else if (!body.WasDiscovered)
                    {
                        status = "⭐ First!";
                    }
                    else if (body.IsMapped && !body.WasMapped)
                    {
                        status = "🗺️ Mapped";
                    }
                    else if (body.IsMapped)
                    {
                        status = "Mapped";
                    }
                    else if (body.WasMapped)
                    {
                        status = "Known";
                    }

                    row.CreateCells(_bodiesGrid, body.BodyName, bodyIcon, bodyType, distance, landable, signals, status);

                    // Subtle color coding
                    if (body.FirstFootfall)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 237, 213); // Very light orange/gold
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(120, 53, 15); // Dark orange text
                    }
                    else if (!body.WasDiscovered)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(240, 253, 244); // Very light green
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(22, 101, 52); // Dark green text
                    }
                    else if (body.IsMapped && !body.WasMapped)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(240, 249, 255); // Very light blue
                    }

                    rows.Add(row);
                }

                _bodiesGrid.Rows.AddRange(rows.ToArray());
                _bodiesGrid.ClearSelection();
                _bodiesGrid.ResumeLayout();
            }));
        }

        public void UpdateSessionData(ExplorationSessionData sessionData)
        {
            _sessionData = sessionData;

            this.BeginInvoke(new MethodInvoker(() =>
            {
                var parts = new List<string>();

                parts.Add($"{sessionData.SystemsVisited} systems");
                parts.Add($"{sessionData.TotalScans} scans");

                if (sessionData.TotalMapped > 0)
                {
                    parts.Add($"{sessionData.TotalMapped} mapped");
                }

                if (sessionData.FirstDiscoveries > 0)
                {
                    parts.Add($"⭐ {sessionData.FirstDiscoveries} discoveries");
                }

                if (sessionData.FirstFootfalls > 0)
                {
                    parts.Add($"👣 {sessionData.FirstFootfalls} first footfalls");
                }

                if (sessionData.SoldValue > 0)
                {
                    parts.Add($"💰 {sessionData.SoldValue:N0} CR");
                }
                else if (sessionData.EstimatedValue > 0)
                {
                    parts.Add($"~{sessionData.EstimatedValue:N0} CR");
                }

                _sessionStatsLabel.Text = string.Join(" • ", parts);
            }));
        }

        public void ClearSystemData()
        {
            _currentSystemData = null;

            this.BeginInvoke(new MethodInvoker(() =>
            {
                _systemNameLabel.Text = "No System Selected";
                _systemStatsLabel.Text = "Start monitoring to see exploration data";
                _bodiesGrid.Rows.Clear();
            }));
        }

        public void RefreshLog()
        {
            _logControl?.Refresh();
        }
    }
}
