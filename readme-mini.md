Release Notes - v0.72.4

What's new

- Fleet Carrier tracker : we hydrate inventory from the latest `Market.json`, pull both personal and squadron stock, refresh automatically when docking.
- Journal watcher seeds the last known system before skipping history so the Exploration tab instantly shows the current system even when no new jump occurs; also fixed the startup window logic so the form always appears on launch.
- Removed the legacy “Start Mining Session” panel/notifications.
- Misc cleanup: exploration system change always feeds the exploration service, UI no longer auto-hides to tray during startup, and session tracker no longer publishes the old mining notifications.
