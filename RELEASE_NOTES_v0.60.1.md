# Elite Data Relay v0.60.1 — Web Overlays, Screenshots, Mining QoL

## Highlights

- Browser-Source Overlays (new)
  - Built-in local server (default port 9005) serving Info, Cargo, Ship Icon, and Exploration overlays
  - Matches in-game overlay styles and sizes; live updates via WebSocket
  - Endpoints: `/info`, `/cargo`, `/ship-icon`, `/exploration`

- Screenshot Helper (enhanced)
  - Auto-renames with `{System}`, `{Body}`, `{Timestamp}`
  - Converts BMP → PNG automatically
  - Uses last-known system name when the event lacks one; also watches the default Pictures folder

- Hotspot Finder UI (new)
  - Simple search by mineral, ring type, system substring, and max distance
  - Uses existing bookmarks dataset

- Faster Start (optional)
  - New FastStart option to skip historic journal lines on initial scan (default: on)

## Changes

- Added: Local web overlay server with matching styles/sizes and live updates
- Added: BMP→PNG conversion in screenshot processing; smarter system name fallback
- Added: Hotspot Finder tab and Mining Companion restock reminder *note its in early stages, so might be borked
- Removed: OBS Compatibility Mode and overlay position exporter (use Browser Source overlays)
- Improved: Startup responsiveness with FastStartSkipJournalHistory
- Settings window got a make over! (went mining, came back different, saw some things out there in the void) 

## Quick OBS (Browser Source)

- Enable the Web Overlay server in Settings → Web Overlay
- Add a Browser Source in OBS with one of:
  - `http://localhost:9005/info`
  - `http://localhost:9005/cargo`
  - `http://localhost:9005/ship-icon`
  - `http://localhost:9005/exploration`

## Installation

New Users
1. Download `EliteDataRelay.zip`
2. Extract
3. Run `EliteDataRelay.exe`
4. Click Start and play

Upgrading
1. Download `EliteDataRelay-v0.60.1.zip`
2. Extract and replace your files
3. Settings and data are preserved

## Requirements

- Windows 10/11
- .NET 8 Desktop Runtime
- Elite Dangerous journal files

## Feedback

- Issues: https://github.com/insert3coins/EliteDataRelay/issues
- Discussions: https://github.com/insert3coins/EliteDataRelay/discussions

## This Build: UI + Cleanup

### Settings and UI Polish
- Refactor: Settings window uses a left sidebar and larger layout (800×600).
- Refactor: General/Overlay/Advanced sections scroll when content exceeds view.
- Tidy: Overlay tab auto-sizes groups to content and removes large gaps.
- Fix: “Appearance: Colors & Opacity” title now shows literal ampersand.
- UX: Background Opacity control row compacted (short slider, small percent label).
- Change: Merged Hotkeys into Advanced; Advanced now hosts both sections without overlap.

### Legacy Feature Removal
- Removed: Text File Output feature and related Settings UI.
- Removed: Legacy config properties (EnableFileOutput, OutputFileFormat, OutputFileName, OutputDirectory).
- Removed: File output code paths and services.
  - Deleted files: Configuration/AppConfiguration.FileOutput.cs, Services/FileOutputService.cs, Services/IFileOutputService.cs, Models/AppSettings.cs.
- Cleanup: Deleted two unused empty root files: ICargoFormUI.cs, IMaterialService.cs.
- Note: Existing keys for the removed feature in settings.json are ignored on load and no longer written on save.
