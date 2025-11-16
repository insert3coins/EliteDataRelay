using System;
using System.Collections.Generic;

namespace EliteDataRelay.Models.Journal
{
    public sealed class ProspectedMaterial
    {
        public string Name { get; init; } = string.Empty;
        public string? LocalisedName { get; init; }
        public double Proportion { get; init; }
    }

    public sealed class ProspectedAsteroidEventArgs : EventArgs
    {
        public ProspectedAsteroidEventArgs(
            DateTime timestamp,
            string starSystem,
            string body,
            long? bodyId,
            long? systemAddress,
            string content,
            string? motherlodeMaterial,
            double remaining,
            IReadOnlyList<ProspectedMaterial> materials)
        {
            Timestamp = timestamp;
            StarSystem = starSystem;
            Body = body;
            BodyId = bodyId;
            SystemAddress = systemAddress;
            Content = content;
            MotherlodeMaterial = motherlodeMaterial;
            Remaining = remaining;
            Materials = materials;
        }

        public DateTime Timestamp { get; }
        public string StarSystem { get; }
        public string Body { get; }
        public long? BodyId { get; }
        public long? SystemAddress { get; }
        public string Content { get; }
        public string? MotherlodeMaterial { get; }
        public double Remaining { get; }
        public IReadOnlyList<ProspectedMaterial> Materials { get; }
    }

    public sealed class AsteroidCrackedEventArgs : EventArgs
    {
        public AsteroidCrackedEventArgs(DateTime timestamp, string starSystem, string body, long? bodyId)
        {
            Timestamp = timestamp;
            StarSystem = starSystem;
            Body = body;
            BodyId = bodyId;
        }

        public DateTime Timestamp { get; }
        public string StarSystem { get; }
        public string Body { get; }
        public long? BodyId { get; }
    }

    public sealed class SupercruiseExitEventArgs : EventArgs
    {
        public SupercruiseExitEventArgs(DateTime timestamp, string starSystem, long? systemAddress, string body, long? bodyId, string bodyType)
        {
            Timestamp = timestamp;
            StarSystem = starSystem;
            SystemAddress = systemAddress;
            Body = body;
            BodyId = bodyId;
            BodyType = bodyType;
        }

        public DateTime Timestamp { get; }
        public string StarSystem { get; }
        public long? SystemAddress { get; }
        public string Body { get; }
        public long? BodyId { get; }
        public string BodyType { get; }
    }

    public sealed class SupercruiseEntryEventArgs : EventArgs
    {
        public SupercruiseEntryEventArgs(DateTime timestamp, string starSystem)
        {
            Timestamp = timestamp;
            StarSystem = starSystem;
        }

        public DateTime Timestamp { get; }
        public string StarSystem { get; }
    }

    public sealed class MusicTrackEventArgs : EventArgs
    {
        public MusicTrackEventArgs(DateTime timestamp, string track)
        {
            Timestamp = timestamp;
            Track = track;
        }

        public DateTime Timestamp { get; }
        public string Track { get; }
    }

    public sealed class ShutdownEventArgs : EventArgs
    {
        public ShutdownEventArgs(DateTime timestamp) => Timestamp = timestamp;
        public DateTime Timestamp { get; }
    }

    public sealed class FileheaderEventArgs : EventArgs
    {
        public FileheaderEventArgs(DateTime timestamp) => Timestamp = timestamp;
        public DateTime Timestamp { get; }
    }

    public sealed class MaterialCollectedEventArgs : EventArgs
    {
        public MaterialCollectedEventArgs(DateTime timestamp, string category, string name, string? localisedName, int count)
        {
            Timestamp = timestamp;
            Category = category;
            Name = name;
            LocalisedName = localisedName;
            Count = count;
        }

        public DateTime Timestamp { get; }
        public string Category { get; }
        public string Name { get; }
        public string? LocalisedName { get; }
        public int Count { get; }
    }
}
