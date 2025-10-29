using System;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Provides data for the <see cref="IJournalWatcherService.CargoCollected"/> event.
    /// </summary>
    public class CargoCollectedEventArgs : EventArgs
    {
        public string Commodity { get; }

        public CargoCollectedEventArgs(string commodity) => Commodity = commodity;
    }
}
