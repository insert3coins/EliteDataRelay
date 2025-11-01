Mini Release Notes - Exploration & Startup v0.70.2

What's new
- Exploration tab: Current system only at startup
  - Suppresses exploration UI/events during the initial journal scan to avoid iterating historical systems.
  - After the scan completes, resolves the last known location and publishes exactly that system to the tab and overlays.
  - Prevents session stats from spiking due to historical entries.
- Faster, cleaner startup behavior
  - Uses the existing Fast Start option (skip journal history) for snappier first paint.
  - OnLocationChanged is ignored during initialization; live updates resume immediately after the initial scan.
- Build cleanup
  - Removed an unreachable code path in the Next Jump overlay, fixing the sole build warning (CS0162).
  - Build is now clean: 0 warnings, 0 errors.
Fixes
- Prevented rare shutdown crash (GDI+ "Parameter is not valid") when disposing UI.
  - Root cause: controls were disposed after shared fonts, causing text measurement to hit disposed FontFamily.
  - Fix: dispose UI controls before `FontManager` (ControlFactory before fonts) and defensively reset control fonts to `SystemFonts.DefaultFont` before disposal.
  - Touched files: `UI/CargoFormUI.cs`, `UI/ControlFactory.Buttons.cs`, `UI/ControlFactory.Labels.cs`.
Notes
- To maximize instant feel, enable the setting: "Fast start: skip journal history at startup".
  This makes the app jump straight to live events; the Exploration tab still resolves the current system once the initial scan finishes.
