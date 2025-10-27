using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Monitors a specific file for changes using the efficient FileSystemWatcher.
    /// This provides near-instant notifications of file modifications.
    /// </summary>
    public class FileMonitoringService : IFileMonitoringService
    {
        private FileSystemWatcher? _watcher;
        private readonly string? _filePath;
        private readonly string _fileName;
        private bool _isMonitoring;
        private System.Threading.Timer? _debounceTimer;
        private System.Threading.Timer? _directoryCheckTimer;
        private readonly object _lock = new object();
        private DateTime _lastActivityTime = DateTime.MinValue;
        private const int ActiveWindowSeconds = 10; // Consider "active" for 10 seconds after a change

        public event Action<string>? FileChanged;

        public bool IsMonitoring => _isMonitoring;

        /// <summary>
        /// Returns true if there has been recent activity (within the last 10 seconds).
        /// This can be used to adaptively increase monitoring frequency.
        /// </summary>
        public bool IsRecentlyActive => (DateTime.UtcNow - _lastActivityTime).TotalSeconds < ActiveWindowSeconds;

        public FileMonitoringService(IJournalWatcherService journalWatcher)
        {
            _filePath = journalWatcher.JournalDirectoryPath;
            // This service now only watches for Cargo.json
            _fileName = "Cargo.json"; 
        }

        public void StartMonitoring()
        {
            if (_isMonitoring || string.IsNullOrEmpty(_filePath)) return;
            
            _isMonitoring = true;
            Debug.WriteLine($"[FileMonitoringService] Starting monitoring for {_filePath}");

            // Attempt to initialize the watcher immediately.
            TryInitializeWatcher();
        }

        private void TryInitializeWatcher()
        {
            lock (_lock)
            {
                // Don't try if the watcher is already running or the path is invalid.
                if (_watcher != null || string.IsNullOrEmpty(_filePath)) return;

                if (!Directory.Exists(_filePath))
                {
                    // Directory doesn't exist yet. Start a timer to check for it periodically.
                    _directoryCheckTimer ??= new System.Threading.Timer(_ => TryInitializeWatcher(), null, 5000, 5000);
                    return;
                }

            _watcher = new FileSystemWatcher(_filePath)
            {
                Filter = _fileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size, // Watch for content changes
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnFileChangeEvent;
            _watcher.Created += OnFileChangeEvent; // Handle if the file is created while watching

                // The directory now exists, so we can stop the check timer.
                _directoryCheckTimer?.Dispose();
                _directoryCheckTimer = null;
                Debug.WriteLine($"[FileMonitoringService] Successfully initialized watcher for directory: {_filePath}");
            }
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring) return;

            lock (_lock)
            {
                _watcher?.Dispose();
                _watcher = null;
                _debounceTimer?.Dispose();
                _debounceTimer = null;
                _directoryCheckTimer?.Dispose();
                _directoryCheckTimer = null;
            }

            _isMonitoring = false;
            Debug.WriteLine("[FileMonitoringService] Stopped monitoring");
        }

        private void OnFileChangeEvent(object sender, FileSystemEventArgs e)
        {
            // Capture the filename and guard against nulls to prevent CS8604 warning.
            string? fileName = e.Name;
            if (fileName == null) return;

            // Track activity time for adaptive monitoring
            _lastActivityTime = DateTime.UtcNow;

            // Debounce the event to handle multiple rapid triggers for a single save.
            lock (_lock)
            {
                // Only process if monitoring is still active
                if (_isMonitoring)
                {
                    // Dispose the old timer to ensure we can create a new one.
                    // This fixes the bug where subsequent updates would not fire.
                    _debounceTimer?.Dispose();
                    const int debounceTimeMs = 25; // Reduced for faster in-game response
                    _debounceTimer = new System.Threading.Timer(_ => FileChanged?.Invoke(fileName), null, debounceTimeMs, Timeout.Infinite);
                }
            }
        }

        public void Dispose()
        {
            StopMonitoring();
        }
    }
}