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

            // Check and update ship info
            if (!string.IsNullOrEmpty(loadGameEvent.Commander) && loadGameEvent.Commander != _lastCommanderName)
            {
                _lastCommanderName = loadGameEvent.Commander;
                Debug.WriteLine($"[JournalWatcherService] Found Commander Name: {_lastCommanderName}");
                CommanderNameChanged?.Invoke(this, new CommanderNameChangedEventArgs(_lastCommanderName));
            }

            // Get the internal ship name first, as it's not in the strongly-typed model.
            string? internalShipName = jsonDoc.RootElement.TryGetProperty("Ship", out var shipProp) ? shipProp.GetString() : null;

            // Get the ship type, fallback to the non-localised name if needed.
            var shipType = !string.IsNullOrEmpty(loadGameEvent.ShipLocalised) ? loadGameEvent.ShipLocalised
                : Capitalize(internalShipName);
            var shipName = loadGameEvent.ShipName;
            var shipIdent = loadGameEvent.ShipIdent;
            UpdateShipInformation(shipName, shipIdent, shipType, internalShipName);

            // Also update the balance from the LoadGame event
            if (loadGameEvent.Credits != _lastKnownBalance)
            {
                _lastKnownBalance = loadGameEvent.Credits;
                BalanceChanged?.Invoke(this, new BalanceChangedEventArgs(_lastKnownBalance));
            }
        }

        private void ProcessLoadoutEvent(string journalLine, JsonSerializerOptions options)
        {
            var loadoutEvent = JsonSerializer.Deserialize<ShipLoadout>(journalLine, options);
            if (loadoutEvent != null)
            {
                if (loadoutEvent.CargoCapacity > 0)
                {
                    Debug.WriteLine($"[JournalWatcherService] Found Loadout event. CargoCapacity: {loadoutEvent.CargoCapacity}");
                    CargoCapacityChanged?.Invoke(this, new CargoCapacityEventArgs(loadoutEvent.CargoCapacity));
                }

                LoadoutChanged?.Invoke(this, new LoadoutChangedEventArgs(loadoutEvent));
                UpdateShipInformation(loadoutEvent.ShipName, loadoutEvent.ShipIdent, _lastShipType, loadoutEvent.Ship);
            }
        }

        private void ProcessShipyardSwapEvent(string journalLine, JsonSerializerOptions options)
        {
            var swapEvent = JsonSerializer.Deserialize<ShipyardSwapEvent>(journalLine, options);
            if (swapEvent != null)
            {
                var newShipType = !string.IsNullOrEmpty(swapEvent.ShipTypeLocalised) ? swapEvent.ShipTypeLocalised : Capitalize(swapEvent.ShipType);
                _lastShipType = newShipType;
                Debug.WriteLine($"[JournalWatcherService] Detected ShipyardSwap. New ship type: {newShipType}");
            }
        }
    }
}