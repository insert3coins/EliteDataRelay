using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models
{
    public class ShipLoadout
    {
        [JsonPropertyName("Ship")]
        public string Ship { get; set; } = string.Empty;

        [JsonPropertyName("ShipName")]
        public string ShipName { get; set; } = string.Empty;

        [JsonPropertyName("ShipIdent")]
        public string ShipIdent { get; set; } = string.Empty;

        [JsonPropertyName("CargoCapacity")]
        public int CargoCapacity { get; set; }

        [JsonPropertyName("Modules")]
        public List<ShipModule> Modules { get; set; } = new List<ShipModule>();
    }

    public class ShipModule
    {
        [JsonPropertyName("Slot")]
        public string Slot { get; set; } = string.Empty;

        [JsonPropertyName("Item")]
        public string Item { get; set; } = string.Empty;

        [JsonPropertyName("On")]
        public bool IsOn { get; set; }

        [JsonPropertyName("Priority")]
        public int Priority { get; set; }

        [JsonPropertyName("Health")]
        public double Health { get; set; }

        [JsonPropertyName("Engineering")]
        public ModuleEngineering? Engineering { get; set; }
    }

    public class ModuleEngineering
    {
        [JsonPropertyName("Engineer")]
        public string Engineer { get; set; } = string.Empty;

        [JsonPropertyName("BlueprintName")]
        public string BlueprintName { get; set; } = string.Empty;

        [JsonPropertyName("Level")]
        public int Level { get; set; }

        [JsonPropertyName("Quality")]
        public double Quality { get; set; }

        [JsonPropertyName("Modifiers")]
        public List<EngineeringModifier> Modifiers { get; set; } = new List<EngineeringModifier>();
    }

    public class EngineeringModifier
    {
        [JsonPropertyName("Label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("Value")]
        public double Value { get; set; }

        [JsonPropertyName("OriginalValue")]
        public double OriginalValue { get; set; }

        [JsonPropertyName("LessIsGood")]
        public int LessIsGood { get; set; }
    }
}