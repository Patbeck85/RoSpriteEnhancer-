# RoSpriteEnhancer v1.21.0 — Liquid Glass 2026 Edition

**GPU-Free Pixel-Art Upscaler for Ragnarok Online Sprites**

Specially designed for legacy hardware — runs on Nvidia 8600 GT and weaker.

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| **Scale2x / Scale4x** | Pixel-Art optimized upscaler — no GPU needed |
| **XBRZ 2x / 4x** | Advanced pixel-art scaling with edge detection |
| **Waifu2x-ncnn-vulkan** | AI Anime upscaling (2x/4x) via local CLI tool |
| **Real-CUGAN** | Alternative AI upscaling for anime sprites |
| **RealESRGAN** | Offline ncnn Vulkan upscaling |
| **GFPGAN** | Face/detail restoration for anime portraits |
| **Bilateral Pre-Filter** | Noise reduction before AI upscaling |
| **2x / 4x Scaling** | 2x recommended for legacy hardware |
| **Outline Enhancement** | Sharper, more defined edges |
| **Drop Shadow** | Subtle depth effect |
| **Highlight Boost** | Enhanced armor/gloss effects |
| **Sharpen** | Optional sharpen filter |
| **HSL Color Shift** | Hue / Saturation / Lightness adjustment |
| **Palette Quantization** | Median-Cut 8-bit for indexed BMPs |
| **Magenta Colorkey** | `#FF00FF` transparency strictly preserved |
| **ACT Anchor Scaling** | Animation offsets scaled proportionally |
| **Incremental Caching** | MD5-based — unchanged files skipped |
| **Before/After Preview** | Real-time slider comparison with zoom/pan |
| **Side-by-Side Blend Preview** | Opacity-based before/after blend |
| **Batch Processing** | Multi-threaded folder processing |
| **Formats** | BMP / PNG / TGA / SPR |
| **GRF Archive** | Read / Extract / Pack / Merge (v2.0 Zlib/Deflate) |
| **Category Presets** | Monster, Jobs, Homunculi, NPCs, Items, UI, Effects |
| **Content-Based Categorization** | AI fallback when path is ambiguous |
| **JSON Preset System** | Save/load configurations, export/import |
| **Preset Sharing** | Cloud upload/download (HTTP repository) |
| **Sprite Atlas** | Multiple sprites → single spritesheet |
| **Onion Skinning** | Multi-frame animation preview |
| **Plugin System** | External DLL upscalers via `IUpscalerPlugin` |
| **Liquid Glass UI** | White-blue 2026 design across all windows |
| **48 Languages** | English default, full localization coverage |
| **Keyboard Shortcuts** | Customizable hotkeys (Strg+O, Strg+S, F1, etc.) |
| **Window State** | Position/size saved and restored |
| **Recent Folders** | Last 5 input/output folders |
| **Quality Check** | Automatic blur/transparency/color-shift detection |

---

## 🔧 Workflow

### 1. Extract Sprites (with GRF Editor)

```
GRF Editor → load your .grf file
→ select data/sprite folder
→ Right-click → Extract → choose folder
```

### 2. RoSpriteEnhancer

```
1. Input folder: extracted sprite folder
2. Output folder: e.g. sprite_enhanced
3. Adjust settings (Scale2x 2x for legacy HW, Waifu2x/RealCUGAN for modern GPU)
4. Select sprite from list → check preview
5. Click "BATCH PROCESSING START"
```

### 3. Pack Back into GRF (with GRF Editor)

```
GRF Editor → Merge/Add Files
→ select sprite_enhanced folder
→ Save GRF
```

---

## ⚡ Performance

| Hardware | Algorithm | Speed (approx.) | RAM |
|----------|-----------|-----------------|-----|
| Nvidia 8600 GT | Scale2x 2x | ~50–200 sprites/min | ~256 MB |
| Nvidia 8600 GT | Scale2x 4x | ~10–30 sprites/min | ~512 MB |
| Modern GPU (RTX 2060+) | Waifu2x 2x | ~100–300 sprites/min | ~1–2 GB VRAM |
| Modern GPU (RTX 2060+) | Waifu2x 4x | ~30–80 sprites/min | ~2–4 GB VRAM |
| Modern GPU (RTX 2060+) | RealCUGAN 2x | ~80–200 sprites/min | ~1–2 GB VRAM |

**Legacy recommendation:** Scale2x 2x is more than sufficient for RO and runs smoothly without GPU.

**Modern recommendation:** Waifu2x (Noise: -1, Model: models-cunet) for best anime quality.

---

## 🏗️ Build

**Prerequisites:** .NET 8 SDK

```batch
build.bat
```

Or manually:

```batch
dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -o bin\Release
```

---

## 📦 Release Package

The release package contains:
- `RoSpriteEnhancer.exe` — Standalone single-file executable (~144 MB)
- Native .NET 8 runtime libraries (D3DCompiler, PenImc, PresentationNative, vcruntime, wpfgfx)
- No .NET installation required on target system

---

## 🏗️ Architecture

See `ARCHITECTURE.md` for the complete system architecture.

---

## 📌 Known Limitations

| Limitation | Details |
|------------|---------|
| **SPR Format** | RO native format — extract via GRF Editor to BMP/PNG first, or process directly via BatchProcessor |
| **Magenta Colorkey** | `0x00FF00FF` treated as transparency — strictly preserved by MagentaHandler |
| **Animations** | Each frame processed individually — works correctly |
| **ACT Files** | Anchor scaling only when `ScaleActAnchors` enabled + correct factor |
| **EUC-KR Filenames** | Korean paths matched via `SpriteFilter` — do NOT rename sprite files! |
| **AI Tools** | Waifu2x/RealCUGAN/RealESRGAN/GFPGAN require local CLI tools |
| **4K Textures** | Client-side downscale to 512x512 or 1024x1024 recommended |

---

## 📋 Changelog

See `CHANGELOG.md` for the full changelog.

---

## 📄 License

Part of the Yggdrasil.Studio ecosystem — Open Source for the RO community.