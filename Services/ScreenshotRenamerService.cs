using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Listens for Screenshot journal events and renames/moves files to a friendly format.
    /// Controlled by AppConfiguration.EnableScreenshotRenamer and ScreenshotRenameFormat.
    /// </summary>
    public class ScreenshotRenamerService : IDisposable
    {
        private readonly IJournalWatcherService _journal;
        private bool _started;
        private FileSystemWatcher? _watcher;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> _processed = new();

        public ScreenshotRenamerService(IJournalWatcherService journal)
        {
            _journal = journal;
        }

        public void Start()
        {
            if (_started) return;
            _journal.ScreenshotTaken += OnScreenshotTaken;
            _started = true;

            // Also watch the default screenshots folder for .bmp files in case the journal event is delayed/missed
            try
            {
                var pics = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                var folder = Path.Combine(pics, "Frontier Developments", "Elite Dangerous");
                if (Directory.Exists(folder))
                {
                    _watcher = new FileSystemWatcher(folder)
                    {
                        Filter = "*.bmp",
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.Size,
                        EnableRaisingEvents = true
                    };
                    _watcher.Created += (s, e) =>
                    {
                        // Avoid double-processing if Screenshot event already handled it
                        if (_processed.ContainsKey(e.FullPath)) return;
                        ProcessScreenshot(e.FullPath, null, null, DateTime.UtcNow);
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ScreenshotRenamer] Watcher init failed: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!_started) return;
            _journal.ScreenshotTaken -= OnScreenshotTaken;
            _started = false;
        }

        private void OnScreenshotTaken(object? sender, ScreenshotEventArgs e)
        {
            if (!AppConfiguration.EnableScreenshotRenamer) return;
            ProcessScreenshot(e.FileName, e.SystemName, e.BodyName, e.Timestamp);
        }

        private void ProcessScreenshot(string sourcePath, string? systemName, string? bodyName, DateTime timestamp)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath)) return;

                if (!_processed.TryAdd(sourcePath, DateTime.UtcNow)) return; // already handled

                // Wait briefly for the game to finish writing the file
                const int maxAttempts = 30; // up to ~3s
                for (int i = 0; i < maxAttempts; i++)
                {
                    try
                    {
                        using var fs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.None);
                        break;
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(100);
                    }
                }

                var folder = Path.GetDirectoryName(sourcePath) ?? AppDomain.CurrentDomain.BaseDirectory;
                var ext = Path.GetExtension(sourcePath) ?? ".bmp";

                // Convert BMP to PNG prior to rename
                string workingPath = sourcePath;
                if (ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using (var img = Image.FromFile(sourcePath))
                        {
                            var pngPath = Path.ChangeExtension(sourcePath, ".png");
                            img.Save(pngPath, ImageFormat.Png);
                            workingPath = pngPath;
                        }
                        File.Delete(sourcePath);
                        ext = ".png";
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ScreenshotRenamer] BMP->PNG conversion failed: {ex.Message}");
                    }
                }

                // Prefer provided system; fallback to last known location from journal watcher
                string? sys = systemName;
                if (string.IsNullOrWhiteSpace(sys))
                {
                    var lastLoc = _journal.GetLastKnownLocation();
                    if (lastLoc != null && !string.IsNullOrWhiteSpace(lastLoc.StarSystem))
                    {
                        sys = lastLoc.StarSystem;
                    }
                }
                var system = string.IsNullOrWhiteSpace(sys) ? "Unknown System" : sys!;
                var body = string.IsNullOrWhiteSpace(bodyName) ? "" : bodyName!;
                var ts = timestamp.ToLocalTime().ToString("yyyyMMdd-HHmmss");

                string name = AppConfiguration.ScreenshotRenameFormat
                    .Replace("{System}", Sanitize(system))
                    .Replace("{Body}", Sanitize(body))
                    .Replace("{Timestamp}", ts);

                // Trim extra separators
                name = Regex.Replace(name, @"\s+-\s+$", string.Empty);
                name = string.IsNullOrWhiteSpace(name) ? ts : name;

                var targetPath = Path.Combine(folder, name + ext);
                targetPath = EnsureUniquePath(targetPath);

                File.Move(workingPath, targetPath);
                Debug.WriteLine($"[ScreenshotRenamer] Saved {Path.GetFileName(targetPath)}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ScreenshotRenamer] Failed to process screenshot: {ex.Message}");
            }
        }

        private static string EnsureUniquePath(string path)
        {
            if (!File.Exists(path)) return path;
            var dir = Path.GetDirectoryName(path) ?? ".";
            var baseName = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);
            int i = 1;
            string candidate;
            do
            {
                candidate = Path.Combine(dir, $"{baseName} ({i++}){ext}");
            } while (File.Exists(candidate));
            return candidate;
        }

        private static string Sanitize(string input)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                input = input.Replace(c, '_');
            }
            return input.Trim();
        }

        public void Dispose()
        {
            Stop();
            try { _watcher?.Dispose(); } catch { }
        }
    }
}
