using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using EliteDataRelay.Models;
using EliteDataRelay.Services;
using EliteDataRelay.UI.Controls;
using System.IO;
using System.Threading.Tasks;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Displays recent journal events in a list similar to EDDiscovery's history view.
    /// </summary>
    public class HistoryTab : TabPage
    {
        private readonly JournalHistoryService _historyService;
        private readonly SafeListView _listView;
        private readonly TextBox _filterBox;
        private readonly Label _countLabel;
        private readonly ImageList _iconList;
        private readonly Dictionary<string, string> _iconKeyByEvent = new(StringComparer.OrdinalIgnoreCase);
        private string _defaultIconKey = "other";
        private List<JournalHistoryEntry> _cachedEntries = new();
        private int _renderScheduled;

        public HistoryTab(JournalHistoryService historyService, FontManager fontManager)
        {
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));

            Text = "History";
            Name = "History";
            Padding = new Padding(10);
            BackColor = Color.White;
            ForeColor = Color.Black;

            _iconList = BuildIconList();
            _listView = CreateListView(fontManager);
            _filterBox = CreateFilterBox(fontManager);
            _countLabel = CreateCountLabel(fontManager);

            var header = BuildHeader(fontManager);
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.FromArgb(10, 10, 10)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(_listView, 0, 1);

            Controls.Add(layout);

            _historyService.HistoryUpdated += OnHistoryUpdated;
            QueueRender();
            BeginLoadIconsAsync();
        }

        private Control BuildHeader(FontManager fontManager)
        {
            var title = new Label
            {
                Text = "Journal History",
                AutoSize = true,
                Font = fontManager.SegoeUIFontBold,
                ForeColor = Color.FromArgb(230, 120, 0),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 12, 0)
            };

            var refreshButton = new Button
            {
                Text = "Refresh",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 4, 10, 4),
                Margin = new Padding(6, 0, 0, 0),
                BackColor = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            refreshButton.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            refreshButton.Click += (_, _) => QueueRender();

            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 1,
                AutoSize = true,
                BackColor = Color.White
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            headerLayout.Controls.Add(title, 0, 0);
            headerLayout.Controls.Add(_filterBox, 1, 0);
            headerLayout.Controls.Add(_countLabel, 2, 0);
            headerLayout.Controls.Add(refreshButton, 3, 0);

            return headerLayout;
        }

        private SafeListView CreateListView(FontManager fontManager)
        {
            var list = new SafeListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                BackColor = Color.White,
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                SmallImageList = _iconList,
                UseCompatibleStateImageBehavior = false,
                Font = fontManager.SegoeUIFont
            };

            list.Columns.Add(string.Empty, 28);
            list.Columns.Add("Time (Local)", 140);
            list.Columns.Add("Event", 160);
            list.Columns.Add("Details", 600);

            typeof(ListView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(list, true);

            list.ItemActivate += (_, _) => ShowRawJson();

            return list;
        }

        private TextBox CreateFilterBox(FontManager fontManager)
        {
            var box = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "Filter by event, system, body or detail...",
                Font = fontManager.SegoeUIFont,
                Margin = new Padding(0, 0, 8, 0),
                BackColor = Color.White,
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };

            box.TextChanged += (_, _) => ApplyFilter();
            return box;
        }

        private Label CreateCountLabel(FontManager fontManager) => new()
        {
            AutoSize = true,
            Font = fontManager.SegoeUIFont,
            ForeColor = Color.FromArgb(60, 70, 86),
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 2, 8, 0),
            Text = "0 items"
        };

        private void QueueRender()
        {
            if (Interlocked.Exchange(ref _renderScheduled, 1) == 1) return;

            Task.Run(() =>
            {
                try
                {
                    var snapshot = _historyService.GetEntries(500).ToList();
                    if (IsDisposed || !_listView.IsHandleCreated) return;

                    BeginInvoke(new Action(() =>
                    {
                        _cachedEntries = snapshot;
                        ApplyFilter();
                    }));
                }
                catch
                {
                    // ignore background errors
                }
                finally
                {
                    Interlocked.Exchange(ref _renderScheduled, 0);
                }
            });
        }

        private void ApplyFilter()
        {
            if (_listView.IsDisposed) return;

            var filter = _filterBox.Text?.Trim() ?? string.Empty;
            var filtered = string.IsNullOrWhiteSpace(filter)
                ? _cachedEntries
                : _cachedEntries.Where(entry => MatchesFilter(entry, filter)).ToList();

            Render(filtered);
        }

        private static bool MatchesFilter(JournalHistoryEntry entry, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) return true;
            return entry.EventName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                   || entry.Summary.Contains(filter, StringComparison.OrdinalIgnoreCase)
                   || (!string.IsNullOrWhiteSpace(entry.StarSystem) && entry.StarSystem.Contains(filter, StringComparison.OrdinalIgnoreCase))
                   || (!string.IsNullOrWhiteSpace(entry.Body) && entry.Body.Contains(filter, StringComparison.OrdinalIgnoreCase))
                   || (!string.IsNullOrWhiteSpace(entry.Station) && entry.Station.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        private void Render(IReadOnlyList<JournalHistoryEntry> entries)
        {
            _listView.BeginUpdate();
            _listView.Items.Clear();

            foreach (var entry in entries)
            {
                var item = new ListViewItem
                {
                    ImageKey = GetIconKey(entry),
                    Tag = entry
                };

                item.SubItems.Add(entry.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                item.SubItems.Add(entry.EventName);
                item.SubItems.Add(entry.Summary);

                _listView.Items.Add(item);
            }

            _listView.EndUpdate();
            _countLabel.Text = $"Showing {entries.Count:N0} / {_cachedEntries.Count:N0}";
        }

        private void ShowRawJson()
        {
            if (_listView.SelectedItems.Count == 0) return;
            if (_listView.SelectedItems[0].Tag is not JournalHistoryEntry entry) return;

            using var dialog = new Form
            {
                Text = $"{entry.EventName} @ {entry.TimestampUtc.ToLocalTime():g}",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(700, 500),
                BackColor = Color.FromArgb(12, 12, 12),
                ForeColor = Color.White,
                Font = Font
            };

            var textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                BackColor = Color.FromArgb(10, 10, 10),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = entry.RawJson
            };

            dialog.Controls.Add(textBox);
            dialog.ShowDialog(this);
        }

        private ImageList BuildIconList()
        {
            var list = new ImageList
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = new Size(18, 18),
                TransparentColor = Color.Black
            };

            // Seed with lightweight fallbacks immediately to keep UI responsive.
            AddIcon(list, "travel", Color.FromArgb(255, 140, 0), "T");
            AddIcon(list, "station", Color.FromArgb(0, 180, 255), "S");
            AddIcon(list, "market", Color.FromArgb(0, 200, 120), "M");
            AddIcon(list, "exploration", Color.FromArgb(72, 160, 255), "E");
            AddIcon(list, "combat", Color.FromArgb(230, 60, 60), "C");
            AddIcon(list, "ship", Color.FromArgb(255, 219, 88), "H");
            AddIcon(list, "other", Color.FromArgb(120, 130, 146), "?");

            EnsureFallbackIcons(list);
            return list;
        }

        private void EnsureFallbackIcons(ImageList list)
        {
            if (!list.Images.ContainsKey("travel")) AddIcon(list, "travel", Color.FromArgb(255, 140, 0), "T");
            if (!list.Images.ContainsKey("station")) AddIcon(list, "station", Color.FromArgb(0, 180, 255), "S");
            if (!list.Images.ContainsKey("market")) AddIcon(list, "market", Color.FromArgb(0, 200, 120), "M");
            if (!list.Images.ContainsKey("exploration")) AddIcon(list, "exploration", Color.FromArgb(72, 160, 255), "E");
            if (!list.Images.ContainsKey("combat")) AddIcon(list, "combat", Color.FromArgb(230, 60, 60), "C");
            if (!list.Images.ContainsKey("ship")) AddIcon(list, "ship", Color.FromArgb(255, 219, 88), "H");
            if (!list.Images.ContainsKey("other")) AddIcon(list, "other", Color.FromArgb(120, 130, 146), "?");

            var firstKey = list.Images.Keys.Cast<string?>().FirstOrDefault(k => !string.IsNullOrWhiteSpace(k)) ?? "other";
            _defaultIconKey = list.Images.ContainsKey("Unknown")
                ? "Unknown"
                : (list.Images.ContainsKey("other") ? "other" : firstKey);
        }

        private void BeginLoadIconsAsync()
        {
            Task.Run(() =>
            {
                var loaded = new List<KeyValuePair<string, Image>>();
                loaded.AddRange(LoadIconsFromResources());
                loaded.AddRange(LoadIconsFromDisk());
                if (loaded.Count == 0) return;

                if (IsDisposed || !_listView.IsHandleCreated) return;
                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        foreach (var icon in loaded)
                        {
                            if (_iconList.Images.ContainsKey(icon.Key)) continue;
                            _iconList.Images.Add(icon.Key, icon.Value);
                            _iconKeyByEvent[icon.Key] = icon.Key;
                        }

                        EnsureFallbackIcons(_iconList);
                        ApplyFilter(); // re-render with new icons
                    }));
                }
                catch
                {
                    // ignore if handle disposed
                }
            });
        }

        private List<KeyValuePair<string, Image>> LoadIconsFromDisk()
        {
            var results = new List<KeyValuePair<string, Image>>();
            try
            {
                var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "History");
                if (!Directory.Exists(root))
                {
                    return results;
                }

                foreach (var file in Directory.EnumerateFiles(root, "*.png", SearchOption.AllDirectories))
                {
                    var key = Path.GetFileNameWithoutExtension(file);
                    if (string.IsNullOrWhiteSpace(key)) continue;

                    try
                    {
                        // Load into memory so we can dispose file handle immediately
                        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                        var image = Image.FromStream(fs);
                        results.Add(new KeyValuePair<string, Image>(key, (Image)image.Clone()));
                    }
                    catch
                    {
                        // Skip invalid images
                    }
                }
            }
            catch
            {
                // ignore
            }

            return results;
        }

        private IEnumerable<KeyValuePair<string, Image>> LoadIconsFromResources()
        {
            var results = new List<KeyValuePair<string, Image>>();
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                foreach (var resName in asm.GetManifestResourceNames())
                {
                    if (!resName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        !resName.Contains(".Images.History.", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var key = ExtractKeyFromResourceName(resName);
                    if (string.IsNullOrWhiteSpace(key)) continue;

                    try
                    {
                        using var stream = asm.GetManifestResourceStream(resName);
                        if (stream == null) continue;
                        var image = Image.FromStream(stream);
                        results.Add(new KeyValuePair<string, Image>(key, (Image)image.Clone()));
                    }
                    catch
                    {
                        // ignore bad resource
                    }
                }
            }
            catch
            {
                // ignore
            }

            return results;
        }

        private static string ExtractKeyFromResourceName(string resName)
        {
            if (string.IsNullOrWhiteSpace(resName)) return string.Empty;
            var parts = resName.Split('.');
            if (parts.Length < 2) return Path.GetFileNameWithoutExtension(resName);
            // last part is extension, second last is file name
            return parts[^2];
        }

        private static void AddIcon(ImageList list, string key, Color color, string glyph)
        {
            var bmp = new Bitmap(list.ImageSize.Width, list.ImageSize.Height);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            var rect = new Rectangle(1, 1, bmp.Width - 3, bmp.Height - 3);
            using var brush = new SolidBrush(color);
            using var pen = new Pen(Color.FromArgb(30, 30, 30), 1);
            g.FillEllipse(brush, rect);
            g.DrawEllipse(pen, rect);

            using var font = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Pixel);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var textBrush = new SolidBrush(Color.White);
            g.DrawString(glyph, font, textBrush, rect, sf);

            if (!list.Images.ContainsKey(key))
            {
                list.Images.Add(key, bmp);
            }
        }

        private string GetIconKey(JournalHistoryEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.EventName) && _iconList.Images.ContainsKey(entry.EventName))
            {
                return entry.EventName;
            }

            if (!string.IsNullOrWhiteSpace(entry.EventName) &&
                _iconKeyByEvent.TryGetValue(entry.EventName, out var mappedKey) &&
                _iconList.Images.ContainsKey(mappedKey))
            {
                return mappedKey;
            }

            if (!string.IsNullOrWhiteSpace(entry.Category) && _iconList.Images.ContainsKey(entry.Category))
            {
                return entry.Category;
            }

            return _defaultIconKey;
        }

        private void OnHistoryUpdated(object? sender, EventArgs e)
        {
            if (IsDisposed || !_listView.IsHandleCreated)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(QueueRender));
            }
            else
            {
                QueueRender();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _historyService.HistoryUpdated -= OnHistoryUpdated;
                _iconList.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
