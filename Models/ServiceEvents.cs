using System;
using EliteDataRelay.Models;

namespace EliteDataRelay.Models
{
    public class LoadoutChangedEventArgs : EventArgs
    {
        public ShipLoadout Loadout { get; }
        public LoadoutChangedEventArgs(ShipLoadout loadout) => Loadout = loadout;
    }

    public class StatusChangedEventArgs : EventArgs
    {
        public Status Status { get; }
        public StatusChangedEventArgs(Status status) => Status = status;
    }

    public class DockedEventArgs : EventArgs
    {
        public DockedEvent DockedEvent { get; }
        public DockedEventArgs(DockedEvent dockedEvent) => DockedEvent = dockedEvent;
    }

    public class UndockedEventArgs : EventArgs
    {
        public string StationName { get; }
        public UndockedEventArgs(string stationName) => StationName = stationName;
    }

    public class BuyDronesEventArgs : EventArgs
    {
        public int Count { get; }
        public long TotalCost { get; }
        public BuyDronesEventArgs(int count, long totalCost)
        {
            Count = count;
            TotalCost = totalCost;
        }
    }

    public class ScreenshotEventArgs : EventArgs
    {
        public string FileName { get; }
        public string? SystemName { get; }
        public string? BodyName { get; }
        public DateTime Timestamp { get; }

        public ScreenshotEventArgs(string fileName, string? systemName, string? bodyName, DateTime timestamp)
        {
            FileName = fileName;
            SystemName = systemName;
            BodyName = bodyName;
            Timestamp = timestamp;
        }
    }
}
