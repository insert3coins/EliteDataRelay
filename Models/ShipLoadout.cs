using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    public class ShipLoadout
    {
        public string Ship { get; set; } = string.Empty;
        [JsonPropertyName("ShipID")]
        public int ShipId { get; set; }
        [JsonPropertyName("Ship_Localised")]
        public string? ShipLocalised { get; set; }
        public string ShipName { get; set; } = string.Empty;
        public string ShipIdent { get; set; } = string.Empty;
        public int CargoCapacity { get; set; }
        public double HullHealth { get; set; }
        public long HullValue { get; set; }
        public long ModulesValue { get; set; }
        public long Rebuy { get; set; }
        public double UnladenMass { get; set; }
        public double MaxJumpRange { get; set; }
        public FuelCapacityInfo? FuelCapacity { get; set; }
        public List<ShipModule> Modules { get; set; } = new List<ShipModule>();
    }

    public class ShipModule
    {
        public string Slot { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
        [JsonPropertyName("Item_Localised")]
        public string? ItemLocalised { get; set; }
        public bool On { get; set; }
        public int Priority { get; set; }
        public double Health { get; set; }
        public double Mass { get; set; }
        public long Value { get; set; }
        public ModuleEngineering? Engineering { get; set; }
    }

    public class ModuleEngineering
    {
        public string Engineer { get; set; } = string.Empty;
        [JsonPropertyName("EngineerID")]
        public long EngineerId { get; set; }
        public string BlueprintName { get; set; } = string.Empty;
        [JsonPropertyName("BlueprintID")]
        public long BlueprintId { get; set; }
        public int Level { get; set; }
        public double Quality { get; set; }
        [JsonPropertyName("ExperimentalEffect_Localised")]
        public string? ExperimentalEffect_Localised { get; set; }
        public List<ModuleModifier> Modifiers { get; set; } = new List<ModuleModifier>();
    }

    public class ModuleModifier
    {
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
        public double OriginalValue { get; set; }
        public int LessIsGood { get; set; }
    }

    public class FuelCapacityInfo
    {
        public double Main { get; set; }
        public double Reserve { get; set; }
    }
}
