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
        private readonly string _filePath;
        private readonly string _fileDir;
        private readonly string _fileName;
        private bool _isMonitoring;
        private System.Threading.Timer? _debounceTimer;
        private readonly object _lock = new object();

        public event EventHandler? FileChanged;

        public bool IsMonitoring => _isMonitoring;

        public FileMonitoringService()
        {
            _filePath = AppConfiguration.CargoPath;
            _fileDir = Path.GetDirectoryName(_filePath) ?? "";
            _fileName = Path.GetFileName(_filePath);
        }

        public void StartMonitoring()
        {
            if (_isMonitoring || string.IsNullOrEmpty(_fileDir) || !Directory.Exists(_fileDir))
            {
                return;
            }

            _watcher = new FileSystemWatcher(_fileDir)
            {
                Filter = _fileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size, // Watch for content changes
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnFileChangeEvent;
            _watcher.Created += OnFileChangeEvent; // Handle if the file is created while watching

            _isMonitoring = true;
            Debug.WriteLine($"[FileMonitoringService] Started monitoring {_filePath}");
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
            }

            _isMonitoring = false;
            Debug.WriteLine("[FileMonitoringService] Stopped monitoring");
        }

        private void OnFileChangeEvent(object sender, FileSystemEventArgs e)
        {
            // Debounce the event to handle multiple rapid triggers for a single save.
            lock (_lock)
            {
                _debounceTimer?.Change(250, Timeout.Infinite); // Reset the timer
                _debounceTimer ??= new System.Threading.Timer(_ => FileChanged?.Invoke(this, EventArgs.Empty), null, 250, Timeout.Infinite);
            }
        }

        public void Dispose()
        {
            StopMonitoring();
        }
    }
}