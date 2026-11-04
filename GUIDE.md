# RoSpriteEnhancer — User Guide

**Version 1.21.0 · Liquid Glass 2026 Edition**

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Build / Compiling](#build--compiling)
3. [Workflow Overview](#workflow-overview)
4. [Step 1: Extract Sprites from GRF](#step-1-extract-sprites-from-grf)
5. [Step 2: Using RoSpriteEnhancer](#step-2-using-rospriteenhancer)
6. [Step 3: Pack Sprites Back into GRF](#step-3-pack-sprites-back-into-grf)
7. [Settings Explained](#settings-explained)
8. [Output Formats Explained](#output-formats-explained)
9. [Performance Tips for Legacy Hardware](#performance-tips-for-legacy-hardware)
10. [Common Errors and Solutions](#common-errors-and-solutions)
11. [Technical Background](#technical-background)
12. [Keyboard Shortcuts](#keyboard-shortcuts)
13. [Presets](#presets)
14. [Sprite Atlas](#sprite-atlas)
15. [Onion Skinning](#onion-skinning)

---

## Prerequisites

| What | Where to Download |
|------|-------------------|
| **.NET 8 SDK** (for compiling) | https://dotnet.microsoft.com/download |
| **GRF Editor** (extract/pack sprites) | https://rathena.org/board/files/file/2766-grf-editor/ |
| **Windows 7 / 8 / 10 / 11** (32-bit or 64-bit) | — |

> **.NET 8 Runtime** is **not** required when using `build.bat` — the EXE is self-contained (everything embedded).

---

## Build / Compiling

### Method 1: build.bat (recommended)

```
1. Extract ZIP
2. Double-click build.bat
3. Wait until "BUILD SUCCESSFUL" appears
4. EXE is located in: bin\Release\RoSpriteEnhancer_v1.21.0\
```

### Method 2: Manual

```batch
cd RoSpriteEnhancer
dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -o bin\Release
```

### What build.bat does

- Checks if .NET SDK is installed (with download link if not)
- Cleans old build
- Compiles as win-x86 Single-File EXE
- Automatically creates a ready ZIP package
- Asks if the EXE should be started directly

---

## Workflow Overview

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│   your .grf file                                            │
│        │                                                    │
│        ▼                                                    │
│   [GRF Editor] → Extract Sprites → Folder on PC            │
│                                              │              │
│                                              ▼              │
│                                   [RoSpriteEnhancer]        │
│                                    Scale2x + Shading        │
│                                              │              │
│                                              ▼              │
│                                   Output Folder (PNG/BMP)   │
│                                              │              │
│                                              ▼              │
│   [GRF Editor] → Pack Sprites → new .grf file              │
│        │                                                    │
│        ▼                                                    │
│   Ragnarok Online Client with better sprites! ⚔            │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Step 1: Extract Sprites from GRF

### Open GRF Editor and Load GRF

```
1. Start GRF Editor
2. File → Open → select your data.grf (or a custom GRF)
```

### Relevant Sprite Folders in RO

| Folder in GRF | Content |
|---------------|---------|
| `data\sprite\¸ó½ºÅÍ\` | Monster sprites |
| `data\sprite\ÀÎ°£Á·\` | Job classes (players) |
| `data\sprite\¸Ó¸®Åë\` | Hair styles |
| `data\sprite\¾ÆÀÌÅÛ\` | Items (ground drops) |
| `data\sprite\npc\` | NPC sprites |
| `data\sprite\°¡»óÀÎ°£Á·\` | Homunculi |

> **Tip:** Folder names are Korean EUC-KR encoded — they appear correctly in GRF Editor.

### Extract

```
1. Select desired folder in GRF Editor
2. Right-click → Extract
   OR: Tools → Extract → Folder with GRF tree structure
3. Choose target folder e.g. C:\RO_Sprites_Original\
4. Wait until export completes
```

> **Recommendation:** Extract with "**GRF tree structure**" so subfolders are preserved. The tool processes subfolders automatically.

---

## Step 2: Using RoSpriteEnhancer

### Start

```
RoSpriteEnhancer.exe double-click
```

### Set Input Folder

```
Left side → "…" button at "Input Folder"
→ C:\RO_Sprites_Original\
→ File list on right fills automatically
```

### Set Output Folder

```
"…" button at "Output Folder"
→ Create new folder e.g. C:\RO_Sprites_Enhanced\
```

> If left empty: tool automatically creates `..._enhanced` next to input folder.

### Check Preview

```
1. Click a sprite in the right list
2. Preview appears in the center
3. Slider at bottom: left = BEFORE, right = AFTER
4. Adjust settings and preview updates automatically
```

### Start Batch Processing

```
1. Adjust settings as desired (see section below)
2. Click "▶ BATCH PROCESSING START"
3. Confirm dialog with Yes
4. Progress bar shows progress
5. If needed: "■ Stop" to abort
6. Completion message appears when finished
```

---

## Step 3: Pack Sprites Back into GRF

### With GRF Editor

```
1. Open GRF Editor
2. Open your data.grf (or custom GRF)
   → Recommendation: Create custom GRF instead of overwriting data.grf!
3. Edit → Add Folder (with GRF tree structure)
   → C:\RO_Sprites_Enhanced\
4. GRF Editor shows all new files as "modified"
5. File → Save (or Ctrl+S)
6. Done!
```

### Create Custom GRF (recommended)

```
1. GRF Editor → File → New GRF
2. Save as e.g. custom_hd_sprites.grf
3. Pack sprites as described above
4. Add to data\rdata.ini (or DATA.INI):

   [Data]
   0=custom_hd_sprites.grf
   1=data.grf

5. Custom GRF has higher priority than data.grf
```

> **Advantage:** Original sprites in data.grf remain untouched.  
> If problems occur, simply remove the entry from DATA.INI.

---

## Settings Explained

### 🔍 Scaling

| Option | Description | Recommendation |
|--------|-------------|----------------|
| **2x** | Scale2x algorithm, double resolution | ✅ For all hardware |
| **4x** | Scale2x applied twice, 4x resolution | Only for fast CPUs |

> At 2x: a 64×64 sprite becomes 128×128  
> At 4x: a 64×64 sprite becomes 256×256

### ✨ Pixel Shading

| Option | Effect | Default |
|--------|--------|---------|
| **Outline Enhancement** | Darkens pixels at sprite edges → sharper contours | ✅ On |
| **Outline Strength** | 0.1 = subtle, 1.0 = strongly outlined | 0.5 |
| **Drop Shadow** | Small black shadow offset 2px → more depth | ✅ On |
| **Highlight Boost** | Bright surfaces (armor, highlights) become brighter | ✅ On |
| **Sharpen** | Unsharp mask after scaling | ❌ Off |

> **Tip:** For monsters, outline strength ~0.6 recommended.  
> For player sprites, 0.4 is often more pleasant.

### 💾 Output Format

See next section.

### 🎨 Magenta Colorkey

| Option | Description |
|--------|-------------|
| **Magenta Handling** | Detects `#FF00FF` as transparency and converts correctly |
| **Alpha Threshold** | Pixels with alpha < value = transparent (default: 128) |

> **Always keep enabled** when processing RO BMP sprites.  
> Without magenta handling, purple artifacts appear at sprite edges.

---

## Output Formats Explained

### PNG (recommended)

- True alpha channel (transparency without tricks)
- Lossless compression
- Best quality
- Works with modern GRF setups and WARP plugins
- **Choose this if unsure**

### BMP + Magenta Colorkey

- Native RO format
- Transparency = pixels with color `#FF00FF`
- Compatible with all GRF versions including very old clients
- Larger files than PNG

### 8-bit BMP (Palette)

- Exactly like original RO sprites
- 256 colors + magenta on index 0
- Smallest file size
- Slight quality loss from color quantization (Median-Cut)
- For maximum client compatibility

---

## Performance Tips for Legacy Hardware

### For Nvidia 8600 GT / Weak CPUs

```
✅ Scaling: 2x (not 4x)
✅ Process each category folder separately instead of all at once
✅ Output format: PNG (faster than 8-bit BMP due to quantization)
✅ Sharpen: Off (saves ~15% time)
```

### Speed Up

```
→ Close other programs during processing
→ Output folder on same drive as input
→ No network drive as output folder
→ For very large folders (1000+ sprites): split into subfolders
```

### Expected Processing Times (2x, CPU only)

| Hardware | ~Sprites/Minute |
|----------|-----------------|
| Nvidia 8600 GT system (legacy) | 50–80 |
| Modern i5/i7 | 200–400 |
| Ryzen 5/7 | 300–500 |

---

## Common Errors and Solutions

### "BUILD FAILED"

```
Cause: .NET 8 SDK not installed
Solution: https://dotnet.microsoft.com/download/dotnet/8.0
          → Download SDK (not Runtime) and install
```

### "No Files Found"

```
Cause: Wrong folder, or file filter checkboxes disabled
Solution: Ensure BMP/PNG/TGA checkboxes are enabled
          Folder must directly contain sprite files
```

### Purple Artifacts Around Sprites

```
Cause: Magenta handling disabled
Solution: Enable "Magenta Handling" checkbox
          Run batch again
```

### Sprites Look Too Dark

```
Cause: Outline strength too high
Solution: Reduce outline strength to 0.3–0.4
          Enable Highlight Boost
```

### TGA Files Skipped

```
Cause: TGA with unknown compression format (rare)
Solution: Tool supports TGA Type 2 (uncompressed) and Type 10 (RLE)
          Other types are skipped with error message
          → Convert to PNG (e.g. with IrfanView) and try again
```

### "System.OutOfMemoryException" at 4x

```
Cause: 4x scaling needs ~4x more RAM than 2x
Solution: Switch to 2x, or split folders
          For 4x: at least 2 GB free RAM recommended
```

### GRF Editor Doesn't Find New Sprites

```
Cause: DATA.INI not configured correctly
Solution: Ensure custom GRF comes BEFORE data.grf in DATA.INI:

          [Data]
          0=custom_hd_sprites.grf   ← first = higher priority
          1=data.grf
```

---

## Technical Background

### Scale2x Algorithm

Scale2x (also EPX) is a pixel-art-specific upscaling algorithm. Unlike bilinear or bicubic interpolation, Scale2x does not **blur** edges — it detects transitions between colors and extends them intelligently.

**Rule:** Each pixel becomes 4 output pixels (2×2 block). An output pixel is only taken from a neighbor when:
- Two adjacent neighbors have the same color
- The opposite neighbors are **not** equal

This preserves sharp edges and produces no blur artifacts.

### Magenta Colorkey

RO sprites in BMP format have no real alpha. Instead, color `#FF00FF` (R:255, G:0, B:255) is defined as "transparent". The client renders these pixels as invisible.

**Why conversion before Scale2x?**  
Scale2x would treat magenta as a normal color. At the edge between a magenta pixel and a blue pixel, Scale2x might produce `#FE01FE` — almost magenta, but not exact. This pixel would render as a visible purple dot. Conversion to real alpha before scaling prevents this.

### Median-Cut Palette Quantization

For 8-bit BMP output, 32-bit ARGB color information is reduced to 256 colors.

**Algorithm:**
1. All unique colors are collected (weighted by frequency)
2. The color space is treated as a 3D cube (R/G/B axes)
3. The cube is recursively split at the median of the longest axis
4. Each resulting "bucket" is reduced to its weighted mean
5. Slot 0 in the palette is always `#FF00FF` (magenta = transparency)

Compared to naive quantization (e.g. only most frequent colors), Median-Cut achieves better color distribution across the entire sprite.

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| **Strg + O** | Select input folder |
| **Strg + S** | Start batch processing |
| **F1** | About dialog |
| **Esc** | Cancel (in dialogs) |

Shortcuts are customizable via `KeyboardShortcutManager` (settings stored in `%AppData%\RoSpriteEnhancer\keyboardshortcuts.json`).

---

## Presets

### Save Preset

1. Configure all settings as desired
2. Enter preset name in text field
3. Click **Save**
4. Preset is stored in `%AppData%\RoSpriteEnhancer\Presets\`

### Load Preset

1. Select preset from ComboBox
2. All settings are applied automatically
3. Preview updates immediately

### Export / Import Preset

- **Export:** Select preset → click **📤** → choose file location
- **Import:** Click **📥** → select `.json` preset file → preset is added to list

### Cloud Sharing

- **Upload:** Click **☁↑** → preset is uploaded to configured repository
- **Download:** Click **☁↓** → enter preset ID → preset is downloaded and added locally

---

## Sprite Atlas

### Create Atlas

1. Click **🖼 Sprite Atlas Create**
2. Select multiple sprite images (BMP/PNG/TGA, multi-select)
3. Choose output folder
4. Atlas is created:
   - `basename_000.png` — the atlas image
   - `basename_000.atlas.json` — metadata with coordinates per sprite

Multiple atlas files are created if sprites exceed 4096×4096 pixels per atlas.

### Extract Sprite from Atlas

Use `SpriteAtlasGenerator.ExtractSprite(atlasImagePath, atlasJsonPath, fileName)` to extract a single sprite by filename.

---

## Onion Skinning

### Enable

1. Load an `.spr` animation file
2. Check **🧅 Onion Skin** checkbox
3. Adjust strength with the slider (0.1 = subtle, 0.9 = strong)

### How It Works

The current frame is displayed at full opacity. Up to 3 frames before and 3 frames after are overlaid with decreasing opacity. This helps visualize animation flow and spot inconsistencies between frames.

---

*RoSpriteEnhancer v1.21.0 — Part of the Yggdrasil.Studio ecosystem*  
*Developed for Ragnarok Online Private Server Modding*
