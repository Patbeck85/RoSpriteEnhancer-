# Architecture — RoSpriteEnhancer v1.21.0

## Overview

RoSpriteEnhancer is a GPU-free pixel-art upscaling tool for Ragnarok Online sprites. It follows a **layered architecture** with a platform-independent .NET 8 Core library and a WPF Windows UI.

---

## Project Structure

```
RoSpriteEnhancer.sln
├── RoSpriteEnhancer/              # WPF UI layer (net8.0-windows)
│   ├── App.xaml/.cs               # Application resources, EUC-KR encoding registration
│   ├── MainWindow.xaml/.cs        # Main window: file list, preview, batch controls
│   ├── SettingsWindow.xaml/.cs    # Settings: presets, AI tools, shortcuts, language
│   ├── AboutWindow.xaml/.cs       # About dialog: version, libraries, license
│   ├── InputDialog.xaml/.cs       # Simple modal text input
│   ├── FileItem.cs                # Data-binding model for file list
│   ├── LocExtension.cs            # XAML markup extension (simplified, code-behind fallback)
│   └── Views/Windows/             # Window definitions
├── RoSpriteEnhancer.Core/         # Core engine (net8.0, platform-independent)
│   ├── Processing/
│   │   ├── BatchProcessor.cs          # Multi-threaded processing pipeline
│   │   ├── DetailExtractor.cs         # Laplacian high-pass detail map injection
│   │   ├── QualityChecker.cs          # Automatic quality assessment
│   │   ├── SpriteAtlasGenerator.cs    # Spritesheet/packer
│   │   ├── SpriteContentClassifier.cs # Content-based category classification
│   │   ├── SpriteResolutionProfile.cs # Resolution recommendations per sprite category
│   │   └── UpscalerPluginManager.cs   # External DLL upscaler plugin loader
│   ├── Filtering/
│   │   ├── BilateralFilter.cs         # Edge-preserving pre-smoothing
│   │   ├── PaletteQuantizer.cs        # Median-Cut 8-bit quantization
│   │   ├── PixelEnhancer.cs           # Outline, shadow, highlight, sharpen, HSL
│   │   └── SuperSampler.cs            # SSAA downscaler (Lanczos3, Bicubic, Bilinear)
│   ├── IO/
│   │   ├── GrfArchive.cs              # GRF v2.0 reader/extractor/packer/merger
│   │   ├── SprReader.cs               # SPR v1.1-v2.1 binary parser
│   │   └── SprWriter.cs               # SPR binary encoder
│   ├── Models/
│   │   └── SpriteFilter.cs            # EUC-KR path category matching (16 categories)
│   ├── Scaling/
│   │   ├── Scale2x.cs                 # Pure C# pixel-art scaler
│   │   └── XbrzScaler.cs              # XBRZ 2x/4x scaler
│   ├── Services/
│   │   ├── PresetManager.cs           # JSON preset save/load/list/delete
│   │   └── PresetSharingService.cs    # HTTP-based preset upload/download/list
│   ├── Upscaling/
│   │   ├── Waifu2xRunner.cs           # Waifu2x-ncnn-vulkan CLI wrapper
│   │   ├── RealCuganRunner.cs         # Real-CUGAN-ncnn-vulkan CLI wrapper
│   │   ├── RealESRGANRunner.cs        # RealESRGAN ncnn Vulkan wrapper
│   │   ├── GFPGANRunner.cs            # GFPGAN face restoration wrapper
│   │   ├── EsrganOptions.cs           # ESRGAN configuration
│   │   ├── Waifu2xOptions.cs          # Waifu2x configuration
│   │   ├── RealCuganOptions.cs        # RealCUGAN configuration
│   │   └── AiFirstOptions.cs          # AI-first high-res shading options
│   └── Utils/
│       ├── AiToolManager.cs           # AI tool detection and download URLs
│       ├── AlphaBleedCleaner.cs       # Alpha channel post-cleanup
│       ├── ColorMatchLock.cs          # Cross-pipeline color consistency
│       ├── KeyboardShortcutManager.cs # Centralized hotkey registry (JSON persistence)
│       ├── LanguageService.cs         # JSON-based localization service (48 languages)
│       ├── MagentaHandler.cs          # Colorkey #FF00FF <-> Alpha conversion
│       ├── ToolInstaller.cs           # Central AI tool installation/detection
│       └── WindowStateService.cs      # Window state + recent folders persistence
└── RoSpriteEnhancer.Tests/        # xUnit test suite
    └── Unit/
```

---

## Layer Responsibilities

### UI Layer (`RoSpriteEnhancer`)
- **Presentation:** All XAML windows, styles, Liquid Glass design system
- **Input:** File dialogs, drag-and-drop, keyboard shortcuts
- **Orchestration:** Coordinates between Core services and UI state
- **Threading:** All UI access via `Dispatcher.InvokeAsync` (THREAD-001)

### Core Layer (`RoSpriteEnhancer.Core`)
- **Processing:** Batch pipeline, detail extraction, quality checking, atlas generation
- **Filtering:** Bilateral smoothing, pixel enhancement, super-sampling, palette quantization
- **IO:** GRF archive, SPR animation files
- **Scaling:** Scale2x, XBRZ pure C# pixel-art scalers
- **Upscaling:** External CLI wrappers (Waifu2x, RealCUGAN, RealESRGAN, GFPGAN)
- **Services:** Preset management, cloud sharing
- **Utils:** AI tools, keyboard shortcuts, localization, window state, magenta colorkey

---

## Key Design Decisions

### SPRITE-001: Transparency & Colorkey Integrity
RO BMP sprites use `#FF00FF` (magenta) as transparency. Conversion strictly follows:
1. `MagentaToAlpha` before scaling
2. `AlphaToMagenta` after shading
This prevents `#FE01FE` (near-magenta) artifacts at sprite edges.

### THREAD-001: UI Thread Protection
Background processing runs on `Parallel.ForEachAsync` or `Task.Run`. UI updates are **always** dispatched via `Dispatcher.InvokeAsync`. Direct cross-thread UI access is prohibited.

### MEMORY-001: Zero Resource Leak
Every `Bitmap`, `Graphics`, `BitmapData`, and unmanaged pointer must be disposed via `using` or `try/finally`. `LockBits`/`UnlockBits` pairs are mandatory.

### Platform Independence
`RoSpriteEnhancer.Core` targets `net8.0` (no WPF dependencies). The WPF UI targets `net8.0-windows`. Core types (`Bitmap` from `System.Drawing.Common`) are used consistently.

### EnableDefaultCompileItems = false
Every `.cs` file must be explicitly listed in `<Compile Include>` in the `.csproj`. Missing files cause CS0246 build errors.

---

## Data Flow

```
User selects Input Folder
       │
       ▼
BatchProcessor.GetFiles() → file list
       │
       ▼
Parallel.ForEachAsync (MaxDegreeOfParallelism = CPU-1)
       │
       ├─► LoadBitmap(filePath)
       ├─► ResolveCategoryIdWithContentFallback()  ← path first, content AI fallback
       ├─► ProcessBitmapPreview()
       │     ├─► DetailExtractor.ExtractDetailMap()
│     ├─► MagentaHandler.MagentaToAlpha()
│     ├─► SuperSampler / XBRZ / Scale2x / AI Runner
│     ├─► PixelEnhancer (outline, shadow, highlight, HSL)
│     ├─► DetailExtractor.InjectDetail()
│     ├─► MagentaHandler.AlphaToMagenta()
│     ├─► PaletteQuantizer.Quantize() (if 8-bit output)
│     └─► QualityChecker.CheckQuality() ← blur, transparency, color-shift
       │
       ▼
SaveBitmap(outputPath, format)
       │
       ▼
Progress + Quality Report dialog
```

---

## Threading Model

| Component | Threading | UI Access |
|-----------|-----------|-----------|
| MainWindow | UI thread | Direct |
| BatchProcessor | `Parallel.ForEachAsync` | `Dispatcher.InvokeAsync` |
| Waifu2xRunner | External process (SemaphoreSlim limit=1) | `Dispatcher.InvokeAsync` |
| QualityChecker | Same thread as processor | N/A (pure computation) |
| WindowStateService | UI thread (save on Closing) | N/A |

---

## Localization

- **Source:** JSON files in `Locales/` (48 languages)
- **Default:** English
- **Service:** `LanguageService` loads/switches languages at runtime
- **XAML:** Mostly code-behind translation (avoided XAML markup extension parser issues)
- **Fallback:** Missing keys fall back to English

---

## Security

- Zero external network calls in Core library
- External CLI tools executed with timeouts and concurrency limits
- Working directory isolation for tool processes
- Temporary files cleaned via `TryDelete` pattern
- Binary readers validate magic bytes and bounds before processing
- No API keys, tokens, or secrets in source code

---

## Build Configuration

| Setting | Value |
|---------|-------|
| Target Framework (Core) | net8.0 |
| Target Framework (UI) | net8.0-windows |
| Nullable | enabled |
| TreatWarningsAsErrors | true |
| AllowUnsafeBlocks | true |
| EnableDefaultCompileItems | false |
| RuntimeIdentifier | win-x64 |
| Publish | SingleFile, self-contained |

---

## Versioning

SemVer `MAJOR.MINOR.PATCH`. Current: `1.21.0`.
