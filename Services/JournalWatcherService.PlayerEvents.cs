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

                // The Loadout event provides the ship's name and ID. We combine this with the
                // ship type we learned from either LoadGame or ShipyardSwap to do a full update.
                UpdateShipInformation(loadoutEvent.ShipName, loadoutEvent.ShipIdent, _lastShipType, loadoutEvent.Ship);
                // We no longer call UpdateShipInformation here, as Loadout's primary job is stats, not identity.
            }
        }

        private void ProcessShipyardSwapEvent(string journalLine, JsonSerializerOptions options)
        {
            var swapEvent = JsonSerializer.Deserialize<ShipyardSwapEvent>(journalLine, options);
            if (swapEvent != null)
            {
                // A ShipyardSwap tells us the new ship's type, but not its name or ID.
                // We update what we know now, which is enough to get the correct ship icon.
                // The subsequent 'Loadout' event will provide the name and ID to complete the update.
                var shipType = !string.IsNullOrEmpty(swapEvent.ShipTypeLocalised) ? swapEvent.ShipTypeLocalised : Capitalize(swapEvent.ShipType);
                _lastShipType = shipType;
                _lastInternalShipName = swapEvent.ShipType; // Cache the internal name for the icon
                Trace.WriteLine($"[JournalWatcherService] Detected ShipyardSwap. New ship type: {shipType}");
                UpdateShipInformation(null, null, shipType, swapEvent.ShipType);
            }
        }

        private void ProcessShipyardNewEvent(string journalLine, JsonSerializerOptions options)
        {
            var newEvent = JsonSerializer.Deserialize<ShipyardNewEvent>(journalLine, options);
            if (newEvent != null)
            {
                // A ShipyardNew event tells us the new ship's type, but not its name or ID.
                // We update what we know now, which is enough to get the correct ship icon.
                // The subsequent 'Loadout' event will provide the name and ID to complete the update.
                var shipType = !string.IsNullOrEmpty(newEvent.ShipTypeLocalised) ? newEvent.ShipTypeLocalised : Capitalize(newEvent.ShipType);
                _lastShipType = shipType;
                _lastInternalShipName = newEvent.ShipType; // Cache the internal name for the icon
                Trace.WriteLine($"[JournalWatcherService] Detected ShipyardNew. New ship type: {shipType}");
                UpdateShipInformation(null, null, shipType, newEvent.ShipType);
            }
        }
    }
}