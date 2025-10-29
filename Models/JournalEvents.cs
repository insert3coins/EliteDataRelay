using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    /// <summary>
    /// Represents the "ShipyardNew" event from the journal.
    /// Written when a new ship is purchased.
    /// </summary>
    public class ShipyardNewEvent
    {
        [JsonPropertyName("ShipType")]
        public string ShipType { get; set; } = string.Empty;

        [JsonPropertyName("ShipType_Localised")]
        public string? ShipTypeLocalised { get; set; }
    }

    /// <summary>
    /// Represents the "ShipyardSwap" event from the journal.
    /// Written when switching to another stored ship.
    /// </summary>
    public class ShipyardSwapEvent : ShipyardNewEvent { }

    /// <summary>
    /// Represents the "Docked" event from the journal.
    /// Written when the player docks at a station or carrier.
    /// </summary>
    public class DockedEvent
    {
        [JsonIgnore] // This is populated manually from the root of the journal event
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("StationName")]
        public string StationName { get; set; } = string.Empty;

        [JsonPropertyName("Name")] // Used for Fleet Carrier custom name
        public string? Name { get; set; }

        [JsonPropertyName("StationType")]
        public string StationType { get; set; } = string.Empty;

        [JsonPropertyName("StationFaction")]
        public Faction? StationFaction { get; set; }

        [JsonPropertyName("StationGovernment_Localised")]
        public string? StationGovernmentLocalised { get; set; }

        [JsonPropertyName("StationGovernment")]
        public string? StationGovernment { get; set; }

        [JsonPropertyName("StationAllegiance")]
        public string? StationAllegiance { get; set; }

        [JsonPropertyName("StationEconomy_Localised")]
        public string? StationEconomyLocalised { get; set; }

        [JsonPropertyName("StationEconomy")]
        public string? StationEconomy { get; set; }

        [JsonPropertyName("StationServices")]
        public List<string> StationServices { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents the "FSSDiscoveryScan" event from the journal.
    /// Written when using FSS to discover the number of bodies in a system.
    /// </summary>
    public class FSSDiscoveryScanEvent
    {
        [JsonPropertyName("Progress")]
        public double Progress { get; set; }

        [JsonPropertyName("BodyCount")]
        public int BodyCount { get; set; }

        [JsonPropertyName("NonBodyCount")]
        public int NonBodyCount { get; set; }

        [JsonPropertyName("SystemName")]
        public string? SystemName { get; set; }

        [JsonPropertyName("SystemAddress")]
        public long? SystemAddress { get; set; }
    }

    /// <summary>
    /// Represents the "SAAScanComplete" event from the journal.
    /// Written when detailed surface scan is completed.
    /// </summary>
    public class SAAScanCompleteEvent
    {
        [JsonPropertyName("BodyName")]
        public string BodyName { get; set; } = string.Empty;

        [JsonPropertyName("BodyID")]
        public int? BodyID { get; set; }

        [JsonPropertyName("SystemAddress")]
        public long? SystemAddress { get; set; }

        [JsonPropertyName("ProbesUsed")]
        public int? ProbesUsed { get; set; }

        [JsonPropertyName("EfficiencyTarget")]
        public int? EfficiencyTarget { get; set; }
    }

    /// <summary>
    /// Represents the "FSSBodySignals" event from the journal.
    /// Written when FSS identifies signals on a body.
    /// </summary>
    public class FSSBodySignalsEvent
    {
        [JsonPropertyName("BodyName")]
        public string BodyName { get; set; } = string.Empty;

        [JsonPropertyName("BodyID")]
        public int? BodyID { get; set; }

        [JsonPropertyName("SystemAddress")]
        public long? SystemAddress { get; set; }

        [JsonPropertyName("Signals")]
        public List<Signal> Signals { get; set; } = new List<Signal>();
    }

    /// <summary>
    /// Represents the "SAASignalsFound" event from the journal.
    /// Written when biological, geological, or other signals are found during mapping.
    /// </summary>
    public class SAASignalsFoundEvent
    {
        [JsonPropertyName("BodyName")]
        public string BodyName { get; set; } = string.Empty;

        [JsonPropertyName("BodyID")]
        public int? BodyID { get; set; }

        [JsonPropertyName("SystemAddress")]
        public long? SystemAddress { get; set; }

        [JsonPropertyName("Signals")]
        public List<Signal> Signals { get; set; } = new List<Signal>();

        [JsonPropertyName("Genuses")]
        public List<Genus>? Genuses { get; set; }
    }

    /// <summary>
    /// Represents a signal found on a body.
    /// </summary>
    public class Signal
    {
        [JsonPropertyName("Type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("Type_Localised")]
        public string? TypeLocalised { get; set; }

        [JsonPropertyName("Count")]
        public int Count { get; set; }
    }

    /// <summary>
    /// Represents genus information for biological signals.
    /// </summary>
    public class Genus
    {
        [JsonPropertyName("Genus")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("Genus_Localised")]
        public string? NameLocalised { get; set; }
    }

    /// <summary>
    /// Represents the "SellExplorationData" event from the journal.
    /// Written when selling exploration data at a station.
    /// </summary>
    public class SellExplorationDataEvent
    {
        [JsonPropertyName("Systems")]
        public List<string> Systems { get; set; } = new List<string>();

        [JsonPropertyName("Discovered")]
        public List<string> Discovered { get; set; } = new List<string>();

        [JsonPropertyName("BaseValue")]
        public long BaseValue { get; set; }

        [JsonPropertyName("Bonus")]
        public long Bonus { get; set; }

        [JsonPropertyName("TotalEarnings")]
        public long TotalEarnings { get; set; }
    }

    /// <summary>
    /// Represents the "Touchdown" event from the journal.
    /// Written when a ship or SRV touches down on a planet surface.
    /// </summary>
    public class TouchdownEvent
    {
        [JsonPropertyName("StarSystem")]
        public string? StarSystem { get; set; }

        [JsonPropertyName("SystemAddress")]
        public long? SystemAddress { get; set; }

        [JsonPropertyName("Body")]
        public string? Body { get; set; }

        [JsonPropertyName("BodyID")]
        public int? BodyID { get; set; }

        [JsonPropertyName("OnStation")]
        public bool? OnStation { get; set; }

        [JsonPropertyName("OnPlanet")]
        public bool? OnPlanet { get; set; }

        [JsonPropertyName("Latitude")]
        public double? Latitude { get; set; }

        [JsonPropertyName("Longitude")]
        public double? Longitude { get; set; }

        [JsonPropertyName("PlayerControlled")]
        public bool? PlayerControlled { get; set; }

        [JsonPropertyName("NearestDestination")]
        public string? NearestDestination { get; set; }
    }
}
