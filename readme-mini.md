Mini Release Notes - Ship Icons v0.70.3

What's new
- Ship Icons
  - Display-name + alias matching for ship icons.
    - Supports Mk spacing variants (e.g., "Cobra MkIII" vs "Cobra Mk III").
    - Supports hyphen/space variants (e.g., "Type-7 Transporter" vs "Type 7 Transporter").
    - Handles punctuation variants (e.g., "Fer-de-Lance").
  - Internal ship names remain supported via mapping, with caching unchanged.
  - Added ship images and redirects:
    - New images: `Images/Ships/Type 10 Defender.png`, `Images/Ships/Type-11 Prospector.png`.
    - Internal names mapped: `type9_military` -> Type 10 Defender, `lakonminer` -> Type-11 Prospector.
  - Embedded ship PNGs into the executable (no runtime file I/O required).
    - Project: `EliteDataRelay.csproj` now embeds `Images/Ships/**/*.png` as resources.
    - Removed copy-to-output of `images/ships`; ShipIconService now loads embedded resources only.
  - Fallback behavior updated:
    - Replaced `unknown.png` with a generated placeholder image containing the text "No image found".
  - Stability/quality fixes:
    - Removed duplicate alias keys causing static initializer exceptions.
    - Resolved nullable warning (CS8601) in alias resolution.

---
