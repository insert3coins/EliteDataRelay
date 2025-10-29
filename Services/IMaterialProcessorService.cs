using System;
using EliteDataRelay.Models;

namespace EliteDataRelay.Services
{
    public interface IMaterialProcessorService
    {
        /// <summary>
        /// Event raised when new material data has been successfully processed.
        /// </summary>
        event EventHandler<MaterialsProcessedEventArgs>? MaterialsProcessed;

        /// <summary>
        /// Processes the Materials.json file and extracts the material inventory.
        /// </summary>
        bool ProcessMaterialsFile(bool force = false);

        /// <summary>
        /// Resets the internal state of the service.
        /// </summary>
        void Reset();
    }
}
