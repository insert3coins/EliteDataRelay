using EliteDataRelay.Models;
using System.Diagnostics;
using System.Text.Json;

namespace EliteDataRelay.Services

{
    public partial class JournalWatcherService
    {
        private static string BuildSuitDisplay(string? suitInternalOrLocalised, string? loadoutName)
        {
            string suit = suitInternalOrLocalised ?? string.Empty;
            // Map common internals to display names
            if (suit.Equals("explorationsuit", StringComparison.OrdinalIgnoreCase) || suit.Equals("artemis", StringComparison.OrdinalIgnoreCase)) suit = "Artemis";
            else if (suit.Equals("utilitysuit", StringComparison.OrdinalIgnoreCase) || suit.Equals("maverick", StringComparison.OrdinalIgnoreCase)) suit = "Maverick";
            else if (suit.Equals("tacticalsuit", StringComparison.OrdinalIgnoreCase) || suit.Equals("dominator", StringComparison.OrdinalIgnoreCase)) suit = "Dominator";
            else if (string.IsNullOrWhiteSpace(suit)) suit = "Suit";

            if (!string.IsNullOrWhiteSpace(loadoutName))
            {
                return $"On Foot ({suit} – {loadoutName})";
            }
            return $"On Foot ({suit})";
        }

        private void ProcessSuitLoadoutEvent(JsonDocument jsonDoc)
        {
            var root = jsonDoc.RootElement;
            string? suit = null;
            string? suitLocalised = null;
            string? loadout = null;
            if (root.TryGetProperty("Suit", out var s)) suit = s.GetString();
            if (root.TryGetProperty("Suit_Localised", out var sl)) suitLocalised = sl.GetString();
            if (root.TryGetProperty("LoadoutName", out var ln)) loadout = ln.GetString();
            var display = BuildSuitDisplay(suitLocalised ?? suit, loadout);
            RaiseTransientShipInfo(display, string.Empty, display, "OnFoot", display);
        }

        private void ProcessSwitchSuitEvent(JsonDocument jsonDoc)
        {
            var root = jsonDoc.RootElement;
            string? to = null;
            string? toLocalised = null;
            if (root.TryGetProperty("ToSuit", out var ts)) to = ts.GetString();
            if (root.TryGetProperty("ToSuit_Localised", out var tsl)) toLocalised = tsl.GetString();
            var display = BuildSuitDisplay(toLocalised ?? to, null);
            RaiseTransientShipInfo(display, string.Empty, display, "OnFoot", display);
        }
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
                string shipType = loadGameEvent.ShipLocalised ?? ShipNameHelper.GetDisplayName(internalShipName);

                // Use any available ShipName/ShipIdent from LoadGame too
                UpdateShipInformation(
                    loadGameEvent.ShipName,
                    loadGameEvent.ShipIdent,
                    shipType,
                    internalShipName,
                    loadGameEvent.ShipLocalised
                );

                // Seed mothership cache; Loadout will refine shortly
                _homeShipName = _lastShipName;
                _homeShipIdent = _lastShipIdent;
                _homeShipType = _lastShipType;
                _homeInternalShipName = _lastInternalShipName;
                _homeShipLocalised = _lastShipLocalised;
            }
        }

        private void ProcessLoadoutEvent(string journalLine, JsonSerializerOptions options)
        {
            var loadoutEvent = JsonSerializer.Deserialize<ShipLoadout>(journalLine, options);
            if (loadoutEvent != null)
            {
                Trace.WriteLine($"[JournalWatcher] Processed Loadout event. Ship: {loadoutEvent.Ship}, Mass: {loadoutEvent.UnladenMass}, Rebuy: {loadoutEvent.Rebuy}");
                loadoutEvent.RawJson = journalLine;
                if (loadoutEvent.CargoCapacity > 0)
                {
                    Trace.WriteLine($"[JournalWatcherService] Found Loadout event. CargoCapacity: {loadoutEvent.CargoCapacity}");
                    CargoCapacityChanged?.Invoke(this, new CargoCapacityEventArgs(loadoutEvent.CargoCapacity));
                }

                LoadoutChanged?.Invoke(this, new LoadoutChangedEventArgs(loadoutEvent));

                // The Loadout event is the primary source of truth for ship identity.
                // It provides the internal name, custom name, and ID. We can now derive the display name.
                // Prioritize the localized name from the event, then the cached name from a swap, then fallback to the service.
                string shipType = loadoutEvent.ShipLocalised ?? _lastShipType ?? ShipNameHelper.GetDisplayName(loadoutEvent.Ship);

                UpdateShipInformation(loadoutEvent.ShipName, loadoutEvent.ShipIdent, shipType, loadoutEvent.Ship, loadoutEvent.ShipLocalised ?? _lastShipLocalised);

                // Persist mothership details from authoritative Loadout
                _homeShipName = _lastShipName;
                _homeShipIdent = _lastShipIdent;
                _homeShipType = _lastShipType;
                _homeInternalShipName = _lastInternalShipName;
                _homeShipLocalised = _lastShipLocalised;
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
                _lastShipType = swapEvent.ShipTypeLocalised ?? ShipNameHelper.GetDisplayName(swapEvent.ShipType);
                _lastShipLocalised = swapEvent.ShipTypeLocalised;
                _lastInternalShipName = swapEvent.ShipType;
                // Update mothership baseline (name/ident will be filled by next Loadout)
                _homeShipType = _lastShipType;
                _homeShipLocalised = _lastShipLocalised;
                _homeInternalShipName = _lastInternalShipName;
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
                _lastShipType = newEvent.ShipTypeLocalised ?? ShipNameHelper.GetDisplayName(newEvent.ShipType);
                _lastShipLocalised = newEvent.ShipTypeLocalised;
                _lastInternalShipName = newEvent.ShipType;
                // Update mothership baseline (name/ident will be filled by next Loadout)
                _homeShipType = _lastShipType;
                _homeShipLocalised = _lastShipLocalised;
                _homeInternalShipName = _lastInternalShipName;
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
                // Entered SRV — reflect immediately in UI without clobbering mothership cache
                RaiseTransientShipInfo("SRV", string.Empty, "SRV", "SRV", "SRV");
            }
            else if (string.Equals(to, "Ship", System.StringComparison.OrdinalIgnoreCase))
            {
                // Returned to ship — restore last known ship details
                RaiseTransientShipInfo(_homeShipName, _homeShipIdent, _homeShipType, _homeInternalShipName, _homeShipLocalised);
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
                RaiseTransientShipInfo("SRV", string.Empty, "SRV", "SRV", "SRV");
            }
            else if (isTaxi)
            {
                RaiseTransientShipInfo("Taxi", string.Empty, "Taxi", "Taxi", "Taxi");
            }
            else if (isMulticrew)
            {
                RaiseTransientShipInfo("Multicrew", string.Empty, "Multicrew", "Multicrew", "Multicrew");
            }
            else
            {
                // Embarked something that isn't SRV — most likely the ship. Restore ship info.
                RaiseTransientShipInfo(_homeShipName, _homeShipIdent, _homeShipType, _homeInternalShipName, _homeShipLocalised);
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
                RaiseTransientShipInfo("SRV", string.Empty, "SRV", "SRV", "SRV");
            }
            else if (isTaxi)
            {
                RaiseTransientShipInfo("Taxi", string.Empty, "Taxi", "Taxi", "Taxi");
            }
            else if (isMulticrew)
            {
                RaiseTransientShipInfo("Multicrew", string.Empty, "Multicrew", "Multicrew", "Multicrew");
            }
            else
            {
                // Most common case: disembark from ship to on-foot.
                RaiseTransientShipInfo("On Foot", string.Empty, "On Foot", "OnFoot", "On Foot");
            }
        }

        private void ProcessLaunchSrvEvent()
        {
            RaiseTransientShipInfo("SRV", string.Empty, "SRV", "SRV", "SRV");
        }

        private void ProcessDockSrvEvent()
        {
            // Return to ship from SRV
            RaiseTransientShipInfo(_homeShipName, _homeShipIdent, _homeShipType, _homeInternalShipName, _homeShipLocalised);
        }

        private void ProcessSrvDestroyedEvent()
        {
            // SRV destroyed -> back to ship
            RaiseTransientShipInfo(_homeShipName, _homeShipIdent, _homeShipType, _homeInternalShipName, _homeShipLocalised);
        }

        private void ProcessLaunchFighterEvent()
        {
            RaiseTransientShipInfo("Fighter", string.Empty, "Fighter", "Fighter", "Fighter");
        }

        private void ProcessDockFighterEvent()
        {
            // Return to mothership
            RaiseTransientShipInfo(_homeShipName, _homeShipIdent, _homeShipType, _homeInternalShipName, _homeShipLocalised);
        }

        private void ProcessFighterDestroyedEvent()
        {
            // Fighter destroyed -> back to ship
            RaiseTransientShipInfo(_homeShipName, _homeShipIdent, _homeShipType, _homeInternalShipName, _homeShipLocalised);
        }

        // Raise a ShipInfoChanged event without mutating the cached mothership/last ship fields.
        private void RaiseTransientShipInfo(string? shipName, string? shipIdent, string? shipType, string? internalShipName, string? shipLocalised)
        {
            ShipInfoChanged?.Invoke(this, new ShipInfoChangedEventArgs(shipName ?? "N/A", shipIdent ?? "N/A", shipLocalised ?? shipType ?? "Unknown", internalShipName ?? "unknown"));
        }
    }
}

