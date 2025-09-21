using EliteDataRelay.Services;
using EliteDataRelay.UI;

namespace EliteDataRelay
{
    /// <summary>
    /// A simple container to manage the creation and injection of services.
    /// This ensures that all components of the application are wired up correctly.
    /// </summary>
    public class DependencyContainer
    {
        public CargoForm CreateMainForm()
        {
            // Create instances of all services. Note that some services depend on others.
            var journalWatcherService = new JournalWatcherService();
            var statusWatcherService = new StatusWatcherService();
            var fileMonitoringService = new FileMonitoringService();
            var cargoProcessorService = new CargoProcessorService();
            var soundService = new SoundService();
            var fileOutputService = new FileOutputService();
            var materialService = new MaterialService(journalWatcherService);
            var visitedSystemsService = new VisitedSystemsService(journalWatcherService);
            var sessionTrackingService = new SessionTrackingService(cargoProcessorService, statusWatcherService);
            var shipLoadoutService = new ShipLoadoutService(journalWatcherService);
            var cargoFormUI = new CargoFormUI();

            // Create the main form, injecting all the required services.
            return new CargoForm(
                fileMonitoringService,
                cargoProcessorService,
                journalWatcherService,
                soundService,
                fileOutputService,
                cargoFormUI,
                statusWatcherService,
                visitedSystemsService,
                materialService,
                sessionTrackingService,
                shipLoadoutService
            );
        }
    }
}