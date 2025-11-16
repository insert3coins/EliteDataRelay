using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Models.Mining
{
    public enum MiningContent
    {
        Low,
        Medium,
        High
    }

    public sealed record MiningProspector(IReadOnlyList<MiningMaterial> Materials, MiningContent Content, string? MotherlodeMaterial, double Remaining);
}
