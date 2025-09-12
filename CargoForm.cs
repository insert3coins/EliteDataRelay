using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media; // Added for SoundPlayer
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows.Forms;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace EliteCargoMonitor
{
    public partial class CargoForm : Form
    {
        private readonly SoundPlayer _startSound;
        private readonly SoundPlayer _stopSound;
        private readonly Font _verdanaFont;

        /* -----------------------------------------------------------------
         *   Data‑models used by the JSON serializer
         * ----------------------------------------------------------------- */
        public record CargoSnapshot([property: JsonPropertyName("Inventory")] List<CargoItem> Inventory);
        public record CargoItem(
            [property: JsonPropertyName("Name")] string Name,
            [property: JsonPropertyName("Count")] int Count,
            [property: JsonPropertyName("Name_Localised")] string Localised,
            [property: JsonPropertyName("Stolen")] int Stolen);

        /* -----------------------------------------------------------------
         *   UI controls
         * ----------------------------------------------------------------- */
        private readonly TextBox _textBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new System.Drawing.Font("Consolas", 10),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Margin = Padding.Empty
        };

        private readonly Button _exitBtn = new Button { Text = "Exit", Height = 30 };
        private readonly Button _startBtn = new Button { Text = "Start", Height = 30 };
        private readonly Button _stopBtn = new Button { Text = "Stop", Height = 30, Enabled = false };
        private readonly Button _aboutBtn = new Button { Text = "About", Height = 30 };

        /* -----------------------------------------------------------------
         *   Paths & state helpers
         * ----------------------------------------------------------------- */
        private static readonly string CargoPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Saved Games",
            "Frontier Developments",
            "Elite Dangerous",
            "Cargo.json");

        private FileSystemWatcher? _watcher;
        private readonly WinFormsTimer _debounceTimer = new WinFormsTimer();
        private const int DebounceMs = 500;          // 500 ms debounce window
        private string? _lastInventoryHash;

        public CargoForm()
        {
            // Initialize font
            var fontStream = new MemoryStream(Properties.Resources.VerdanaFont);
            _verdanaFont = new System.Drawing.Font("Verdana", 10);

            // Initialize sound players
            _startSound = new SoundPlayer(Properties.Resources.Start);
            _stopSound = new SoundPlayer(Properties.Resources.Stop);

            // Basic window
            Text = $"Cargo Monitor – Stopped: {CargoPath}";
            Width = 800;
            Height = 600;
            Padding = Padding.Empty;        // remove any form padding

            /* --- Icon setup --- */
            this.Icon = new System.Drawing.Icon(new MemoryStream(Properties.Resources.AppIcon));

            /* ----- button panel ------------------------------------------------ */
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 35,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };

            // Order: Start | Stop | About | Exit
            buttonPanel.Controls.Add(_startBtn);
            buttonPanel.Controls.Add(_stopBtn);
            buttonPanel.Controls.Add(_aboutBtn);   // <--- New button
            buttonPanel.Controls.Add(_exitBtn);

            Controls.Add(_textBox);
            Controls.Add(buttonPanel);

            // Set font for text box
            _textBox.Font = _verdanaFont;

            /* -----------------------------------------------------------------
             *   Welcome text – show it immediately on startup
             * ----------------------------------------------------------------- */
            string welcome = $"Welcome to Cargo watcher{Environment.NewLine}Please press Start to start watching cargo{Environment.NewLine}Press Stop to stop watching{Environment.NewLine}Exit to shutdown program.";
            _textBox.AppendText(welcome + Environment.NewLine);

            /* ----- event hookups ------------------------------------------------ */
            Load += CargoForm_Load;
            FormClosing += CargoForm_FormClosing;

            // Button clicks
            _exitBtn.Click += (_, _) => Close();
            _startBtn.Click += StartMonitoring;
            _stopBtn.Click += StopMonitoringClick;   // renamed click‑handler
            _aboutBtn.Click += About_Click;          // new handler
        }

        /* -----------------------------------------------------------------
         *   Form events
         * ----------------------------------------------------------------- */
        private void CargoForm_Load(object? sender, EventArgs e)
        {
            if (!File.Exists(CargoPath))
            {
                MessageBox.Show(
                    $"Cargo.json not found.\nMake sure Elite Dangerous is running\nand the file is at:\n{CargoPath}",
                    "File not found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
        }

        private void CargoForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            StopMonitoringInternal();
            _startSound.Dispose();
            _stopSound.Dispose();
            _verdanaFont.Dispose();
        }

        /* -----------------------------------------------------------------
         *   Start / Stop helpers
         * ----------------------------------------------------------------- */
        private void StartMonitoring(object? sender, EventArgs e)
        {
            _startSound.Play();  // Play start sound
            _startBtn.Enabled = false;
            _stopBtn.Enabled = true;
            Text = $"Cargo Monitor – Watching: {CargoPath}";

            ProcessFullFile();          // initial snapshot
            InitializeWatcher();        // start watching
            StartPolling();             // start polling fallback
        }

        private void StopMonitoringClick(object? sender, EventArgs e) => StopMonitoringInternal();

        private void StopMonitoringInternal()
        {
            _stopSound.Play();  // Play stop sound
            _startBtn.Enabled = true;
            _stopBtn.Enabled = false;
            Text = $"Cargo Monitor – Stopped: {CargoPath}";

            _watcher?.Dispose();
            _debounceTimer.Stop();
            _pollTimer.Stop();
        }

        /* -----------------------------------------------------------------
         *   FileSystemWatcher
         * ----------------------------------------------------------------- */
        private void InitializeWatcher()
        {
            var watchDir = Path.GetDirectoryName(CargoPath)!;

            _watcher = new FileSystemWatcher(watchDir)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _watcher.Created += OnCargoChanged;
            _watcher.Changed += OnCargoChanged;
            _watcher.Renamed += OnCargoChanged;
            _watcher.Deleted += OnCargoChanged;
            _watcher.Error += OnWatcherError;

            // Debounce timer setup
            _debounceTimer.Interval = DebounceMs;
            _debounceTimer.Tick += DebounceTimer_Tick;

            Debug.WriteLine($"[Watcher] Monitoring folder: {watchDir}");
        }

        private void OnCargoChanged(object? sender, FileSystemEventArgs e)
        {
            Debug.WriteLine($"Watcher event: {e.ChangeType} – {e.FullPath}");
            if (!IsCargoFile(e.Name)) return;

            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void DebounceTimer_Tick(object? sender, EventArgs e)
        {
            _debounceTimer.Stop();

            // Give the file system a little breathing room before we try to read.
            Thread.Sleep(50);   // 50 ms delay

            // *** Polling thread with up to 10 retries ***
            new Thread(() =>
            {
                const int maxRetries = 10;
                const int retryDelayMs = 20;

                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    try
                    {
                        // Marshal the heavy work back onto the UI thread
                        Invoke((MethodInvoker)ProcessFullFile);
                        break;   // success – exit loop
                    }
                    catch (IOException)
                    {
                        // file still locked – wait a bit before retrying
                        Thread.Sleep(retryDelayMs);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Debounce thread error: {ex}");
                        break;   // unexpected error – stop retrying
                    }
                }
            }).Start();
        }

        private bool IsCargoFile(string fileName) =>
            string.Equals(fileName, Path.GetFileName(CargoPath), StringComparison.OrdinalIgnoreCase);

        private void OnWatcherError(object? sender, ErrorEventArgs e)
        {
            Debug.WriteLine($"[Watcher] Error: {e.GetException()}");
        }

        /* -----------------------------------------------------------------
         *   Polling fallback (1 s)
         * ----------------------------------------------------------------- */
        private readonly WinFormsTimer _pollTimer = new WinFormsTimer();
        private DateTime _lastWriteTimeUtc;
        private long _lastSize;

        private void StartPolling()
        {
            if (!File.Exists(CargoPath)) return;

            _lastWriteTimeUtc = File.GetLastWriteTimeUtc(CargoPath);
            _lastSize = new FileInfo(CargoPath).Length;

            _pollTimer.Interval = 1000;          // 1 s
            _pollTimer.Tick += PollTimer_Tick;
            _pollTimer.Start();
        }

        private void PollTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                var nowWrite = File.GetLastWriteTimeUtc(CargoPath);
                var nowSize = new FileInfo(CargoPath).Length;

                if (nowWrite != _lastWriteTimeUtc || nowSize != _lastSize)
                {
                    _lastWriteTimeUtc = nowWrite;
                    _lastSize = nowSize;
                    ProcessFullFile();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Polling error: {ex}");
            }
        }

        /* -----------------------------------------------------------------
         *  Core logic – read, hash, display, persist
         * ----------------------------------------------------------------- */
        private void ProcessFullFile()
        {
            const int maxAttempts = 10;
            const int delayMs = 100;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    if (!File.Exists(CargoPath)) return;

                    //  Open file with shared read‑write
                    using var stream = new FileStream(CargoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    string json = reader.ReadToEnd();

                    // Guard against empty or partially written files
                    var trimmed = json.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        Thread.Sleep(delayMs);
                        continue;
                    }

                    // Quick sanity check – it must start with “{” or “[”
                    if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
                    {
                        Thread.Sleep(delayMs);
                        continue;
                    }

                    //  Deserialize
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var snapshot = JsonSerializer.Deserialize<CargoSnapshot>(json, options);
                    if (snapshot == null) return;

                    //  Fingerprint guard – skip duplicate snapshots
                    string hash = ComputeHash(snapshot.Inventory);
                    if (hash == _lastInventoryHash) return;
                    _lastInventoryHash = hash;

                    //  UI + side‑effects
                    UpdateUI(snapshot);
                    WriteSnapshotToFile(snapshot);
                    break; // success
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    // file still locked – wait a bit before retrying
                    Thread.Sleep(delayMs);
                }
                catch (JsonException)
                {
                    // Malformed JSON – ignore for now and try again later
                    Thread.Sleep(delayMs);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ProcessFullFile error: {ex}");
                    break;   // unexpected error – stop retrying
                }
            }
        }

        private string ComputeHash(List<CargoItem> inventory)
        {
            string json = JsonSerializer.Serialize(
                inventory,
                new JsonSerializerOptions { WriteIndented = false, PropertyNameCaseInsensitive = true });

            using var sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            byte[] hashBytes = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        private void UpdateUI(CargoSnapshot snapshot)
        {
            string cargoString = string.Join(
                " ",
                snapshot.Inventory.Select(item =>
                    $"{(string.IsNullOrEmpty(item.Localised) ? item.Name : item.Localised)} ({item.Count})"));

            string entry = $"{cargoString}{Environment.NewLine}";
            _textBox.AppendText(entry);

            // ---- Autoprune ----
            TrimTextBoxLines(_textBox, MaxListItems);

            _textBox.SelectionStart = _textBox.TextLength;
            _textBox.ScrollToCaret();
        }

        private const int MaxListItems = 100;   // <-- PRUNE AT 100 LINES

        private void TrimTextBoxLines(TextBox textBox, int maxLines)
        {
            if (maxLines <= 0) return;
            string[] lines = textBox.Lines;
            if (lines.Length <= maxLines) return;
            textBox.Lines = lines.Skip(lines.Length - maxLines).ToArray();
        }

        private void WriteSnapshotToFile(CargoSnapshot snapshot)
        {
            string outputDir = "out";
            string outputFile = Path.Combine(outputDir, "cargo.txt");

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            string cargoString = string.Join(
                " ",
                snapshot.Inventory.Select(item =>
                    $"{(string.IsNullOrEmpty(item.Localised) ? item.Name : item.Localised)} ({item.Count})"));

            File.WriteAllText(outputFile, cargoString);
        }

        /* -----------------------------------------------------------------
         * ✨  About click handler
         * ----------------------------------------------------------------- */
        private void About_Click(object? sender, EventArgs e)
        {
            const string aboutText = "Made by insert3coins. Version 0.5a";
            _textBox.AppendText($"{aboutText}{Environment.NewLine}");
            _textBox.SelectionStart = _textBox.TextLength;
            _textBox.ScrollToCaret();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _verdanaFont?.Dispose();
        }
    }
}