Release Notes - v0.73.7

What's new
----------

- Fleet Carrier tab facelift: light-themed stat cards, color-coded crew roster, and inventory callouts for stolen/black-market/buy/sell states. Layout now reserves space for the carrier cards while the inventory tabs fill the rest, so nothing overlaps.
- Carrier tracking parity: journal events now update both the personal and squadron carriers (fuel, balance, crew, trade orders, cargo transfers, etc.) so both summaries stay live.
- ListView stability: new SafeListView guard stops WinForms from throwing `StateImageIndex` exceptions when overlays or tabs refresh.
