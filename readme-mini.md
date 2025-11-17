Release Notes - v0.74.1

What's new
----------

- Fixed `SafeListView` so session history rows remain visible after handle creation.
- Session history now persists on startup by loading saved and legacy records.
- Closing the app now stops active sessions to ensure history is saved.
- Prospector overlay now hides when no scan data exists.
- “Reposition Overlays” temporarily shows every overlay, saves their positions, and restores normal visibility afterward.
- Removed the non-functional “Motherlodes” column from the Mining tab to avoid empty data.
- Mining tab “Collected” values now track cargo scoops so the column reflects mined ore pickups.
- Mining tab keeps the last session yield visible even when the session ends or no new activity occurs.

