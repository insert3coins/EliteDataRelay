Release Notes - v0.70.7

What's new
- Historical exploration import now shows a modeless progress window with per-file and overall progress, and closes automatically when finished.
- Vehicle/suit transitions are fully handled: SRV, Fighter, On Foot, Taxi, and Multicrew update immediately; re-boarding restores your ship details.
- Fixed ship label sticking on SRV/suit after re-boarding by preserving mothership info and debouncing late transient events.
- Ship rename (SetUserShipName) is applied instantly so name/ident update without waiting for a Loadout.
- Special-mode icons: SRV/Fighter/On Foot show themed placeholders in the app; the web overlay uses data-URL icons when no PNG is present.
- Overlay updates: ship text and icon stay in sync with actual state across transitions.

Supported events (highlights)
- LoadGame, Loadout, ShipyardSwap/New
- VehicleSwitch, Embark, Disembark
- LaunchSRV/DockSRV, SRVDestroyed
- LaunchFighter/DockFighter, FighterDestroyed
- SetUserShipName

Notes
- To re-run the historical import, set `ExplorationHistoryImported` to `false` in `%APPDATA%\EliteDataRelay\settings.json`.
