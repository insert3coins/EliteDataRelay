using System;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models.Mining
{
    public enum MiningItemType
    {
        Ore,
        Material
    }

    public sealed class MiningItem
    {
        public MiningItem(string name, MiningItemType type)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
        }

        [JsonConstructor]
        public MiningItem(
            string name,
            MiningItemType type,
            int refinedCount,
            int collectedCount,
            int prospected,
            double minPercentage,
            double maxPercentage,
            int motherLoad,
            int lowContent,
            int medContent,
            int highContent)
            : this(name, type)
        {
            RefinedCount = refinedCount;
            CollectedCount = collectedCount;
            Prospected = prospected;
            MinPercentage = minPercentage;
            MaxPercentage = maxPercentage;
            MotherLoad = motherLoad;
            LowContent = lowContent;
            MedContent = medContent;
            HighContent = highContent;
        }

        public string Name { get; }
        public MiningItemType Type { get; }
        public int RefinedCount { get; set; }
        public int CollectedCount { get; set; }
        public int Prospected { get; set; }
        public double MinPercentage { get; set; }
        public double MaxPercentage { get; set; }
        public int MotherLoad { get; set; }
        public int LowContent { get; set; }
        public int MedContent { get; set; }
        public int HighContent { get; set; }
        public int ContentHitCount => LowContent + MedContent + HighContent;

        public void AddContent(string? content)
        {
            switch (content)
            {
                case "$AsteroidMaterialContent_Low;":
                    LowContent++;
                    break;
                case "$AsteroidMaterialContent_Medium;":
                    MedContent++;
                    break;
                case "$AsteroidMaterialContent_High;":
                    HighContent++;
                    break;
            }
        }

        public MiningItem Clone()
        {
            return new MiningItem(Name, Type)
            {
                RefinedCount = RefinedCount,
                CollectedCount = CollectedCount,
                Prospected = Prospected,
                MinPercentage = MinPercentage,
                MaxPercentage = MaxPercentage,
                MotherLoad = MotherLoad,
                LowContent = LowContent,
                MedContent = MedContent,
                HighContent = HighContent
            };
        }
    }
}
