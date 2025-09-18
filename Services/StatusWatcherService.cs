 ﻿using System;
 using System.Diagnostics;
 using System.IO;
 using System.Threading;
 using EliteDataRelay.Configuration;
 using EliteDataRelay.Models;
 using System.Text.Json;
 
 namespace EliteDataRelay.Services
 {
     /// <summary>
     /// Service for monitoring Status.json for real-time player status like balance.
     /// </summary>
     public class StatusWatcherService : IStatusWatcherService
     {
         private FileSystemWatcher? _watcher;
         private bool _isMonitoring;
         private long _lastKnownBalance = -1;
 
         public event EventHandler<BalanceChangedEventArgs>? BalanceChanged;
 
         public bool IsMonitoring => _isMonitoring;
 
         public void StartMonitoring()
         {
             if (_isMonitoring) return;
 
             // NOTE: AppConfiguration should be updated to include a path for Status.json
             // e.g., public static string StatusJsonPath => Path.Combine(PlayerDataPath, "Status.json");
             var statusFilePath = AppConfiguration.StatusJsonPath;
             var watchDir = Path.GetDirectoryName(statusFilePath);
 
             if (string.IsNullOrEmpty(watchDir) || !Directory.Exists(watchDir))
             {
                 Debug.WriteLine($"[StatusWatcherService] Directory not found, cannot start monitoring: {watchDir}");
                 return;
             }
 
             _watcher = new FileSystemWatcher(watchDir)
             {
                 Filter = Path.GetFileName(statusFilePath),
                 NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                 EnableRaisingEvents = true
             };
 
             _watcher.Changed += OnStatusFileChanged;
             _watcher.Created += OnStatusFileChanged;
 
             _isMonitoring = true;
             Debug.WriteLine($"[StatusWatcherService] Started monitoring {statusFilePath}");
 
             // Process file on start to get initial state
             ProcessStatusFile();
         }
 
         public void StopMonitoring()
         {
             if (!_isMonitoring) return;
 
             _watcher?.Dispose();
             _watcher = null;
             _isMonitoring = false;
             Debug.WriteLine("[StatusWatcherService] Stopped monitoring");
         }
 
         private void OnStatusFileChanged(object? sender, FileSystemEventArgs e)
         {
             ProcessStatusFile();
         }
 
         private void ProcessStatusFile()
         {
             // Use a short delay and retries to handle potential file locks
             new Thread(() =>
             {
                 for (int i = 0; i < 5; i++)
                 {
                     try
                     {
                         if (!File.Exists(AppConfiguration.StatusJsonPath)) return;
 
                         var json = File.ReadAllText(AppConfiguration.StatusJsonPath);
                         var status = JsonSerializer.Deserialize<StatusFile>(json);
 
                        // Only update if the Balance property exists in the JSON and has changed.
                        if (status?.Balance.HasValue == true && status.Balance.Value != _lastKnownBalance)
                         {
                            _lastKnownBalance = status.Balance.Value;
                            BalanceChanged?.Invoke(this, new BalanceChangedEventArgs(status.Balance.Value));
                         }
                         return; // Success
                     }
                     catch (IOException)
                     {
                         Thread.Sleep(100); // Wait and retry
                     }
                     catch (Exception ex)
                     {
                         Debug.WriteLine($"[StatusWatcherService] Error processing status file: {ex.Message}");
                         return; // Abort on other errors
                     }
                 }
             }).Start();
         }
 
         public void Dispose()
         {
             StopMonitoring();
         }
     }
 }