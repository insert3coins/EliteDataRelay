using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;

namespace EliteDataRelay.Services
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
        /// <returns>The formatted string that was written to the file.</returns>
        public string WriteCargoSnapshot(CargoSnapshot snapshot, int? cargoCapacity)
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
                return cargoString;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FileOutputService] Error writing cargo snapshot: {ex}");
            }
            return string.Empty;
        }

        /// <summary>
        /// Format the cargo snapshot into a readable string format
        /// </summary>
        /// <param name="snapshot">The cargo snapshot to format</param>
        /// <param name="cargoCapacity">The total cargo capacity, if known</param>
        /// <returns>Formatted cargo string</returns>
        private string FormatCargoString(CargoSnapshot snapshot, int? cargoCapacity)
        {
            string countSlashCapacity = cargoCapacity.HasValue
                ? $"{snapshot.Count}/{cargoCapacity.Value}"
                : snapshot.Count.ToString();
    
            string singleLineItems = string.Join(
                " ",
                snapshot.Inventory.Select(item =>
                    $"{(string.IsNullOrEmpty(item.Localised) ? item.Name : item.Localised)} ({item.Count})"));
    
            string multiLineItems = string.Join(
                Environment.NewLine,
                snapshot.Inventory.Select(item =>
                    $"- {(string.IsNullOrEmpty(item.Localised) ? item.Name : item.Localised)}: {item.Count}"));
    
            var outputString = AppConfiguration.OutputFileFormat
                .Replace("{count}", snapshot.Count.ToString())
                .Replace("{capacity}", cargoCapacity.HasValue ? cargoCapacity.Value.ToString() : "")
                .Replace("{count_slash_capacity}", countSlashCapacity)
                .Replace("{items}", singleLineItems)
                .Replace("{items_multiline}", multiLineItems)
                .Replace("\\n", Environment.NewLine);
    
            return outputString;
        }
    }
}