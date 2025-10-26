using EliteDataRelay.Models;
using EliteDataRelay.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Control that displays historical exploration data from the database.
    /// </summary>
    public class ExplorationLogControl : UserControl
    {
        private readonly ExplorationDatabaseService _database;
        private readonly DataGridView _systemsGrid;
        private readonly DataGridView _bodiesGrid;
        private readonly Label _totalStatsLabel;
        private readonly Label _selectedSystemLabel;
        private readonly Panel _detailPanel;
        private bool _isDataLoaded = false;

        public ExplorationLogControl(ExplorationDatabaseService database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));

            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Padding = new Padding(16);

            // Main layout - 2 columns
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40)); // Systems list
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60)); // System details
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));      // Main content
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));      // Stats footer

            // === LEFT PANEL - Systems List ===
            var systemsCard = CreateCard();
            systemsCard.Padding = new Padding(0);
            systemsCard.Margin = new Padding(0, 0, 8, 8);

            var systemsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            systemsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            systemsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var systemsHeader = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 0, 20, 0)
            };

            var systemsHeaderLabel = new Label
            {
                Text = "VISITED SYSTEMS",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            systemsHeader.Controls.Add(systemsHeaderLabel);

            _systemsGrid = CreateSystemsGrid();

            systemsLayout.Controls.Add(systemsHeader, 0, 0);
            systemsLayout.Controls.Add(_systemsGrid, 0, 1);
            systemsCard.Controls.Add(systemsLayout);

            mainLayout.Controls.Add(systemsCard, 0, 0);

            // === RIGHT PANEL - System Details ===
            _detailPanel = CreateCard();
            _detailPanel.Padding = new Padding(0);
            _detailPanel.Margin = new Padding(8, 0, 0, 8);

            var detailLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var detailHeader = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 16, 20, 16)
            };

            _selectedSystemLabel = new Label
            {
                Text = "Select a system to view details",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true
            };
            detailHeader.Controls.Add(_selectedSystemLabel);

            _bodiesGrid = CreateBodiesGrid();

            detailLayout.Controls.Add(detailHeader, 0, 0);
            detailLayout.Controls.Add(_bodiesGrid, 0, 1);
            _detailPanel.Controls.Add(detailLayout);

            mainLayout.Controls.Add(_detailPanel, 1, 0);

            // === STATS FOOTER (spans both columns) ===
            var statsCard = CreateCard();
            statsCard.Padding = new Padding(20, 14, 20, 14);
            statsCard.Margin = new Padding(0);

            _totalStatsLabel = new Label
            {
                Text = "Loading statistics...",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            statsCard.Controls.Add(_totalStatsLabel);

            mainLayout.Controls.Add(statsCard, 0, 1);
            mainLayout.SetColumnSpan(statsCard, 2);

            this.Controls.Add(mainLayout);

            this.VisibleChanged += OnVisibleChanged;
        }

        private void OnVisibleChanged(object? sender, EventArgs e)
        {
            if (this.Visible && !_isDataLoaded)
            {
                try
                {
                    LoadSystemsList();
                    UpdateTotalStats();
                    _isDataLoaded = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExplorationLogControl] Error during initial data load: {ex.Message}");
                }
            }
        }

        private Panel CreateCard()
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0)
            };

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

        private DataGridView CreateSystemsGrid()
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
                RowTemplate = { Height = 50 },
                Margin = new Padding(0)
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = grid.ColumnHeadersDefaultCellStyle.BackColor;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(16, 8, 16, 8);
            grid.ColumnHeadersHeight = 44;

            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 245, 249);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            grid.DefaultCellStyle.Padding = new Padding(16, 6, 16, 6);

            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.White;

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SystemName",
                HeaderText = "System Name",
                FillWeight = 60
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Bodies",
                HeaderText = "Bodies",
                FillWeight = 20
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "LastVisited",
                HeaderText = "Last Visit",
                FillWeight = 20
            });

            // Hidden column for SystemAddress
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SystemAddress",
                Visible = false
            });

            grid.Columns["Bodies"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            grid.SelectionChanged += OnSystemSelected;

            return grid;
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

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = grid.ColumnHeadersDefaultCellStyle.BackColor;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(16, 8, 16, 8);
            grid.ColumnHeadersHeight = 44;

            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 245, 249);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            grid.DefaultCellStyle.Padding = new Padding(16, 6, 16, 6);

            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.White;

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "BodyName", HeaderText = "Body Name", FillWeight = 40 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "BodyType", HeaderText = "Type", FillWeight = 25 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Distance", HeaderText = "Distance (Ls)", FillWeight = 15 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 20 });

            grid.Columns["Distance"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            return grid;
        }

        public void LoadSystemsList()
        {
            try
            {
                var systems = _database.GetVisitedSystems(100);

                System.Diagnostics.Debug.WriteLine($"[ExplorationLogControl] Found {systems.Count} systems in database");

                _systemsGrid.SuspendLayout();
                _systemsGrid.Rows.Clear();

                if (systems.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[ExplorationLogControl] No systems found - database may be empty");
                    _systemsGrid.ResumeLayout();
                    return;
                }

                var rows = new List<DataGridViewRow>();
                foreach (var system in systems)
                {
                    var row = new DataGridViewRow();

                    var bodiesText = $"{system.ScannedBodies}";
                    if (system.MappedBodies > 0)
                    {
                        bodiesText += $" ({system.MappedBodies} mapped)";
                    }

                    var timeAgo = GetTimeAgo(system.LastVisited);

                    row.CreateCells(_systemsGrid, system.SystemName, bodiesText, timeAgo, system.SystemAddress.ToString());
                    rows.Add(row);
                }

                System.Diagnostics.Debug.WriteLine($"[ExplorationLogControl] Created {rows.Count} rows for grid");

                _systemsGrid.Rows.AddRange(rows.ToArray());
                _systemsGrid.ClearSelection();
                _systemsGrid.ResumeLayout();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExplorationLogControl] Error loading systems: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ExplorationLogControl] Stack trace: {ex.StackTrace}");
            }
        }

        private void OnSystemSelected(object? sender, EventArgs e)
        {
            if (_systemsGrid.SelectedRows.Count == 0)
            {
                _selectedSystemLabel.Text = "Select a system to view details";
                _bodiesGrid.Rows.Clear();
                return;
            }

            var selectedRow = _systemsGrid.SelectedRows[0];
            var systemAddressStr = selectedRow.Cells["SystemAddress"].Value?.ToString();

            if (string.IsNullOrEmpty(systemAddressStr) || !long.TryParse(systemAddressStr, out var systemAddress))
                return;

            LoadSystemDetails(systemAddress);
        }

        private void LoadSystemDetails(long systemAddress)
        {
            try
            {
                var system = _database.LoadSystem(systemAddress);
                if (system == null) return;

                _selectedSystemLabel.Text = $"{system.SystemName} — {system.ScannedBodies} bodies scanned, {system.MappedBodies} mapped";

                _bodiesGrid.SuspendLayout();
                _bodiesGrid.Rows.Clear();

                var rows = new List<DataGridViewRow>();
                foreach (var body in system.Bodies.OrderBy(b => b.DistanceFromArrival ?? double.MaxValue))
                {
                    var row = new DataGridViewRow();

                    string bodyType = body.BodyType;
                    if (!string.IsNullOrEmpty(body.TerraformState) && body.TerraformState != "Not Terraformable")
                    {
                        bodyType += " ⚡";
                    }

                    string distance = body.DistanceFromArrival.HasValue
                        ? $"{body.DistanceFromArrival.Value:N0}"
                        : "—";

                    string status = body.IsMapped ? "🗺️ Mapped" : "Scanned";
                    if (body.FirstFootfall)
                    {
                        status = "👣 First Footfall!";
                    }
                    else if (!body.WasDiscovered)
                    {
                        status = "⭐ First Discovery";
                    }

                    row.CreateCells(_bodiesGrid, body.BodyName, bodyType, distance, status);

                    if (body.FirstFootfall)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 237, 213); // Very light orange/gold
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(120, 53, 15); // Dark orange text
                    }
                    else if (!body.WasDiscovered)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(240, 253, 244);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(22, 101, 52);
                    }
                    else if (body.IsMapped && !body.WasMapped)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(240, 249, 255);
                    }

                    rows.Add(row);
                }

                _bodiesGrid.Rows.AddRange(rows.ToArray());
                _bodiesGrid.ClearSelection();
                _bodiesGrid.ResumeLayout();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExplorationLogControl] Error loading system details: {ex.Message}");
            }
        }

        private void UpdateTotalStats()
        {
            try
            {
                var (totalSystems, totalBodies, totalMapped) = _database.GetTotalStatistics();

                if (totalSystems == 0)
                {
                    _totalStatsLabel.Text = "EXPLORATION HISTORY: No data yet - start monitoring and scan some systems!";
                    return;
                }

                var parts = new List<string>
                {
                    $"{totalSystems} systems explored",
                    $"{totalBodies} bodies scanned"
                };

                if (totalMapped > 0)
                {
                    parts.Add($"{totalMapped} bodies mapped");
                }

                _totalStatsLabel.Text = $"EXPLORATION HISTORY: {string.Join(" • ", parts)}";
            }
            catch (Exception ex)
            {
                _totalStatsLabel.Text = "Unable to load statistics - check debug output";
                System.Diagnostics.Debug.WriteLine($"[ExplorationLogControl] Error loading stats: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ExplorationLogControl] Stack trace: {ex.StackTrace}");
            }
        }

        private string GetTimeAgo(DateTime date)
        {
            var span = DateTime.Now - date;

            System.Diagnostics.Debug.WriteLine($"[ExplorationLogControl] GetTimeAgo - Date: {date:o}, Now: {DateTime.Now:o}, Span: {span.TotalMinutes:F2} minutes");

            if (span.TotalMinutes < 1)
                return "Just now";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7)
                return $"{(int)span.TotalDays}d ago";
            if (span.TotalDays < 30)
                return $"{(int)(span.TotalDays / 7)}w ago";
            if (span.TotalDays < 365)
                return $"{(int)(span.TotalDays / 30)}mo ago";

            return $"{(int)(span.TotalDays / 365)}y ago";
        }

        public new void Refresh()
        {
            if (this.Visible)
            {
                LoadSystemsList();
                UpdateTotalStats();
            }
            _isDataLoaded = false; // Force a reload next time it becomes visible
        }
    }
}