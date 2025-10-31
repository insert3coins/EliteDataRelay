# Elite Data Relay — Mini Release Notes

## What’s New (Exploration)

- Accurate FSS completion and totals
  - Handles FSSDiscoveryScan, FSSAllBodiesFound, NavBeaconScan, and legacy DiscoveryScan.
  - Shows completion badges: “All scanned” and “All mapped”.

- Mappability parity
  - Uses a planet‑class whitelist to determine DSS‑mappable bodies (matches game intent).

- Signals and Codex
  - Aggregates non‑body signals (USS/POI) per system and shows counts in UI/overlays.
  - Records biological Codex entries; shows count in UI/overlays.

- Overlays updates
  - Desktop overlay: FSS %, mapped/scanned, completion badges, Signals (count only), Codex bio count.
  - Web overlay: fixed FSS percent (no double scaling) and added Completion/Signals/Codex rows.

- Persistence
  - New SQLite tables persist system‑level signals and biological Codex entries.
  - Data is restored on load and visible in UI/overlays.

- Importer
  - Backfills FSS totals, signals, Codex bio, exobiology, first footfall, and SAA from historical journals.
  - Runs with UI events suppressed and background writer paused for smooth imports.

## Quick Tips

- Start monitoring, jump into a new system, run FSS: the overlay/tab should flip to “FSS: Complete” when done and show Signals if found.
- Map a body to trigger “All mapped” when all mappable bodies are done.

## Notes

- Web Overlay endpoints: `http://localhost:9005/info`, `/cargo`, `/ship-icon`, `/exploration`.
- Settings/DB: `%APPDATA%\EliteDataRelay\settings.json`, `%APPDATA%\EliteDataRelay\exploration.db`.
