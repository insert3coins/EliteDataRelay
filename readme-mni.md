# Elite Data Relay — Mini Release Notes

## Highlights
- One‑time Exploration History Import now fully seeds the database and activates the Exploration view reliably after completion.
- Exploration UI and overlays refresh immediately after import; the “No System Selected” state is resolved automatically.

## Fixes & Improvements
- Import completion popup: shows a small window popup (“Exploration History Import”) instead of a tray balloon.
- Current system activation after import:
  - Uses the app’s current Location event (name + SystemAddress when available).
  - Falls back to resolving by system name from the database if the address is missing.
  - Final fallback: most‑recent visited system from the database.
  - Forces the “Current System” tab header to update immediately.
- Exploration Log time display: extended beyond 1 day.
  - Shows minutes, hours, days (up to 60), then months and years for older entries.
- Overlay/web overlay sync: when monitoring is active, exploration data and session stats are pushed to overlays and the web overlay immediately after import.
- Safer, smoother import: UI events suppressed and async DB writer paused during import to avoid cross‑thread UI churn and freezes.

## Exploration History Import
- Runs automatically once after update; scans historical `Journal.*.log`.
- Preserves journal timestamps:
  - `LastVisited` uses the original journal event time.
  - `FirstVisited` captures the first event seen for that system.
- Safe re‑runs: updates existing rows without duplicating bodies.

Re‑run the import
1) Close Elite Data Relay.
2) Edit `%APPDATA%\EliteDataRelay\settings.json` and set `"ExplorationHistoryImported": false`.
3) (Optional) Delete `%APPDATA%\EliteDataRelay\exploration.db` to rebuild from scratch.
4) Start the app; the importer runs again on startup.

## Paths
- Settings: `%APPDATA%\EliteDataRelay\settings.json`
- Exploration DB: `%APPDATA%\EliteDataRelay\exploration.db`
- Logs: `%APPDATA%\EliteDataRelay\debug_log.txt`, `%APPDATA%\EliteDataRelay\crash_log.txt`

## Notes
- Web overlay endpoints (optional): `http://localhost:9005/info`, `/cargo`, `/ship-icon`, `/exploration`.
- Overlays are draggable and configurable; use “Reposition Overlays” in Settings.
