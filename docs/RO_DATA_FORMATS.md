# RO Data Formats — RoSpriteEnhancer v1.21.0

## Overview

This document describes Ragnarok Online-specific data formats supported by RoSpriteEnhancer.

## GRF (Gravity Resource File)

**Files:** `data.grf`, `custom.grf`, `rdata.grf`

| Property | Value |
|----------|-------|
| Format | GRF v2.0 |
| Compression | Zlib / Deflate |
| Path Encoding | EUC-KR / ANSI |
| Contents | Sprites, images, sounds, maps, Lua scripts |

### Directory Structure

```
data/
├── sprite/
│   ├── ¸ó½ºÅÍ/          # Monsters
│   ├── ÀÎ°£Á·/          # Job classes
│   ├── ¼Ó¼º/            # Hair
│   ├── ¾ÆÀÌÅÛ/          # Items
│   ├── npc/             # NPCs
│   └── °¡»óÀÎ°£Á·/      # Homunculi
├── texture/
├── sound/
└── map/
```

> **Note:** Folder names are Korean EUC-KR encoded. Do not rename or convert to UTF-8.

### Operations

- Read archive contents
- Extract files/folders (preserve GRF tree structure)
- Repack modified files
- Merge multiple GRFs

**See:** `RoSpriteEnhancer.Core.IO.GrfArchive`

---

## SPR (Sprite Animation)

**Files:** `data/sprite/**/*.spr`

| Property | Value |
|----------|-------|
| Format | Binary v1.1–2.1 |
| Frames | Multiple animation frames |
| Palette | Indexed 256 colors |
| Colorkey | Index 0 = `#FF00FF` (transparent) |
| Compression | RLE (Run-Length Encoding) |

### Frame Structure

```
[Frame Header]
├── Width (2 bytes)
├── Height (2 bytes)
├── Offset X (2 bytes)
├── Offset Y (2 bytes)
└── Pixel Data (RLE-encoded)
```

### ACT Alignment

When scaling `.spr` files, the corresponding `.act` file must be updated:

```csharp
ActFile.TryScaleOffsets(actPath, scaleFactor)
```

This ensures sprite anchor points don't drift after scaling.

---

## ACT (Animation)

**Files:** `data/sprite/*.act`

| Property | Value |
|----------|-------|
| Format | Binary |
| Contents | Frame offsets, anchor points, scale factors |

### Offset Scaling

ACT offsets must be scaled proportionally when sprites are upscaled:

```
newOffsetX = oldOffsetX * (newWidth / oldWidth)
newOffsetY = oldOffsetY * (newHeight / oldHeight)
```

ScaleX/ScaleY values remain unchanged (they are layer-relative).

---

## BMP (Bitmap)

**Files:** `*.bmp`

| Variant | Description |
|---------|-------------|
| 32-bit RGBA | True alpha channel |
| 24-bit RGB | Magenta (`#FF00FF`) = transparency |
| 8-bit Palette | 256 colors, index 0 = magenta |

### Magenta Colorkey

In RO BMP sprites, index 0 / `#FF00FF` represents transparency.

**Strict conversion required:**
1. `MagentaToAlpha()` before any scaling/shading
2. `AlphaToMagenta()` after processing

**Never** interpolate index 0 color during palette quantization.

---

## File Naming

**Rule:** Never rename RO sprite files.

| Rule | Reason |
|------|--------|
| Never rename | RO Client looks up files by exact name |
| EUC-KR encoding | UTF-8 conversion breaks file lookup |
| Preserve case | Case-sensitive on some systems |
| Preserve path structure | Subdirectories matter for categorization |

**Example:**
```
Correct:  data/sprite/°¡»óÀÎ°£Á·/001.spr
Wrong:    data/sprite/homunculus/001.spr  (renamed)
```

---

## EUC-KR Encoding

RO uses EUC-KR (Korean) for file names and paths.

```csharp
// Correct
Encoding.GetEncoding("euc-kr")

// Wrong
Encoding.UTF8
```

**Registered in:** `App.xaml.cs`

---

## Supported Sprite Categories

| Category | Path Fragments | Description |
|----------|--------------|-------------|
| Monster | `몬스터`, `몬스터\`, `mob` | Monsters |
| Job | `직업`, `캐릭터` | Player classes |
| Item | `아이템`, `item` | Items |
| NPC | `npc`, `NPC` | Non-player characters |
| Homunculus | `호문쿨루스` | Homunculi |
| Hair | `머리`, `hair` | Hair styles |
| Weapon | `무기`, `weapon` | Weapons (excluded from some ops) |
| Effect | `이펙트`, `effect` | Visual effects |
| Custom | User-defined | Custom category |

---

## See Also

- `FILE_FORMATS.md` — General file format documentation
- `API.md` — `SpriteFilter`, `GrfArchive`, `SprReader`, `ActFile` class references
- `DECISIONS.md` — ADR-004 (Category Matching), ADR-009 (File Naming Immunity)
