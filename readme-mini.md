Release Notes - v0.70.8

What's new
- Historical exploration import now shows a modeless progress window with per-file and overall progress, and closes automatically when finished.
- Vehicle/suit transitions are fully handled: SRV, Fighter, On Foot, Taxi, and Multicrew update immediately; re-boarding restores your ship details.
- Fixed ship label sticking on SRV/suit after re-boarding by preserving mothership info and debouncing late transient events.
- Ship rename (SetUserShipName) is applied instantly so name/ident update without waiting for a Loadout.
- Special-mode icons: SRV/Fighter/On Foot show themed placeholders in the app; the web overlay uses data-URL icons when no PNG is present.
- Overlay updates: ship text and icon stay in sync with actual state across transitions.
- Removed: Web Overlay server and related settings/UI to reduce startup time and background overhead.
- Next Jump overlay now shows EDSM traffic as soon as FSD charge begins. It uses cached system info when available and triggers an immediate fetch for the target to populate traffic pre‑witchspace.
- New per‑overlay traffic toggles in Settings → Overlay:
  - "Show traffic on exploration overlay"
  - "Show traffic on next jump overlay"
- Exploration overlay traffic respects its toggle and falls back to compact system info when disabled.

Fixes
- Resolved intermittent crash in the Cargo tab caused by DataGridView painting during rapid row updates. Updates are now marshalled to the UI thread, temporarily suspend layout and hide the grid while mutating rows, and resume cleanly after. This removes the null-reference in background paint.
- Hardened cell painting for the Cargo grid with extra safety checks to avoid header/invalid cell cases during selection indicator drawing.

Hotkeys
- Respect the “Enable global hotkeys” setting at startup (do not register when disabled).
- Added no-repeat behavior to global hotkeys to prevent multiple triggers when keys are held.
- Enforce exact modifier matches, so extra modifiers (e.g., Shift) won’t activate Ctrl+Alt hotkeys by accident.
- Ignore AltGr-as-Ctrl+Alt collisions for non-function keys to avoid unintended triggers while typing on layouts that use AltGr.
- Support Win-key modifiers if configured in settings.

Supported events (highlights)
- LoadGame, Loadout, ShipyardSwap/New
- VehicleSwitch, Embark, Disembark
- LaunchSRV/DockSRV, SRVDestroyed
- LaunchFighter/DockFighter, FighterDestroyed
- SetUserShipName

Notes
- To re-run the historical import, set `ExplorationHistoryImported` to `false` in `%APPDATA%\EliteDataRelay\settings.json`.
