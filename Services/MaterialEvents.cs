using EliteDataRelay.Models;
using System;

namespace EliteDataRelay.Services
{
    #region Material Event Args

    public class MaterialsEventArgs : EventArgs
    {
        public MaterialsEvent EventData { get; }
        public MaterialsEventArgs(MaterialsEvent eventData) => EventData = eventData;
    }

    public class MaterialCollectedEventArgs : EventArgs
    {
        public MaterialCollectedEvent EventData { get; }
        public MaterialCollectedEventArgs(MaterialCollectedEvent eventData) => EventData = eventData;
    }

    public class MaterialTradeEventArgs : EventArgs
    {
        public MaterialTradeEvent EventData { get; }
        public MaterialTradeEventArgs(MaterialTradeEvent eventData) => EventData = eventData;
    }

    public class EngineerCraftEventArgs : EventArgs
    {
        public EngineerCraftEvent EventData { get; }
        public EngineerCraftEventArgs(EngineerCraftEvent eventData) => EventData = eventData;
    }

    #endregion
}