# Mini README – Recent Changes

## Overview
This mini README summarizes recent fixes and tweaks to ship display and icon mapping, plus a logging change that was requested and then reverted.

## Ship UI – Use Localised Name
- The ship label in the UI now prefers the localised display name from journals.
- Change: LoadGame handling now calls the internal updater with `Ship_Localised` when present, and falls back to a friendly display name derived from the internal ship code.
  - File: Services/JournalWatcherService.PlayerEvents.cs:32–52

## Ship Icon Mapping – Align With Images
Standardized mappings so every internal ship name resolves to an existing PNG in `Images/ships`.

Adjusted mappings
- "diamondback" → "Diamondback Scout" (Images/ships/Diamondback Scout.png)
- "empire_courier" → "Imperial Courier"
- "federation_dropship" → "Federal Dropship"
- "federation_dropship_mkii" → "Federal Assault Ship"
- "federation_gunship" → "Federal_Gunship"
- "ferdelance" → "Fer De Lance"
- "typex" → "Alliance Chieftain"
- "type9_military" → "Type9_Military"
- "belugaliner" → "Beluga Liner"

New mappings added
- "cobramkv" → "Cobra Mk V"
- "python_mkii" → "Python Mk II"
- "typex_2" → "Alliance Crusader"
- "cyclops" → "Cyclops"

All mapping values now match existing image filenames.
  - File: Services/ShipIconService.cs

## Logging Listener (Reverted)
- A temporary filtering trace listener was added to suppress `[CargoProcessorService]` in `debug_log.txt`, then removed per request.
  - Program.cs now uses the standard `TextWriterTraceListener` again.

## Quick Verification
1) Ship name on load
   - Launch with a LoadGame event containing `Ship` and `Ship_Localised`.
   - Expected: UI shows the localised name (e.g., "Type-11 Prospector").
2) Icon display
   - Switch ships and confirm the icon and name match images in `Images/ships`.
3) Logs
   - `debug_log.txt` should include normal trace output (no filtering applied).

