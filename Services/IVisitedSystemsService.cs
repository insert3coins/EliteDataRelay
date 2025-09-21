using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EliteDataRelay.Models;

namespace EliteDataRelay.Services
{
    public interface IVisitedSystemsService : IDisposable
    {
        event EventHandler? SystemsUpdated;
        event EventHandler<JournalScanCompletedEventArgs>? JournalScanCompleted;
        IReadOnlyList<StarSystem> VisitedSystems { get; }
        void Start();
        void Stop();
        Task ScanAllJournalsAsync();
    }
}