using EliteDataRelay.Models;
using System.Diagnostics;
using System.Text.Json;

namespace EliteDataRelay.Services

{
    public partial class JournalWatcherService
    {
        private void ProcessLoadGameEvent(JsonDocument jsonDoc, string journalLine, JsonSerializerOptions options)
        {
            var loadGameEvent = JsonSerializer.Deserialize<LoadGameEvent>(journalLine, options);
            if (loadGameEvent == null) return;

            // Check and update commander name
            if (!string.IsNullOrEmpty(loadGameEvent.Commander) && loadGameEvent.Commander != _lastCommanderName)
            {
                _lastCommanderName = loadGameEvent.Commander; // This line was missing
                Trace.WriteLine($"[JournalWatcherService] Found Commander Name: {_lastCommanderName}");
                CommanderNameChanged?.Invoke(this, new CommanderNameChangedEventArgs(_lastCommanderName));
            }

            // Get the internal ship name first, as it's not in the strongly-typed model.
    var root = jsonDoc.RootElement;
    string? internalShipName = root.TryGetProperty("Ship", out var shipProp) ? shipProp.GetString() : null;

    // The ship type is in "Ship_Localised". If it's missing, fall back to the last known type.
    string? shipType = root.TryGetProperty("Ship_Localised", out var shipLocProp) ? shipLocProp.GetString() : _lastShipType;

            var shipName = loadGameEvent.ShipName;
            var shipIdent = loadGameEvent.ShipIdent;
    UpdateShipInformation(shipName, shipIdent, shipType, internalShipName);
        }

        private void ProcessLoadoutEvent(string journalLine, JsonSerializerOptions options)
        {
            var loadoutEvent = JsonSerializer.Deserialize<ShipLoadout>(journalLine, options);
            if (loadoutEvent != null)
            {
                Trace.WriteLine($"[JournalWatcher] Processed Loadout event. Ship: {loadoutEvent.Ship}, Mass: {loadoutEvent.UnladenMass}, Rebuy: {loadoutEvent.Rebuy}");
                if (loadoutEvent.CargoCapacity > 0)
                {
                    Trace.WriteLine($"[JournalWatcherService] Found Loadout event. CargoCapacity: {loadoutEvent.CargoCapacity}");
                    CargoCapacityChanged?.Invoke(this, new CargoCapacityEventArgs(loadoutEvent.CargoCapacity));
                }

                LoadoutChanged?.Invoke(this, new LoadoutChangedEventArgs(loadoutEvent));

                // The Loadout event is the most reliable source for the current ship.
                // We can derive the ship type from its internal name if _lastShipType is not set (e.g., on startup).
                var shipType = _lastShipType ?? ShipIconService.GetShipDisplayName(loadoutEvent.Ship);
                UpdateShipInformation(loadoutEvent.ShipName, loadoutEvent.ShipIdent, shipType, loadoutEvent.Ship);
            }
        }

        private void ProcessShipyardSwapEvent(string journalLine, JsonSerializerOptions options)
        {
            var swapEvent = JsonSerializer.Deserialize<ShipyardSwapEvent>(journalLine, options);
            if (swapEvent != null)
            {
                var newShipType = !string.IsNullOrEmpty(swapEvent.ShipTypeLocalised) ? swapEvent.ShipTypeLocalised : Capitalize(swapEvent.ShipType);
                _lastShipType = newShipType;
                Trace.WriteLine($"[JournalWatcherService] Detected ShipyardSwap. New ship type: {newShipType}");
            }
        }
    }
}