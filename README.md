# Elite Data Relay

Elite Data Relay (EDR) is a Windows companion for Elite Dangerous. It watches the live journal feed, tracks everything that matters to a Commander, and surfaces it through a modern desktop UI plus lightweight overlays. No web services, no accounts—just local state you control.

---

## What It Does

- **Live Session Tracking**
  - Tracks credits earned, cargo collected, limpets used, and session duration.
  - Dedicated mining tab powered by ODEliteTracker’s session model: live stats, latest prospectors, and session history (persisted between launches).
  - Fleet Carrier tab combining personal + squadron data, inventory, crew state, and jump timing.

- **Exploration Logging**
  - Automatically records systems visited, bodies scanned/mapped, FSS completion, non-body signals, and Codex bio finds into a local SQLite database.
  - Detects FSSDiscoveryScan, FSSAllBodiesFound, NavBeaconScan, and legacy DiscoveryScan events.
  - Safe importer to backfill new schema fields from historical journals.

- **Ship & Cargo Insight**
  - Ship tab shows loadout stats, jump range calculations, power usage, and engineered module tooltips.
  - Session tab shows live metrics and history; cargo/material tracking stays synced with journal events.
  - Mining tab mirrors ODEliteTracker’s UX inside EDR, so you don’t need a separate app.

- **Overlays (Optional)**
  - Info, Cargo, Session, and Exploration overlays with custom fonts/colors/opacity.
  - Drag to position; coordinates persist. Designed for OBS/browser-source scenes.
  - Can be turned off entirely if you prefer the desktop UI only.

- **Quality-of-Life**
  - Screenshot helper (rename + BMP→PNG), fast-start option to skip old journals.
  - Exportable overlay positions for streaming layouts.
  - All data lives under `%APPDATA%\EliteDataRelay` (SQLite DB + JSON session files).

### Mining Insights
- Mining tab with live prospectors, ore/material breakdowns, limpets, and asteroid counts.
- Mining sessions persist to `%APPDATA%\EliteDataRelay\mining_sessions.json`, so runs survive restarts.
- Pop-out overlays (table + prospector) remain available for secondary displays.

### Fleet Carrier Tracking
- Fleet carrier tab shows personal and squadron carriers side-by-side: status, fuel, balance, docking access, and jump timers.
- Inventory grid surfaces stock levels, outstanding orders, prices, and callouts for stolen/black-market goods or sale listings.
- Crew roster displays current assignments (Active/Unavailable) with color-coded states.

---

## Getting Started

1. **Install Requirements** – Windows 10/11 with .NET 9 Desktop Runtime.
2. **Download** – Grab the latest release and run `EliteDataRelay.exe`.
3. **Launch Elite** – Start Elite Dangerous to generate journals.
4. **Click Start** – In EDR hit *Start* to begin monitoring. Tabs populate immediately.
5. **Customize** – Use Settings to toggle overlays, adjust colors/fonts, enable screenshot renaming, etc.

---

## Why Local Storage Matters

- `exploration.db` (SQLite) holds every system/body you’ve logged.
- `mining_sessions.json` keeps your mining history between runs.
- No telemetry or cloud services—everything stays on your machine.

---

## License & Disclaimer

- Licensed under **GPL-3.0** (see `LICENSE.txt`).
- Not affiliated with Frontier Developments. Use at your own risk.

If EDR helps your adventures, consider starring the repo or opening an issue/PR. Fly safe, CMDR!
