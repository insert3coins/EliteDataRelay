using System;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay
{
    public partial class CargoForm
    {
        #region Service Event Handlers

        private void OnFileChanged(object? sender, EventArgs e)
        {
            // Delegate file processing to the cargo processor service
            _cargoProcessorService.ProcessCargoFile();
        }

        private void OnCargoProcessed(object? sender, CargoProcessedEventArgs e)
        {
            _lastCargoSnapshot = e.Snapshot;
            // --- File Output ---
            // If enabled in settings, write the snapshot to the output text file.
            if (AppConfiguration.EnableFileOutput)
            {
                _fileOutputService.WriteCargoSnapshot(e.Snapshot, _cargoCapacity);
            }

            int totalCount = e.Snapshot.Inventory.Sum(item => item.Count);

            // Update the header label in the button panel
            _cargoFormUI.UpdateCargoHeader(totalCount, _cargoCapacity);

            // Update the main window display with the new list view
            _cargoFormUI.UpdateCargoList(e.Snapshot);

            // Update the visual cargo size indicator
            _cargoFormUI.UpdateCargoDisplay(e.Snapshot, _cargoCapacity);
        }

        private void OnCargoCapacityChanged(object? sender, CargoCapacityEventArgs e)
        {
            _cargoCapacity = e.CargoCapacity;
        }

        private void OnBalanceChanged(object? sender, BalanceChangedEventArgs e)
        {
            _lastBalance = e.Balance;
            _cargoFormUI.UpdateBalance(e.Balance);
        }

        private void OnCommanderNameChanged(object? sender, CommanderNameChangedEventArgs e)
        {
            _lastCommanderName = e.CommanderName;
            _cargoFormUI.UpdateCommanderName(e.CommanderName);
        }

        private void OnShipInfoChanged(object? sender, ShipInfoChangedEventArgs e)
        {
            _lastShipName = e.ShipName;
            _lastShipIdent = e.ShipIdent;
            _lastShipType = e.ShipType;
            _cargoFormUI.UpdateShipInfo(e.ShipName, e.ShipIdent, e.ShipType);
        }

        private void OnMaterialsUpdated(object? sender, EventArgs e)
        {
            _lastMaterialServiceCache = _materialService;
            Invoke(new Action(() =>
            {
                _cargoFormUI.UpdateMaterialList(_materialService);
                _cargoFormUI.UpdateMaterialsOverlay(_materialService);
            }));
        }

        private void OnSessionUpdated(object? sender, EventArgs e)
        {
            if (sender is not SessionTrackingService tracker) return;

            if (AppConfiguration.EnableSessionTracking && AppConfiguration.ShowSessionOnOverlay)
            {
                Invoke(new Action(() =>
                {
                    _cargoFormUI.UpdateSessionOverlay(tracker.TotalCargoCollected, tracker.CreditsEarned);
                }));
            }
        }

        private void OnSystemsUpdated(object? sender, EventArgs e)
        {
            _lastVisitedSystems = _visitedSystemsService.VisitedSystems;
            Invoke(new Action(() =>
            {
                if (_lastVisitedSystems != null)
                {
                    _cargoFormUI.UpdateStarMap(_lastVisitedSystems, _lastLocation ?? string.Empty);
                    _cargoFormUI.UpdateStarMapAutocomplete(_lastVisitedSystems);
                }
            }));
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            _lastLocation = e.StarSystem;
            Invoke(new Action(() =>
            {
                _cargoFormUI.UpdateLocation(_lastLocation);
                if (_lastVisitedSystems != null)
                {
                    // Set the current system and center the map in a single operation to prevent redraw artifacts.
                    _cargoFormUI.SetAndCenterStarMapOnSystem(_lastLocation);
                }
            }));
        }

        private void OnJournalScanCompleted(object? sender, JournalScanCompletedEventArgs e)
        {
            // This event is raised from a background thread, so we must invoke on the UI thread.
            Invoke(new Action(() =>
            {
                if (e.Success)
                {
                    MessageBox.Show(this,
                        $"Journal scan complete.\n\nFiles Scanned: {e.FilesScanned}\nNew Systems Found: {e.NewSystemsFound}\nNew Bodies Found: {e.NewBodiesFound}",
                        "Scan Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this,
                        $"The journal scan failed.\n\nError: {e.ErrorMessage}",
                        "Scan Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }));
        }

        private void OnJournalScanProgressed(object? sender, JournalScanProgressEventArgs e)
        {
            Invoke(new Action(() =>
            {
                int percentage = e.TotalFiles > 0 ? (int)((double)e.FilesProcessed / e.TotalFiles * 100) : 0;
                _cargoFormUI.UpdateScanProgress(percentage, $"Scanning file {e.FilesProcessed} of {e.TotalFiles}...");
            }));
        }

        #endregion
    }
}