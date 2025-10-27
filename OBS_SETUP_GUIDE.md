# OBS Setup Guide for Elite Data Relay Overlays

This guide will help you capture your Elite Data Relay overlays in OBS with correct positioning.

## Quick Start

### Step 1: Enable OBS Compatibility Mode

1. Open **Elite Data Relay**
2. Go to **Settings** → **Overlay** tab
3. Check **"Enable OBS compatibility mode (allows Window Capture)"**
4. Click **OK** and **restart the application**

### Step 2: Get Overlay Positions

When your overlays are visible, Elite Data Relay automatically exports their positions to:
```
%AppData%\EliteDataRelay\output\overlay_positions.json
```

This file contains the exact X/Y coordinates for each overlay on your screen.

### Step 3: Add Overlays to OBS

For each overlay you want to capture:

1. In OBS, click **+ (Add Source)** → **Window Capture**
2. Name it (e.g., "Elite Info Overlay")
3. Select the window from the dropdown:
   - `Elite Data Relay: Info`
   - `Elite Data Relay: Cargo`
   - `Elite Data Relay: Ship Icon`
   - `Elite Data Relay: Exploration`
4. Click **OK**

### Step 4: Position the Overlays in OBS

Open the `overlay_positions.json` file to see each overlay's position. For example:

```json
{
  "Info": {
    "X": 20,
    "Y": 910,
    "Width": 320,
    "Height": 85,
    "WindowTitle": "Elite Data Relay: Info"
  }
}
```

**To position the overlay in OBS:**

1. Right-click the source in your scene
2. Select **Transform** → **Edit Transform**
3. Set the **Position** to match the JSON file:
   - Position X: `20`
   - Position Y: `910`
4. Click **Close**

Repeat for each overlay!

## Alternative Method: Manual Positioning

If you prefer, you can manually position the overlays in OBS:

1. Add the Window Capture source as described above
2. In your OBS scene, drag the overlay to match where it appears on your screen
3. Lock the source (right-click → Lock) so you don't accidentally move it

## Tips & Tricks

### Chroma Key (Remove Background)

To make the overlay backgrounds transparent in OBS:

1. Right-click your Window Capture source → **Filters**
2. Click **+ (Add Filter)** → **Chroma Key**
3. Set **Key Color Type** to **Custom**
4. Click the color picker and select the overlay background color (usually black)
5. Adjust **Similarity** and **Smoothness** to remove the background

### Crop the Window

If you want to crop parts of the overlay:

1. Hold **Alt** and drag the red edges of the source in OBS
2. Or right-click → **Filters** → **Crop/Pad**

### Automatic Updates

The `overlay_positions.json` file is updated automatically whenever:
- You start the overlays
- You move an overlay by dragging it

Just check the file after repositioning to get the new coordinates!

## Troubleshooting

### OBS can't see my overlays
- Make sure **OBS Compatibility Mode** is enabled in settings
- **Restart Elite Data Relay** after enabling compatibility mode
- Check that the overlays are visible on your screen

### Overlays appear at (0,0) in OBS
- Use the position values from `overlay_positions.json` to manually position them
- Right-click source → Transform → Edit Transform → Set X/Y position

### Overlays look different in OBS
- In OBS compatibility mode, transparency quality is slightly reduced
- This is normal and allows OBS to capture the windows
- Use Chroma Key filter if you want true transparency

## Questions?

Check the main README or submit an issue on GitHub!
