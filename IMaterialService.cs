using EliteDataRelay.Models;
using System;
using System.Collections.Generic;

namespace EliteDataRelay.Services
{
    public interface IMaterialService
    {
        event EventHandler? MaterialsUpdated;
        IReadOnlyDictionary<string, MaterialItem> RawMaterials { get; }
        IReadOnlyDictionary<string, MaterialItem> ManufacturedMaterials { get; }
        IReadOnlyDictionary<string, MaterialItem> EncodedMaterials { get; }
        void Start();
        void Stop();
    }
}