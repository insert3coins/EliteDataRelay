using EliteDataRelay.Models;
using System;
using System.Text.Json.Serialization;

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

    /// <summary>
    /// Provides data for the BalanceChanged event.
    /// </summary>
    public class BalanceChangedEventArgs : EventArgs
    {
        public long Balance { get; }
        public BalanceChangedEventArgs(long balance)
        {
            Balance = balance;
        }
    }

    public class LoadGameEvent
    {
        public string Commander { get; set; } = string.Empty;
        public string ShipName { get; set; } = string.Empty;
        public string ShipIdent { get; set; } = string.Empty;
        [JsonPropertyName("Ship_Localised")]
        public string? ShipLocalised { get; set; }
        public long Credits { get; set; }
    }

    public class ShipyardSwapEvent
    {
        public string ShipType { get; set; } = string.Empty;
        [JsonPropertyName("ShipType_Localised")]
        public string? ShipTypeLocalised { get; set; }
    }

}
