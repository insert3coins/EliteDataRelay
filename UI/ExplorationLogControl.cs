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
        private readonly Button _loadMoreButton;
        private const int PageSize = 500;
        private bool _isDataLoaded = false;
        private readonly System.Collections.Generic.List<SystemExplorationData> _systemsData = new();
        private System.Collections.Generic.List<ScannedBody> _currentBodies = new();

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
                RowCount = 3,
                ColumnCount = 1
            };
            systemsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            systemsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            systemsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

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

            // Load More button footer
            var systemsFooter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(20, 6, 20, 6)
            };

            _loadMoreButton = new Button
            {
                Text = "Load More",
                AutoSize = false,
                Height = 30,
                Dock = DockStyle.Right,
                Width = 120,
                Enabled = false
            };
            _loadMoreButton.Click += (s, e) => LoadMoreSystems();
            systemsFooter.Controls.Add(_loadMoreButton);

            systemsLayout.Controls.Add(systemsHeader, 0, 0);
            systemsLayout.Controls.Add(_systemsGrid, 0, 1);
            systemsLayout.Controls.Add(systemsFooter, 0, 2);
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
            detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90)); // Increased from 70 to accommodate longer FSS text
            detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var detailHeader = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 12, 20, 12)
            };

            _selectedSystemLabel = new Label
            {
                Text = "Select a system to view details",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold), // Reduced from 12F to 10F
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = false, // Changed to false to allow wrapping
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                MaximumSize = new Size(0, 66) // Allow up to 3 lines of text
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
                Margin = new Padding(0),
                VirtualMode = true
            };

            // Enable double buffering via reflection to reduce flicker
            try
            {
                typeof(Control).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.SetProperty |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic,
                    null,
                    grid,
                    new object[] { true });
            }
            catch { /* best-effort */ }

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

            var bodiesColumn = new DataGridViewTextBoxColumn
            {
                Name = "Bodies",
                HeaderText = "Bodies",
                FillWeight = 20
            };
            bodiesColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns.Add(bodiesColumn);

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



            grid.CellValueNeeded += OnSystemsCellValueNeeded;
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
                Margin = new Padding(0),
                VirtualMode = true
            };

            // Enable double buffering via reflection to reduce flicker
            try
            {
                typeof(Control).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.SetProperty |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic,
                    null,
                    grid,
                    new object[] { true });
            }
            catch { /* best-effort */ }

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
                FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Status",
                FillWeight = 23
            });

            // Hook virtual mode providers for bodies grid
            grid.CellValueNeeded += OnBodiesCellValueNeeded;
            grid.CellFormatting += OnBodiesCellFormatting;
            return grid;
        }

        public void LoadSystemsList()
        {
            try
            {
                var systems = _database.GetVisitedSystems(PageSize, 0);

                System.Diagnostics.Debug.WriteLine($"[ExplorationLogControl] Found {systems.Count} systems in database");

                _systemsData.Clear();
                _systemsData.AddRange(systems);
                _systemsGrid.SuspendLayout();
                _systemsGrid.RowCount = _systemsData.Count;

                if (systems.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[ExplorationLogControl] No systems found - database may be empty");
                    _loadMoreButton.Enabled = false;
                    _loadMoreButton.Text = "Load More";
                    _systemsGrid.ResumeLayout();
                    return;
                }

                _systemsGrid.ClearSelection();
                _systemsGrid.ResumeLayout();

                // Enable/disable Load More based on whether we likely have more rows
                _loadMoreButton.Enabled = systems.Count >= PageSize;
                _loadMoreButton.Text = _loadMoreButton.Enabled ? "Load More" : "No More";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExplorationLogControl] Error loading systems: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ExplorationLogControl] Stack trace: {ex.StackTrace}");
            }
        }

        private void LoadMoreSystems()
        {
            try
            {
                var alreadyLoaded = _systemsData.Count;
                var systems = _database.GetVisitedSystems(PageSize, alreadyLoaded);

                System.Diagnostics.Debug.WriteLine($"[ExplorationLogControl] LoadMore - offset={alreadyLoaded}, fetched={systems.Count}");

                if (systems.Count == 0)
                {
                    _loadMoreButton.Enabled = false;
                    _loadMoreButton.Text = "No More";
                    return;
                }

                _systemsGrid.SuspendLayout();
                _systemsData.AddRange(systems);
                _systemsGrid.RowCount = _systemsData.Count;
                _systemsGrid.ResumeLayout();

                if (systems.Count < PageSize)
                {
                    _loadMoreButton.Enabled = false;
                    _loadMoreButton.Text = "No More";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExplorationLogControl] Error in LoadMoreSystems: {ex.Message}");
            }
        }



        private void OnSystemSelected(object? sender, EventArgs e)
        {
            // Prefer SelectedRows, but fall back to CurrentCell for VirtualMode reliability
            int selectedIndex = -1;
            if (_systemsGrid.SelectedRows.Count > 0)
            {
                selectedIndex = _systemsGrid.SelectedRows[0].Index;
            }
            else if (_systemsGrid.CurrentCell != null)
            {
                selectedIndex = _systemsGrid.CurrentCell.RowIndex;
            }

            if (selectedIndex < 0 || selectedIndex >= _systemsData.Count)
            {
                _selectedSystemLabel.Text = "Select a system to view details";
                _currentBodies = new System.Collections.Generic.List<ScannedBody>();
                _bodiesGrid.RowCount = 0;
                return;
            }

            var systemAddressNullable = _systemsData[selectedIndex].SystemAddress;
            if (!systemAddressNullable.HasValue) return;
            LoadSystemDetails(systemAddressNullable.Value);
        }

        private void LoadSystemDetails(long systemAddress)
        {
            try
            {
                var system = _database.LoadSystem(systemAddress);
                if (system == null) return;

                // Build system details string with FSS status
                var detailsParts = new List<string>();

                // FSS status
                if (system.FSSProgress >= 100 && system.TotalBodies > 0)
                {
                    detailsParts.Add($"FSS: Complete ({system.TotalBodies} bodies detected)");
                }
                else if (system.FSSProgress > 0 && system.FSSProgress < 100)
                {
                    int detectedBodies = system.TotalBodies > 0
                        ? (int)Math.Round(system.TotalBodies * (system.FSSProgress / 100.0))
                        : 0;
                    detailsParts.Add($"FSS: {system.FSSProgress:F1}% ({detectedBodies} detected)");
                }

                // Scanned and mapped counts
                detailsParts.Add($"{system.ScannedBodies} scanned");
                if (system.MappedBodies > 0)
                {
                    detailsParts.Add($"{system.MappedBodies} mapped");
                }

                _selectedSystemLabel.Text = $"{system.SystemName} — {string.Join(" • ", detailsParts)}";

                // Virtual mode population: update backing list + row count, then return
                _currentBodies = system.Bodies
                    .OrderBy(b => b.DistanceFromArrival ?? double.MaxValue)
                    .ToList();
                _bodiesGrid.RowCount = _currentBodies.Count;
                _bodiesGrid.ClearSelection();
                _bodiesGrid.Invalidate();
                return;


            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExplorationLogControl] Error loading system details: {ex.Message}");
            }
        }

        private void OnSystemsCellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _systemsData.Count) return;
            var system = _systemsData[e.RowIndex];
            var columnName = _systemsGrid.Columns[e.ColumnIndex].Name;
            switch (columnName)
            {
                case "SystemName":
                    e.Value = system.SystemName ?? string.Empty;
                    break;
                case "Bodies":
                    var bodiesText = $"{system.ScannedBodies}";
                    if (system.MappedBodies > 0) bodiesText += $" ({system.MappedBodies} mapped)";
                    e.Value = bodiesText;
                    break;
                case "LastVisited":
                    e.Value = GetTimeAgo(system.LastVisited);
                    break;
                case "SystemAddress":
                    e.Value = system.SystemAddress.ToString();
                    break;
            }
        }

        private void OnBodiesCellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _currentBodies.Count) return;
            if (e.ColumnIndex < 0) return;
            var body = _currentBodies[e.RowIndex];
            var col = _bodiesGrid.Columns[e.ColumnIndex].Name;
            switch (col)
            {
                case "BodyName":
                    e.Value = body.BodyName;
                    break;
                case "BodyIcon":
                    e.Value = BodyIconGenerator.GetIconForBodyType(body.BodyType);
                    break;
                case "BodyType":
                    var bodyType = body.BodyType;
                    if (!string.IsNullOrEmpty(body.TerraformState) && body.TerraformState != "Not Terraformable")
                        bodyType += " ?";
                    e.Value = bodyType;
                    break;
                case "Distance":
                    e.Value = body.DistanceFromArrival.HasValue ? $"{body.DistanceFromArrival.Value:N0}" : "-";
                    break;
                case "Status":
                    string status = body.IsMapped ? "??? Mapped" : "Scanned";
                    if (body.FirstFootfall) status = "?? First Footfall!";
                    else if (!body.WasDiscovered) status = "? First Discovery";
                    e.Value = status;
                    break;
            }
        }

        private void OnBodiesCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _currentBodies.Count) return;
            var body = _currentBodies[e.RowIndex];
            if (body.FirstFootfall)
            {
                e.CellStyle.BackColor = Color.FromArgb(255, 237, 213);
                e.CellStyle.ForeColor = Color.FromArgb(120, 53, 15);
            }
            else if (!body.WasDiscovered)
            {
                e.CellStyle.BackColor = Color.FromArgb(240, 253, 244);
                e.CellStyle.ForeColor = Color.FromArgb(22, 101, 52);
            }
            else if (body.IsMapped && !body.WasMapped)
            {
                e.CellStyle.BackColor = Color.FromArgb(240, 249, 255);
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
            // Ensure the date from the database is treated as UTC and converted to local before comparison.
            var localDate = date.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(date, DateTimeKind.Utc).ToLocalTime() : date.ToLocalTime();
            var span = DateTime.Now - localDate;

            System.Diagnostics.Debug.WriteLine($"[ExplorationLogControl] GetTimeAgo - Date (UTC): {date:o}, Date (Local): {localDate:o}, Now: {DateTime.Now:o}, Span: {span.TotalMinutes:F2} minutes");

            if (span.TotalMinutes < 1)
                return "Just now";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours}h ago";
            // Show days for longer periods so it goes beyond 1 day clearly
            if (span.TotalDays < 60)
                return $"{(int)span.TotalDays}d ago";
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
