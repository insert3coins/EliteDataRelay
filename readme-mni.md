# Elite Data Relay — Exploration History Import (mini)

## What’s New
- One‑time historical journal import for the Exploration tab.
- On first launch after update, it scans your old `Journal.*.log` files and populates the exploration database.
- Runs automatically and silently on startup (no UI needed); refreshes the Exploration log when finished.

## What Gets Imported
- System context: `FSDJump`, `Location`, `CarrierJump` (sets current system + visit time).
- Exploration events: `FSSDiscoveryScan`, `Scan`, `SAAScanComplete`, `FSSBodySignals`, `SAASignalsFound`, `SellExplorationData`.
- Timestamps preserved:
  - `LastVisited` uses the journal event time during import.
  - `FirstVisited` is set from the first event seen for that system.
  - `LastUpdated` for exploration events uses the original journal timestamps during import.

## Where Data Lives
- Settings: `%APPDATA%\EliteDataRelay\settings.json`.
- Exploration DB: `%APPADATA%\EliteDataRelay\exploration.db`.

## One‑Time Behavior
- Controlled by `ExplorationHistoryImported` in `settings.json`.
- After a successful import this flag is set to `true` and the importer will not run again automatically.

## Re‑Run The Import
1) Close Elite Data Relay.
2) Edit `%APPDATA%\EliteDataRelay\settings.json` and set `"ExplorationHistoryImported": false`.
3) (Optional) Delete `%APPDATA%\EliteDataRelay\exploration.db` to rebuild from scratch.
4) Start the app; the importer will run again on startup.

Notes:
- The database uses keys per system/body, so re‑import is safe; it updates existing rows without duplicating bodies.

## Performance and Stability
- Import runs in the background, suppressing UI events to avoid cross‑thread updates.
- The async DB writer is paused during import to keep writes single‑threaded and avoid freezes.

## If Nothing Imports
- If the journal folder isn’t found, the importer skips work and marks as done to avoid retrying every launch.
- To try again later: ensure journals are present, then set `ExplorationHistoryImported` back to `false` and restart.
