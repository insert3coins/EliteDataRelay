# Elite Dangerous Data Relay

A lightweight Windows companion app for Elite Dangerous that provides real-time data overlays, comprehensive session tracking, exploration logging, and stream-friendly output.

---

## Features

**Session Tracking**
- Track credits earned, cargo collected, and session duration
- Dedicated Mining Session mode with limpet usage, refined materials, and active mining time

**Exploration Logging**
- Automatically records every system visited with timestamps
- Logs all scanned and mapped bodies, highlighting first discoveries and first footfalls
- SQLite database for persistent exploration history with FSS progress tracking
- Estimates and tracks exploration data value

**Real-Time Data Monitoring**
- Ship status, cargo hold, and material inventory tracking
- Ship loadout viewer with detailed tooltips for engineered modules
- Live cargo monitoring with optimized response times

**In-Game Overlays**
- Customizable overlays: Info, Cargo, Ship Icon, and Exploration
- Draggable positioning with automatic position memory
- Configurable fonts, colors, transparency, and borders
- Global hotkeys for show/hide control (Ctrl+Alt+F11/F12)

**Browser-Source Overlays**
- Built‑in local web server (default port 9005)
- Matching styles and sizes for Info, Cargo, Ship Icon, and Exploration
- Auto‑updates live via WebSocket (no scene refresh required)
- Endpoints: `/info`, `/cargo`, `/ship-icon`, `/exploration`

**Streaming & Content Creation**
- Text file output with customizable format templates
- Multiple overlay support for stream scenes
- Position export for OBS integration
- Real-time cargo and session data for stream displays
- Screenshot helper: auto‑rename with {System}/{Body}/{Timestamp} and BMP→PNG conversion

**Advanced Features**
- Global hotkeys for monitoring control (Ctrl+Alt+F9/F10)
- Comprehensive event tracking from Elite Dangerous journals
- Performance-optimized file monitoring with 25ms debounce
- Fast start option to skip historic journal lines on initial scan

---

## Getting Started

1. **Prerequisites**: Windows 10/11 with .NET 8 Desktop Runtime
2. **Download**: Get the latest release and run `EliteDataRelay.exe`
3. **Launch**: Start Elite Dangerous
4. **Monitor**: Click Start in the app to begin monitoring
5. **Customize**: Configure overlays, hotkeys, and file output in Settings

### OBS Browser Source (optional)
- Enable Web Overlay server in Settings → Web Overlay
- Add Browser Source to OBS using one of:
  - `http://localhost:9005/info`
  - `http://localhost:9005/cargo`
  - `http://localhost:9005/ship-icon`
  - `http://localhost:9005/exploration`

---

## License & Disclaimer

Licensed under GPL-3.0 - see `LICENSE.txt` for details.

This is a third-party tool not affiliated with or endorsed by Frontier Developments plc.
