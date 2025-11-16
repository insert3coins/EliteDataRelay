Release Notes - v0.73.6

What's new

- Fleet Carrier tracker : we hydrate inventory from the latest `Market.json`, pull both personal and squadron stock, refresh automatically when docking.
- Journal watcher seeds the last known system before skipping history so the Exploration tab instantly shows the current system even when no new jump occurs; also fixed the startup window logic so the form always appears on launch.
- Removed the legacy “Start Mining Session” panel/notifications.
- Misc cleanup: exploration system change always feeds the exploration service, UI no longer auto-hides to tray during startup, and session tracker no longer publishes the old mining notifications.
- Mining Tracker tab: a full live/current session view, latest prospector breakdown, and session history are now available without leaving EDR. Sessions begin when you drop into a ring (or fire your first prospector), close automatically on travel/menu events, and persist between launches.
- Ship tab and status bar ship link now open your current build on EDSY with the full loadout JSON payload.
