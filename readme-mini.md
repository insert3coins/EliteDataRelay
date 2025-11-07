Release Notes - v0.70.8

What's new
- Next Jump overlay now shows EDSM traffic as soon as FSD charge begins. It uses cached system info when available and triggers an immediate fetch for the target to populate traffic pre‑witchspace.
- New per‑overlay traffic toggles in Settings → Overlay:
  - "Show traffic on exploration overlay"
  - "Show traffic on next jump overlay"
- Exploration overlay traffic respects its toggle and falls back to compact system info when disabled.

- Cargo overlay auto-sizes to content. The cargo overlay now grows/shrinks as items change, with sensible min/max bounds and screen-aware clamping. Initial size starts compact and expands as needed.
- Session stats readability tweaks. Added spacing before Session CR and Session Cargo values to improve legibility in the cargo overlay.


