Release Notes - v0.70.6

What's new
- Historical exploration import now shows a progress window while scanning large journal collections. The window is modeless, so the main app stays responsive.
- The progress window appears only when a one-time import runs; after completion it will not show again.
- Overall progress displays by file and percent; the UI closes automatically when done.

Notes
- To re-run the import (for troubleshooting), edit your settings file and set `ExplorationHistoryImported` to `false`:
  - Path: `%APPDATA%\\EliteDataRelay\\settings.json`
  - Key: `ExplorationHistoryImported`
