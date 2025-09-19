using EliteDataRelay.Models;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Interface for writing cargo data to output files
    /// </summary>
    public interface IFileOutputService
    {
        /// <summary>
        /// Write the cargo snapshot data to the output file
        /// </summary>
        /// <param name="snapshot">The cargo snapshot to write</param>
        /// <param name="cargoCapacity">The total cargo capacity, if known</param>
        string WriteCargoSnapshot(CargoSnapshot snapshot, int? cargoCapacity);
    }
}