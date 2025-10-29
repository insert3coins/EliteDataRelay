# Elite Data Relay — Mini Release Notes

## Highlights
- Full localization support with in‑app language selection (Localization tab).
- Exploration History Import reliability improvements and automatic activation of current system.
- Diagnostics window with live log tail and optional verbose logging.

## Changes & Fixes
- Localization
  - Added `Settings → Localization` tab with language dropdown (System Default, en, fr, de, es, it, pt‑BR, ru, zh‑Hans, ja).
  - App applies selected language on startup; most strings update immediately, some after restart (prompt shown).
  - Localized Settings UI (title, subtitle, nav items, Save/Cancel), Advanced options, Diagnostics window, About window.
  - Localized Exploration UI: systems header, column headers, empty/history banners, status labels (“Scanned”, “Mapped”, “First Discovery”, “First Footfall”, “Known”).
  - Localized Exploration overlay: “NO SYSTEM DATA”, FSS progress/completed, scanned/mapped lines, “Known System”, and session summary.
  - Localized tray tooltip: “Minimized to tray.”

- Exploration History Import
  - After import completes, app resolves the current system using live Location (name + SystemAddress when available),
    falls back to name lookup in the newly imported DB, then to most-recent visited system.
  - Forces the “Current System” tab to update and pushes exploration data to desktop and web overlays if monitoring is active.
  - Import completion now shows a window popup (instead of tray balloon). This can be made optional later.

- Exploration Log
  - “Time ago” extended beyond 1 day (days up to 60, then months/years).
  - Improved empty/history banners and localized column headers.

- Diagnostics & Logging
  - New `Diagnostics` window (Settings → Advanced) tails `%APPDATA%\EliteDataRelay\debug_log.txt` with open/clear/refresh actions.
  - Added `Verbose Logging` toggle in Settings → Advanced.
  - Centralized logging via `Logger.Info/Verbose`; replaced `Debug.WriteLine` across services/UI to flow through `Trace`.

- Settings persistence
  - `settings.json` now includes `UICulture` (language code or empty for system default), persisted via `AppConfiguration`.

## Technical Notes
- Resource files under `Properties/Strings*.resx` are compiled into satellite assemblies (e.g., `fr/`, `de/`, `es/` in output).
- Culture is applied at startup in `Program.cs`; the in‑app picker also applies culture immediately where possible.
- Diagnostics uses the existing `Trace` listener (see `%APPDATA%\EliteDataRelay\debug_log.txt`).

## How To Use
- Set language: Settings → Localization → choose language → OK. Some texts refresh after restart.
- Enable verbose logs or open Diagnostics: Settings → Advanced.
- Re-run exploration import: set `ExplorationHistoryImported` to `false` in `settings.json` (optionally delete `exploration.db`).

