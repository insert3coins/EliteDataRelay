 ﻿using System;
 
 namespace EliteCargoMonitor.Services
 {
     public class BalanceChangedEventArgs : EventArgs
     {
         public long Balance { get; }
         public BalanceChangedEventArgs(long balance) { Balance = balance; }
     }
 
     /// <summary>
     /// Interface for a service that monitors Status.json for real-time player status.
     /// </summary>
     public interface IStatusWatcherService : IDisposable
     {
         /// <summary>
         /// Event raised when the player's balance changes.
         /// </summary>
         event EventHandler<BalanceChangedEventArgs>? BalanceChanged;
 
         /// <summary>
         /// Starts monitoring the status file.
         /// </summary>
         void StartMonitoring();
 
         /// <summary>
         /// Stops monitoring the status file.
         /// </summary>
         void StopMonitoring();
 
         /// <summary>
         /// Gets a value indicating whether the service is currently monitoring.
         /// </summary>
         bool IsMonitoring { get; }
     }
 }