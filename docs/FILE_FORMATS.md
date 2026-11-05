# File Formats — RoSpriteEnhancer v1.21.0

## Supported Formats

| Format | Extension | Read | Write | Notes |
|--------|-----------|------|-------|-------|
| BMP | `.bmp` | ✅ | ✅ | Magenta colorkey (`#FF00FF`) = transparency |
| PNG | `.png` | ✅ | ✅ | True alpha channel, lossless |
| TGA | `.tga` | ✅ | ✅ | Uncompressed 32-bit RGBA |
| SPR | `.spr` | ✅ | ✅ | RO animation (v1.1-v2.1) |
| ACT | `.act` | ✅ | ✅ | Animation offsets |
| GRF | `.grf` | ✅ | ✅ | Archive (v2.0 Zlib/Deflate) |
| JSON | `.json` | ✅ | ✅ | Presets, locales, palette |
| ICO | `.ico` | ✅ | — | Application icon |

---

## SPR (Sprite Animation)

**File:** `data/sprite/*.spr`

| Property | Description |
|----------|-------------|
| Format | Binary, version 1.1–2.1 |
| Frames | Multiple animation frames |
| Palette | Indexed color (256 colors) |
| Colorkey | Index 0 = `#FF00FF` = transparent |
| RLE | Run-length encoding supported |

### Structure

```
SPR Header (version, frame count, width, height)
├── Frame 1 (RLE-encoded pixel data)
├── Frame 2 (RLE-encoded pixel data)
└── ...
```

**See:** `RoSpriteEnhancer.Core.IO.SprReader`, `SprWriter`

---

## ACT (Animation)

**File:** `data/sprite/*.act`

| Property | Description |
|----------|-------------|
| Format | Binary |
| Frames | Animation frame count |
| Offsets | Anchor points per frame |
| Scale | Scale factors per layer |

### Anchor Point Scaling

When scaling SPR files, ACT offset points must be scaled proportionally:

```csharp
newOffsetX = header.OffsetX * (scaleX / header.OriginalWidth)
newOffsetY = header.OffsetY * (scaleY / header.OriginalHeight)
```

**See:** `RoSpriteEnhancer.Core.IO.ActFile.TryScaleOffsets()`

---

## GRF (Gravity Resource File)

**File:** `data.grf`, `custom.grf`

| Property | Description |
|----------|-------------|
| Format | GRF v2.0 |
| Compression | Zlib/Deflate |
| Contents | Sprites, images, sounds, maps |
| Path encoding | EUC-KR/ANSI |

### Operations

- Read archive contents
- Extract files/folders
- Repack modified files
- Merge multiple GRFs

**See:** `RoSpriteEnhancer.Core.IO.GrfArchive`

---

## BMP (Bitmap)

**File:** `*.bmp`

| Variant | Description |
|---------|-------------|
| 32-bit RGBA | True alpha channel |
| 24-bit RGB | No alpha (magenta = transparency) |
| 8-bit Palette | 256 colors, index 0 = magenta |

### Magenta Colorkey

Color `#FF00FF` (Index 0 in palette) = Transparency.

**Conversion:**
1. `MagentaToAlpha()` before scaling/shading
2. `AlphaToMagenta()` after processing

**See:** `RoSpriteEnhancer.Core.Utils.MagentaHandler`

---

## PNG (Portable Network Graphics)

**File:** `*.png`

| Property | Description |
|----------|-------------|
| Alpha | True alpha channel |
| Compression | Lossless |
| Best for | Modern RO clients, WARP plugins |

**Recommended format** for most use cases.

---

## TGA (Truevision TGA)

**File:** `*.tga`

| Property | Description |
|----------|-------------|
| Type 2 | Uncompressed true-color (supported) |
| Type 10 | RLE compressed (supported) |
| Depth | 32-bit RGBA |
| Origin | Top-left |

**See:** `BatchProcessor.LoadTga()`, `SaveTga()`

---

## JSON

**File:** `*.json`

| File | Purpose |
|------|---------|
| `*.palette.json` | Color palette definitions |
| `*.json` (Locales) | Language translations (48 languages) |
| `*.json` (Presets) | Enhancement preset configurations |
| `*.atlas.json` | Sprite atlas metadata |
| `keyboardshortcuts.json` | Custom hotkeys |

---

## ICO (Icon)

**File:** `Assets/Icon.ico`

Application icon for Windows executable.

**See:** `RoSpriteEnhancer.csproj` `<ApplicationIcon>`

---

## EUC-KR Encoding

RO sprite file paths use EUC-KR/ANSI encoding. **Never** use UTF-8 for sprite file names.

```csharp
// Correct: EUC-KR encoding
using var reader = new StreamReader(path, Encoding.GetEncoding("euc-kr"));
```

**See:** `App.xaml.cs` EUC-KR registration.

---

## See Also

- `ARCHITECTURE.md` — System architecture
- `API.md` — API documentation
- `RO_DATA_FORMATS.md` — RO-specific data formats
