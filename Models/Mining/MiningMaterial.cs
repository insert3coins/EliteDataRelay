using System.Text.Json.Serialization;

namespace EliteDataRelay.Models.Mining
{
    public sealed class MiningMaterial
    {
        [JsonConstructor]
        public MiningMaterial(string name, double proportion)
        {
            Name = name;
            Proportion = proportion;
        }

        public string Name { get; }
        public double Proportion { get; }

        public MiningMaterial Clone() => new(Name, Proportion);
    }
}
