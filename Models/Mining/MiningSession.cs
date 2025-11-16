using EliteDataRelay.Models.Journal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models.Mining
{
    public sealed class MiningSession
    {
        public MiningSession(string starSystem, string location, long? systemAddress, long? bodyId)
        {
            StarSystem = starSystem;
            Location = location;
            SystemAddress = systemAddress;
            BodyID = bodyId;
        }

        [JsonConstructor]
        public MiningSession(
            string starSystem,
            string location,
            long? systemAddress,
            long? bodyID,
            DateTime timeStarted,
            DateTime timeFinished,
            List<MiningItem>? items,
            List<MiningProspector>? prospectors,
            int asteroidsProspected,
            int asteroidsCracked,
            int prospectorsFired,
            int collectorsDeployed,
            int lowContent,
            int medContent,
            int highContent)
            : this(starSystem, location, systemAddress, bodyID)
        {
            TimeStarted = timeStarted;
            TimeFinished = timeFinished;
            AsteroidsProspected = asteroidsProspected;
            AsteroidsCracked = asteroidsCracked;
            ProspectorsFired = prospectorsFired;
            CollectorsDeployed = collectorsDeployed;
            LowContent = lowContent;
            MedContent = medContent;
            HighContent = highContent;

            Items.Clear();
            if (items != null && items.Count > 0)
            {
                Items.AddRange(items.Select(item => item.Clone()));
            }

            Prospectors.Clear();
            if (prospectors != null && prospectors.Count > 0)
            {
                foreach (var prospector in prospectors)
                {
                    var materials = prospector.Materials.Select(m => m.Clone()).ToList();
                    Prospectors.Add(new MiningProspector(materials, prospector.Content, prospector.MotherlodeMaterial, prospector.Remaining));
                }
            }
        }

        public string StarSystem { get; }
        public string Location { get; }
        public long? SystemAddress { get; }
        public long? BodyID { get; }
        public DateTime TimeStarted { get; private set; } = DateTime.MaxValue;
        public DateTime TimeFinished { get; set; } = DateTime.MaxValue;
        public List<MiningItem> Items { get; } = new();
        public List<MiningProspector> Prospectors { get; } = new();
        public int AsteroidsProspected { get; private set; }
        public int AsteroidsCracked { get; set; }
        public int ProspectorsFired { get; set; }
        public int CollectorsDeployed { get; set; }
        public int LowContent { get; private set; }
        public int MedContent { get; private set; }
        public int HighContent { get; private set; }

        internal bool Started => ProspectorsFired > 0 || CollectorsDeployed > 0 || HasData;

        internal bool HasData => AsteroidsProspected > 0 || Items.Any(x => x.Type == MiningItemType.Ore && x.RefinedCount > 0);

        internal void CheckStartTime(DateTime timestamp)
        {
            if (timestamp < TimeStarted)
            {
                TimeStarted = timestamp;
            }
        }

        internal void AddAsteroid(ProspectedAsteroidEventArgs e)
        {
            if (e.Remaining <= 0)
            {
                return;
            }

            AsteroidsProspected++;

            if (!string.IsNullOrWhiteSpace(e.MotherlodeMaterial))
            {
                var motherlodeName = MiningNameHelper.NormalizeName(e.MotherlodeMaterial);
                var known = Items.FirstOrDefault(x => string.Equals(x.Name, motherlodeName, StringComparison.OrdinalIgnoreCase));
                if (known == null)
                {
                    known = new MiningItem(motherlodeName, MiningItemType.Ore);
                    Items.Add(known);
                }
                known.MotherLoad++;
                known.Prospected++;
                known.AddContent(e.Content);
            }

            foreach (var mat in e.Materials)
            {
                var friendly = MiningNameHelper.NormalizeName(mat.Name, mat.LocalisedName);
                var known = Items.FirstOrDefault(x => string.Equals(x.Name, friendly, StringComparison.OrdinalIgnoreCase));
                if (known == null)
                {
                    known = new MiningItem(friendly, MiningItemType.Ore);
                    Items.Add(known);
                }

                if (known.MinPercentage == 0 || known.MinPercentage > mat.Proportion)
                {
                    known.MinPercentage = mat.Proportion;
                }

                if (mat.Proportion > known.MaxPercentage)
                {
                    known.MaxPercentage = mat.Proportion;
                }

                known.Prospected++;
                known.AddContent(e.Content);
            }

            switch (e.Content)
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

        internal void AddOre(string commodityName)
        {
            var friendly = MiningNameHelper.NormalizeName(commodityName);
            var known = Items.FirstOrDefault(x => string.Equals(x.Name, friendly, StringComparison.OrdinalIgnoreCase));
            if (known == null)
            {
                known = new MiningItem(friendly, MiningItemType.Ore);
                Items.Add(known);
            }

            known.RefinedCount++;
        }

        internal void AddMaterial(MaterialCollectedEventArgs e)
        {
            var friendly = MiningNameHelper.NormalizeName(e.Name, e.LocalisedName);
            var known = Items.FirstOrDefault(x => string.Equals(x.Name, friendly, StringComparison.OrdinalIgnoreCase));
            if (known == null)
            {
                known = new MiningItem(friendly, MiningItemType.Material);
                Items.Add(known);
            }

            known.CollectedCount += Math.Max(1, e.Count);
        }

        public void AddProspector(MiningProspector prospector)
        {
            if (Prospectors.Contains(prospector))
            {
                return;
            }

            Prospectors.Insert(0, prospector);
        }

        internal MiningSession Clone()
        {
            var clone = new MiningSession(StarSystem, Location, SystemAddress, BodyID)
            {
                TimeStarted = TimeStarted,
                TimeFinished = TimeFinished,
                AsteroidsProspected = AsteroidsProspected,
                AsteroidsCracked = AsteroidsCracked,
                ProspectorsFired = ProspectorsFired,
                CollectorsDeployed = CollectorsDeployed,
                LowContent = LowContent,
                MedContent = MedContent,
                HighContent = HighContent
            };

            foreach (var item in Items)
            {
                clone.Items.Add(item.Clone());
            }
            foreach (var prospector in Prospectors)
            {
                var materials = prospector.Materials.Select(m => m.Clone()).ToList();
                clone.Prospectors.Add(new MiningProspector(materials, prospector.Content, prospector.MotherlodeMaterial, prospector.Remaining));
            }

            return clone;
        }
    }
}
