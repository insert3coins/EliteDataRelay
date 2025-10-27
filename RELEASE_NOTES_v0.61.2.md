# Elite Data Relay v0.61.2 — Settings Revamp, Faster Cargo Updates, Web Overlay Polish

## Highlights

- New Settings window layout
  - Left sidebar navigation, larger window (800×600), scrolling sections
  - Hotkeys merged into Advanced to simplify navigation

- Snappier cargo updates
  - Async cargo.json processing path with fewer, shorter retries
  - Quicker file change debounce for near‑instant UI refresh

- Web overlays get their own opacity control
  - Separate “Background Opacity” for browser‑source overlays (independent from desktop overlays)

- Legacy removal: Text File Output
  - Old cargo.txt feature and related settings removed to reduce clutter

## Changes

### Settings & UI
- Refactor: Settings window rebuilt with a left sidebar and 800×600 layout
- Refactor: General / Overlay / Advanced panels now scroll when content exceeds view
- Change: Hotkeys merged into Advanced; Advanced hosts both sections cleanly
- Tidy: Overlay tab auto‑sizes groups to content and removes large gaps
- Fix: “Appearance: Colors & Opacity” title shows a literal ampersand
- UX: Background Opacity control row compacted (short slider + small percent label)

### Web Overlays
- Added: `WebOverlayOpacity` setting (0–100, default 85)
  - UI: Slider in Settings → Advanced (Browser‑Source Overlay Server)
  - Server: CSS background uses `WebOverlayOpacity` (not desktop overlay opacity)

### Performance
- File monitoring
  - Debounce reduced to 10ms for faster reaction to Cargo.json changes
- Cargo processing
  - New `ProcessCargoFileAsync` reads bytes once, hashes bytes, and deserializes directly from bytes
  - Updated call sites to async on Start, file change, and initial scan complete
  - Retry tuning: attempts reduced to 3, delay to 20ms

### Legacy Feature Removal
- Removed: Text File Output feature and UI
- Removed config properties: `EnableFileOutput`, `OutputFileFormat`, `OutputFileName`, `OutputDirectory`
- Deleted files: `Configuration/AppConfiguration.FileOutput.cs`, `Services/FileOutputService.cs`, `Services/IFileOutputService.cs`, `Models/AppSettings.cs`
- Cleanup: Deleted unused empty root files: `ICargoFormUI.cs`, `IMaterialService.cs`
- Note: Old keys in `settings.json` are ignored on load and no longer written

## Notes

- Web overlays: Adjust opacity in Settings → Advanced → Browser‑Source Overlay Server
- Desktop overlays: Unchanged; they continue using the desktop Overlay Opacity control
- Expect measurably faster cargo UI refresh after in‑game changes due to async path + tighter debounce

