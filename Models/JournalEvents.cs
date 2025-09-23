using System;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    public class LoadoutChangedEventArgs : EventArgs
    {
        public ShipLoadout Loadout { get; }

        public LoadoutChangedEventArgs(ShipLoadout loadout)
        {
            Loadout = loadout;
        }
    }

    public class StatusFile
    {
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }
        [JsonPropertyName("event")]
        public string? Event { get; set; }
        [JsonPropertyName("fuel")]
        public StatusFuel? Fuel { get; set; }
        [JsonPropertyName("cargo")]
        public double Cargo { get; set; }
        [JsonPropertyName("balance")]
        public long? Balance { get; set; }
        [JsonPropertyName("hullhealth")]
        public double? HullHealth { get; set; }
    }

    public class StatusFuel
    {
        public double FuelMain { get; set; }
        public double FuelReservoir { get; set; }
    }

    public class StatusChangedEventArgs : EventArgs
    {
        public StatusFile Status { get; }
        public StatusChangedEventArgs(StatusFile status) { Status = status; }
    }
}