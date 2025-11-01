Mini Release Notes - Next Jump Overlay (today)

What's new

- Next Jump overlay
  - Shows on FSD charge via Status.json (FSDCharging rising edge).
  - Also shows on StartJump(Hyperspace); MusicTrack=Jump as fallback.
  - Hides on charge cancel (FSDCharging falling edge) and on FSDJump (with a 2s linger).
  - Overlay is hidden at app start and only appears on charge.

- NavRoute-driven context.
  - Uses NavRoute.json to resolve the next hop, segment distances, jumps left, and total remaining ly.
  - Destination hop has a ring highlight; active hop has a subtle glow.

- Overlay redesign (clean, compact).
  - Header: system name (left) + distance (right, plain text; pill removed).
  - Route strip: inner margins, clamped labels, destination ring, active-hop glow.
  - Progress bar: overall route completion.
  - Footer: "Jump i of N" (left) and "Remaining N.N ly" (right, gold value).

- Settings (new options).
  - ShowNextJumpJumpsLeft (default: true) — bullet "• N jumps left".

- Reliability / behavior.
  - Status priming prevents the overlay from appearing at app start.
  - Lazy creation ensures the overlay is available when charge starts.
  - Removed debug spam for the overlay once verified working.

Additional tweaks

- Startup behavior: fixed a case where the Next Jump overlay appeared on app start; now strictly tied to FSDCharging/StartJump.
- Linger: overlay remains visible ~2s after FSDJump before hiding for better continuity.
- UI spacing: prevented top-left name and top-right distance from colliding; ensured route-strip labels don't touch panel borders; removed duplicate "ly" near system title.
- Visuals: slightly stronger active-hop emphasis; destination ring retained.
- Performance: exploration overlay updates are immediate on system change and do not block the Next Jump overlay.

