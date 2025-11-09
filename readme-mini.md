Release Notes - v0.71.1

What's new

- Performance: Virtualized exploration grids (systems + bodies) using DataGridView VirtualMode. Backing lists supply cells via CellValueNeeded; smoother scrolling and lower memory.
- Rendering: Enabled double buffering for DataGridViews to reduce flicker (Cargo tab and Exploration log).
- Icons: Added caching for body icons to avoid repetitive image allocations; cache disposed on exit.
- Monitoring: Debounce for file change events increased from 10ms to 50ms to coalesce duplicate notifications.
- Robustness: Wrapped Start monitoring flow in try/catch to guard against unhandled exceptions; clearer error message on failure.
- Startup: Enabled ReadyToRun (Release) for faster app startup.
- Cleanup: Removed unreachable legacy row-building code and an unused helper from ExplorationLogControl; light whitespace tidy across touched files.
