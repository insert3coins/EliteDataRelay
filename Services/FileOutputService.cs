using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EliteCargoMonitor.Configuration;
using EliteCargoMonitor.Models;

namespace EliteCargoMonitor.Services
{
    /// <summary>
    /// Service for writing cargo data to output files
    /// </summary>
    public class FileOutputService : IFileOutputService
    {
        /// <summary>
        /// Write the cargo snapshot data to the output file
        /// </summary>
        /// <param name="snapshot">The cargo snapshot to write</param>
        /// <param name="cargoCapacity">The total cargo capacity, if known</param>
        public void WriteCargoSnapshot(CargoSnapshot snapshot, int? cargoCapacity)
        {
            try
            {
                var outputPath = Path.Combine(AppConfiguration.OutputDirectory, AppConfiguration.OutputFileName);
                
                // Ensure output directory exists
                if (!Directory.Exists(AppConfiguration.OutputDirectory))
                {
                    Directory.CreateDirectory(AppConfiguration.OutputDirectory);
                    Debug.WriteLine($"[FileOutputService] Created output directory: {AppConfiguration.OutputDirectory}");
                }

                // Format cargo string similar to original implementation
                string cargoString = FormatCargoString(snapshot, cargoCapacity);

                // Write to file
                File.WriteAllText(outputPath, cargoString);
                
                Debug.WriteLine($"[FileOutputService] Written cargo data to: {outputPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FileOutputService] Error writing cargo snapshot: {ex}");
            }
        }

        /// <summary>
        /// Format the cargo snapshot into a readable string format
        /// </summary>
        /// <param name="snapshot">The cargo snapshot to format</param>
        /// <param name="cargoCapacity">The total cargo capacity, if known</param>
        /// <returns>Formatted cargo string</returns>
        private string FormatCargoString(CargoSnapshot snapshot, int? cargoCapacity)
        {
            string capacityString = cargoCapacity.HasValue ? $"/{cargoCapacity.Value}" : "";
            string cargoString = $"Total Cargo: {snapshot.Count}{capacityString} ";

            cargoString += string.Join(
                " ",
                snapshot.Inventory.Select(item =>
                    $"{(string.IsNullOrEmpty(item.Localised) ? item.Name : item.Localised)} ({item.Count})"));

            return cargoString;
        }
    }
}