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
            
            // The LoadGame event gives us the internal name. We cache it here.
            // The subsequent Loadout event will use this to provide the full ship details.
            _lastInternalShipName = internalShipName;
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

                // The Loadout event is the primary source of truth for ship identity.
                // It provides the internal name, custom name, and ID. We can now derive the display name.
                // Prioritize the localized name from the event, then the cached name from a swap, then fallback to the service.
                string shipType = loadoutEvent.ShipLocalised ?? _lastShipType ?? ShipIconService.GetShipDisplayName(loadoutEvent.Ship);

                UpdateShipInformation(loadoutEvent.ShipName, loadoutEvent.ShipIdent, shipType, loadoutEvent.Ship, loadoutEvent.ShipLocalised ?? _lastShipLocalised);
            }
        }

        private void ProcessShipyardSwapEvent(string journalLine, JsonSerializerOptions options)
        {
            var swapEvent = JsonSerializer.Deserialize<ShipyardSwapEvent>(journalLine, options);
            if (swapEvent != null)
            {
                // A ShipyardSwap tells us the new ship's type, but not its name or ID.
                // We update what we know now, which is enough to get the correct ship icon.
                // We cache the localized name here, so the subsequent 'Loadout' event can use it.
                _lastShipType = swapEvent.ShipTypeLocalised ?? ShipIconService.GetShipDisplayName(swapEvent.ShipType);
                _lastShipLocalised = swapEvent.ShipTypeLocalised;
                _lastInternalShipName = swapEvent.ShipType;
                // We only update the internal name for now to trigger an icon change, but wait for Loadout for the full UI update.
                ShipInfoChanged?.Invoke(this, new ShipInfoChangedEventArgs("...", "...", _lastShipType, swapEvent.ShipType));
                Trace.WriteLine($"[JournalWatcherService] Detected ShipyardSwap. New ship type: {swapEvent.ShipType}");
            }
        }

        private void ProcessShipyardNewEvent(string journalLine, JsonSerializerOptions options)
        {
            var newEvent = JsonSerializer.Deserialize<ShipyardNewEvent>(journalLine, options);
            if (newEvent != null)
            {
                // A ShipyardNew event tells us the new ship's type, but not its name or ID.
                // We update what we know now, which is enough to get the correct ship icon.
                // We cache the localized name here, so the subsequent 'Loadout' event can use it.
                _lastShipType = newEvent.ShipTypeLocalised ?? ShipIconService.GetShipDisplayName(newEvent.ShipType);
                _lastShipLocalised = newEvent.ShipTypeLocalised;
                _lastInternalShipName = newEvent.ShipType;
                // We only update the internal name for now to trigger an icon change, but wait for Loadout for the full UI update.
                ShipInfoChanged?.Invoke(this, new ShipInfoChangedEventArgs("...", "...", _lastShipType, newEvent.ShipType));
                Trace.WriteLine($"[JournalWatcherService] Detected ShipyardNew. New ship type: {newEvent.ShipType}");
            }
        }
    }
}