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

            // Also update ship info immediately from LoadGame using Ship_Localised if present,
            // so the UI shows a friendly ship name on startup before Loadout arrives.
            if (!string.IsNullOrEmpty(internalShipName))
            {
                string shipType = loadGameEvent.ShipLocalised ?? ShipIconService.GetShipDisplayName(internalShipName);

                // Use any available ShipName/ShipIdent from LoadGame too
                UpdateShipInformation(
                    loadGameEvent.ShipName,
                    loadGameEvent.ShipIdent,
                    shipType,
                    internalShipName,
                    loadGameEvent.ShipLocalised
                );
            }
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

        private void ProcessSetUserShipNameEvent(string journalLine, JsonSerializerOptions options)
        {
            var renameEvent = JsonSerializer.Deserialize<SetUserShipNameEvent>(journalLine, options);
            if (renameEvent == null) return;

            // Use last known type/localised where available, just update name/ident immediately.
            var shipType = _lastShipLocalised ?? _lastShipType ?? (string?)null;
            var internalName = string.IsNullOrEmpty(renameEvent.Ship) ? _lastInternalShipName : renameEvent.Ship;

            UpdateShipInformation(
                renameEvent.UserShipName,
                renameEvent.UserShipIdent,
                shipType,
                internalName,
                _lastShipLocalised
            );
        }

        private void ProcessVehicleSwitchEvent(JsonDocument jsonDoc)
        {
            // Horizons uses VehicleSwitch with a simple target descriptor
            string? to = jsonDoc.RootElement.TryGetProperty("To", out var toEl) ? toEl.GetString() : null;
            if (string.Equals(to, "SRV", System.StringComparison.OrdinalIgnoreCase))
            {
                // Entered SRV — reflect immediately in UI
                UpdateShipInformation("SRV", "", "SRV", "SRV", "SRV");
            }
            else if (string.Equals(to, "Ship", System.StringComparison.OrdinalIgnoreCase))
            {
                // Returned to ship — restore last known ship details
                UpdateShipInformation(_lastShipName, _lastShipIdent, _lastShipType, _lastInternalShipName, _lastShipLocalised);
            }
        }

        private void ProcessEmbarkEvent(JsonDocument jsonDoc)
        {
            // Odyssey uses Embark when boarding a vehicle. It includes flags like SRV/Taxi/Multicrew.
            bool isSrv = jsonDoc.RootElement.TryGetProperty("SRV", out var srvEl) && srvEl.ValueKind == System.Text.Json.JsonValueKind.True;
            bool isTaxi = jsonDoc.RootElement.TryGetProperty("Taxi", out var taxiEl) && taxiEl.ValueKind == System.Text.Json.JsonValueKind.True;
            bool isMulticrew = jsonDoc.RootElement.TryGetProperty("Multicrew", out var mcEl) && mcEl.ValueKind == System.Text.Json.JsonValueKind.True;
            if (isSrv)
            {
                UpdateShipInformation("SRV", "", "SRV", "SRV", "SRV");
            }
            else if (isTaxi)
            {
                UpdateShipInformation("Taxi", "", "Taxi", "Taxi", "Taxi");
            }
            else if (isMulticrew)
            {
                UpdateShipInformation("Multicrew", "", "Multicrew", "Multicrew", "Multicrew");
            }
            else
            {
                // Embarked something that isn't SRV — most likely the ship. Restore ship info.
                UpdateShipInformation(_lastShipName, _lastShipIdent, _lastShipType, _lastInternalShipName, _lastShipLocalised);
            }
        }

        private void ProcessDisembarkEvent(JsonDocument jsonDoc)
        {
            // Odyssey uses Disembark when leaving a vehicle (to on-foot, SRV, taxi etc.).
            bool isSrv = jsonDoc.RootElement.TryGetProperty("SRV", out var srvEl) && srvEl.ValueKind == System.Text.Json.JsonValueKind.True;
            bool isTaxi = jsonDoc.RootElement.TryGetProperty("Taxi", out var taxiEl) && taxiEl.ValueKind == System.Text.Json.JsonValueKind.True;
            bool isMulticrew = jsonDoc.RootElement.TryGetProperty("Multicrew", out var mcEl) && mcEl.ValueKind == System.Text.Json.JsonValueKind.True;
            if (isSrv)
            {
                UpdateShipInformation("SRV", "", "SRV", "SRV", "SRV");
            }
            else if (isTaxi)
            {
                UpdateShipInformation("Taxi", "", "Taxi", "Taxi", "Taxi");
            }
            else if (isMulticrew)
            {
                UpdateShipInformation("Multicrew", "", "Multicrew", "Multicrew", "Multicrew");
            }
            else
            {
                // Most common case: disembark from ship to on-foot.
                UpdateShipInformation("On Foot", "", "On Foot", "OnFoot", "On Foot");
            }
        }

        private void ProcessLaunchSrvEvent()
        {
            UpdateShipInformation("SRV", "", "SRV", "SRV", "SRV");
        }

        private void ProcessDockSrvEvent()
        {
            // Return to ship from SRV
            UpdateShipInformation(_lastShipName, _lastShipIdent, _lastShipType, _lastInternalShipName, _lastShipLocalised);
        }

        private void ProcessSrvDestroyedEvent()
        {
            // SRV destroyed -> back to ship
            UpdateShipInformation(_lastShipName, _lastShipIdent, _lastShipType, _lastInternalShipName, _lastShipLocalised);
        }

        private void ProcessLaunchFighterEvent()
        {
            UpdateShipInformation("Fighter", "", "Fighter", "Fighter", "Fighter");
        }

        private void ProcessDockFighterEvent()
        {
            // Return to mothership
            UpdateShipInformation(_lastShipName, _lastShipIdent, _lastShipType, _lastInternalShipName, _lastShipLocalised);
        }

        private void ProcessFighterDestroyedEvent()
        {
            // Fighter destroyed -> back to ship
            UpdateShipInformation(_lastShipName, _lastShipIdent, _lastShipType, _lastInternalShipName, _lastShipLocalised);
        }
    }
}
