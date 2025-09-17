using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EliteCargoMonitor.Configuration;

namespace EliteCargoMonitor.Services
{
    public class FileMonitoringService : IFileMonitoringService, IDisposable
    {
        private FileSystemWatcher? _watcher;
        private readonly System.Windows.Forms.Timer _debounceTimer;
        private readonly System.Windows.Forms.Timer _pollTimer;
        private DateTime _lastWriteTimeUtc;
        private long _lastSize;
        private bool _isMonitoring;

        public event EventHandler? FileChanged;

        public bool IsMonitoring => _isMonitoring;

        public FileMonitoringService()
        {
            // Initialize debounce timer
            _debounceTimer = new System.Windows.Forms.Timer
            {
                Interval = AppConfiguration.DebounceDelayMs
            };
            _debounceTimer.Tick += DebounceTimer_Tick;

            // Initialize polling timer
            _pollTimer = new System.Windows.Forms.Timer
            {
                Interval = AppConfiguration.PollingIntervalMs
            };
            _pollTimer.Tick += PollTimer_Tick;
        }

        public void StartMonitoring()
        {
            if (_isMonitoring) return;

            InitializeFileSystemWatcher();
            StartPollingFallback();
            _isMonitoring = true;

            Debug.WriteLine("[FileMonitoringService] Started monitoring");
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring) return;

            _watcher?.Dispose();
            _watcher = null;
            _debounceTimer.Stop();
            _pollTimer.Stop();
            _isMonitoring = false;

            Debug.WriteLine("[FileMonitoringService] Stopped monitoring");
        }

        private void InitializeFileSystemWatcher()
        {
            var watchDir = Path.GetDirectoryName(AppConfiguration.CargoPath);
            if (string.IsNullOrEmpty(watchDir)) return;

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

            Debug.WriteLine($"[FileMonitoringService] FileSystemWatcher monitoring: {watchDir}");
        }

        private void OnCargoChanged(object? sender, FileSystemEventArgs e)
        {
            Debug.WriteLine($"[FileMonitoringService] Watcher event: {e.ChangeType} – {e.FullPath}");
            
            if (!IsCargoFile(e.Name)) return;

            // Reset debounce timer
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private async void DebounceTimer_Tick(object? sender, EventArgs e)
        {
            _debounceTimer.Stop();
 
            // Give the file system a little breathing room before we try to read.
            // The actual read and retry logic is now handled by the processor service.
            await Task.Delay(AppConfiguration.FileSystemDelayMs);
 
            // Raise the event. The consumer is responsible for handling file access and retries.
            FileChanged?.Invoke(this, EventArgs.Empty);
        }

        private void StartPollingFallback()
        {
            if (!File.Exists(AppConfiguration.CargoPath)) return;

            _lastWriteTimeUtc = File.GetLastWriteTimeUtc(AppConfiguration.CargoPath);
            _lastSize = new FileInfo(AppConfiguration.CargoPath).Length;
            _pollTimer.Start();

            Debug.WriteLine("[FileMonitoringService] Polling fallback started");
        }

        private async void PollTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Perform file I/O on a background thread to avoid blocking the UI.
                var (changed, nowWrite, nowSize) = await Task.Run(() =>
                {
                    if (!File.Exists(AppConfiguration.CargoPath))
                    {
                        return (false, DateTime.MinValue, 0L);
                    }

                    var currentWrite = File.GetLastWriteTimeUtc(AppConfiguration.CargoPath);
                    var currentSize = new FileInfo(AppConfiguration.CargoPath).Length;

                    bool hasChanged = currentWrite != _lastWriteTimeUtc || currentSize != _lastSize;
                    return (hasChanged, currentWrite, currentSize);
                });

                if (changed)
                {
                    _lastWriteTimeUtc = nowWrite;
                    _lastSize = nowSize;
                    
                    Debug.WriteLine("[FileMonitoringService] Polling detected change");
                    // This is invoked on the UI thread because we awaited on the UI thread.
                    FileChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FileMonitoringService] Polling error: {ex}");
            }
        }

        private bool IsCargoFile(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;
            return string.Equals(fileName, Path.GetFileName(AppConfiguration.CargoPath), StringComparison.OrdinalIgnoreCase);
        }

        private void OnWatcherError(object? sender, ErrorEventArgs e)
        {
            Debug.WriteLine($"[FileMonitoringService] Watcher error: {e.GetException()}");
        }

        public void Dispose()
        {
            StopMonitoring();
            _debounceTimer?.Dispose();
            _pollTimer?.Dispose();
        }
    }
}