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

    /// <summary>
    /// Provides data for the NextJumpSystemChanged event.
    /// </summary>
    public class NextJumpSystemChangedEventArgs : EventArgs
    {
        public string NextSystemName { get; }
        public string? StarClass { get; }
        public double? JumpDistanceLy { get; }
        public int? RemainingJumps { get; }

        public NextJumpSystemChangedEventArgs(string nextSystemName, string? starClass = null, double? jumpDistanceLy = null, int? remainingJumps = null)
        {
            NextSystemName = nextSystemName;
            StarClass = starClass;
            JumpDistanceLy = jumpDistanceLy;
            RemainingJumps = remainingJumps;
        }
    }

    /// <summary>
    /// Provides data when a hyperspace jump is initiated (FSD charging).
    /// </summary>
    public class JumpInitiatedEventArgs : EventArgs
    {
        public string TargetSystemName { get; }
        public string? StarClass { get; }
        public double? JumpDistanceLy { get; }
        public int? RemainingJumps { get; }

        public JumpInitiatedEventArgs(string targetSystemName, string? starClass, double? jumpDistanceLy, int? remainingJumps)
        {
            TargetSystemName = targetSystemName;
            StarClass = starClass;
            JumpDistanceLy = jumpDistanceLy;
            RemainingJumps = remainingJumps;
        }
    }

    /// <summary>
    /// Provides data when a hyperspace jump completes (FSDJump).
    /// Mirrors how SrvSurvey surfaces destination info post-jump.
    /// </summary>
    public class JumpCompletedEventArgs : EventArgs
    {
        public string SystemName { get; }
        public string? StarClass { get; }
        public double? JumpDistanceLy { get; }

        public JumpCompletedEventArgs(string systemName, string? starClass, double? jumpDistanceLy)
        {
            SystemName = systemName;
            StarClass = starClass;
            JumpDistanceLy = jumpDistanceLy;
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
