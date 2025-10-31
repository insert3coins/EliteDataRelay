# Elite Data Relay — Release Notes v0.60.9

## Highlights (Exploration)

- Full FSS coverage and correct totals
  - Supports FSSDiscoveryScan, FSSAllBodiesFound, NavBeaconScan, and legacy DiscoveryScan.
  - “FSS: Complete” shown reliably with body totals.

- Accurate scanned/mapped display
  - Filters out barycentres, belt clusters, and rings from the scanned display to match in‑game expectations.
  - Shows “All scanned / All mapped” completion badges (planet‑class whitelist for mappability).

- Signals aligned with the game
  - “Signals” uses FSS NonBodyCount when available (matches “Signals detected” in game).
  - Falls back to distinct signal types if the FSS count isn’t present yet.

- Biological surfacing
  - Captures biological Codex entries and shows a count in the overlay/tab.

- Persistence
  - New SQLite persistence for system‑level non‑body signals and biological Codex entries.
  - Values restored on load for overlays and the Exploration tab.

- Web overlay enhancements
  - Correct FSS percentage (no double scaling).
  - Added Completion / Signals / Codex rows to the `/exploration` page.

- Importer updates
  - Backfills FSS totals, signals, Codex (bio), exobiology, first footfall, SAA from historical journals.
  - Runs with UI events suppressed and background writer paused for smooth imports.

## Desktop Overlay

- Size increased to 360×255 for readability.
- Lines shown: System, FSS status, Scanned, Mapped, Completion, Signals, Codex, Session summary.

## Quick Tips

- Start monitoring, jump into a new system, and run FSS to 100%.
- “Signals: N” should match in‑game “Signals detected”.
- Mapped count and “All mapped” reflect only DSS‑mappable planet classes.

## Paths / Endpoints

- Settings: `%APPDATA%\EliteDataRelay\settings.json`
- Exploration DB: `%APPDATA%\EliteDataRelay\exploration.db`
- Web overlay endpoints: `http://localhost:9005/info`, `/cargo`, `/ship-icon`, `/exploration`

