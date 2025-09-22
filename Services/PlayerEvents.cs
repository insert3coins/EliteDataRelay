using EliteDataRelay.Models;
using System;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Provides data for the CommanderNameChanged event.
    /// </summary>
    public class CommanderNameChangedEventArgs : EventArgs
    {
        public string CommanderName { get; }
        public CommanderNameChangedEventArgs(string commanderName)
        {
            CommanderName = commanderName;
        }
    }

    /// <summary>
    /// Provides data for the ShipInfoChanged event.
    /// </summary>
    public class ShipInfoChangedEventArgs : EventArgs
    {
        public string ShipName { get; }
        public string ShipIdent { get; }
        public string ShipType { get; }
        public string InternalShipName { get; }

        public ShipInfoChangedEventArgs(string shipName, string shipIdent, string shipType, string internalShipName)
        {
            ShipName = shipName;
            ShipIdent = shipIdent;
            ShipType = shipType;
            InternalShipName = internalShipName;
        }
    }

    public class LoadoutChangedEventArgs : EventArgs
    {
        public ShipLoadout Loadout { get; }
        public LoadoutChangedEventArgs(ShipLoadout loadout)
        {
            Loadout = loadout;
        }
    }
}