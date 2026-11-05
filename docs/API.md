# API Documentation — RoSpriteEnhancer v1.21.0

## Overview

RoSpriteEnhancer consists of two layers:
- **Core** (`RoSpriteEnhancer.Core`): Platform-independent .NET 8 library (net8.0)
- **UI** (`RoSpriteEnhancer`): WPF application (net8.0-windows)

---

## Core Namespace: `RoSpriteEnhancer.Core`

### Processing

| Class | Namespace | Description |
|-------|-----------|-------------|
| `BatchProcessor` | `RoSpriteEnhancer.Core.Processing` | Multi-threaded batch processing pipeline |
| `DetailExtractor` | `RoSpriteEnhancer.Core.Processing` | Laplacian high-pass detail map injection |
| `QualityChecker` | `RoSpriteEnhancer.Core.Processing` | Automatic quality assessment (blur, transparency, color shift) |
| `SpriteAtlasGenerator` | `RoSpriteEnhancer.Core.Processing` | Spritesheet/packer generator |
| `SpriteContentClassifier` | `RoSpriteEnhancer.Core.Processing` | Content-based category classification |
| `SpriteResolutionProfile` | `RoSpriteEnhancer.Core.Processing` | Resolution recommendations per sprite category |
| `UpscalerPluginManager` | `RoSpriteEnhancer.Core.Processing` | External DLL upscaler plugin loader |

#### BatchProcessor

```csharp
public enum OutputFormat { Png, Bmp_MagentaKey, Bmp_Indexed8_Palette, Tga_Uncompressed }
public enum ScaleAlgorithm { Scale2x, XbrzX2, XbrzX4, Waifu2xAnime, Waifu4xAnime, ... }

public class ProcessingOptions
{
    public string InputFolder { get; set; } = "";
    public string OutputFolder { get; set; } = "";
    public int ScalePasses { get; set; } = 1;
    public ScaleAlgorithm ScaleAlgorithm { get; set; } = ScaleAlgorithm.Scale2x;
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Png;
    public int OutputQuality { get; set; } = 9;
    public bool IncludeBmp { get; set; } = true;
    public bool IncludePng { get; set; } = true;
    public bool IncludeTga { get; set; } = true;
    public bool IncludeSpr { get; set; } = true;
    public bool ScaleActAnchors { get; set; } = false;
    public bool Subfolders { get; set; } = true;
    public bool HandleMagentaKey { get; set; } = true;
    public int AlphaThreshold { get; set; } = 128;
}

public class BatchProcessor
{
    public static event Action<string>? LogMessage;
    public List<QualityReport> QualityReports { get; } = new();
    public CancellationTokenSource? Cts { get; set; }

    public void Cancel();
    public bool IsPaused { get; }
    public void Pause();
    public void Resume();

    public static Bitmap ProcessBitmapPreview(Bitmap src, ProcessingOptions opts, string? categoryId = null, EnhancerOptions? enhancerOverride = null);
    public static string ResolveCategoryId(string relativePath);
    public static string ResolveCategoryIdWithContentFallback(string relativePath, Bitmap? bmp);
    public static EnhancerOptions ResolveEnhancerOptions(string relativePath, ProcessingOptions opts);
}
```

### Filtering

| Class | Namespace | Description |
|-------|-----------|-------------|
| `BilateralFilter` | `RoSpriteEnhancer.Core.Filtering` | Edge-preserving pre-smoothing |
| `PaletteQuantizer` | `RoSpriteEnhancer.Core.Filtering` | Median-Cut 8-bit quantization |
| `PixelEnhancer` | `RoSpriteEnhancer.Core.Filtering` | Outline, shadow, highlight, sharpen, HSL |
| `SuperSampler` | `RoSpriteEnhancer.Core.Filtering` | SSAA downscaler (Lanczos3, Bicubic, Bilinear) |
| `Volumetric3DEffect` | `RoSpriteEnhancer.Core.Filtering` | Plastic 3D body effect |

### Upscaling

| Class | Namespace | Description |
|-------|-----------|-------------|
| `Waifu2xRunner` | `RoSpriteEnhancer.Core.Upscaling` | Waifu2x-ncnn-vulkan CLI wrapper |
| `RealCuganRunner` | `RoSpriteEnhancer.Core.Upscaling` | Real-CUGAN-ncnn-vulkan CLI wrapper |
| `RealESRGANRunner` | `RoSpriteEnhancer.Core.Upscaling` | RealESRGAN ncnn Vulkan wrapper |
| `GFPGANRunner` | `RoSpriteEnhancer.Core.Upscaling` | GFPGAN face restoration wrapper |

#### Waifu2xOptions

```csharp
public class Waifu2xOptions
{
    public string ExecutablePath { get; set; } = "";
    public string ModelPath { get; set; } = "models-cunet";
    public int NoiseLevel { get; set; } = -1;
    public int ScaleFactor { get; set; } = 2;
    public int TimeoutSeconds { get; set; } = 60;
    public bool ForceCpuMode { get; set; } = false;
}
```

#### RealESRGANRunner

```csharp
public class RealESRGANOptions
{
    public string ExecutablePath { get; set; } = "";
    public string ModelPath { get; set; } = "realesrgan-x4plus-anime";
    public int ScaleFactor { get; set; } = 4;
    public int TileSize { get; set; } = 0;
    public int TilePad { get; set; } = 4;
    public bool PrePad { get; set; } = false;
    public bool UpscaleOnly { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 120;
    public bool UseGpu { get; set; } = true;
}

public static class RealESRGANRunner
{
    public static string LastError { get; private set; } = "";
    public static Bitmap? Upscale(Bitmap src, RealESRGANOptions opts);
    public static string? AutoDetectExecutable();
    public static bool ModelExists(string modelPath);
}
```

### IO

| Class | Namespace | Description |
|-------|-----------|-------------|
| `GrfArchive` | `RoSpriteEnhancer.Core.IO` | GRF v2.0 reader/extractor/packer/merger |
| `SprReader` | `RoSpriteEnhancer.Core.IO` | SPR v1.1-v2.1 binary parser |
| `SprWriter` | `RoSpriteEnhancer.Core.IO` | SPR binary encoder |

### Services

| Class | Namespace | Description |
|-------|-----------|-------------|
| `PresetManager` | `RoSpriteEnhancer.Core.Services` | JSON preset save/load/list/delete |
| `PresetSharingService` | `RoSpriteEnhancer.Core.Services` | HTTP-based preset upload/download/list |

### Utils

| Class | Namespace | Description |
|-------|-----------|-------------|
| `AiToolManager` | `RoSpriteEnhancer.Core.Utils` | AI tool detection and download URLs |
| `AlphaBleedCleaner` | `RoSpriteEnhancer.Core.Utils` | Alpha channel post-cleanup |
| `ColorMatchLock` | `RoSpriteEnhancer.Core.Utils` | Cross-pipeline color consistency |
| `KeyboardShortcutManager` | `RoSpriteEnhancer.Core.Utils` | Centralized hotkey registry (JSON persistence) |
| `LanguageService` | `RoSpriteEnhancer.Core.Utils` | JSON-based localization service (48 languages) |
| `Logger` | `RoSpriteEnhancer.Core.Utils` | Structured logging (debug, info, warn, error, fatal) |
| `MagentaHandler` | `RoSpriteEnhancer.Core.Utils` | Colorkey `#FF00FF` <-> Alpha conversion |
| `PathHelper` | `RoSpriteEnhancer.Core.Utils` | Central file system operations |
| `ServiceContainer` | `RoSpriteEnhancer.Core.Utils` | DI container for instance-based services |
| `ThemeDetector` | `RoSpriteEnhancer.Core.Utils` | System theme detection (Dark/Light) |
| `ToolInstaller` | `RoSpriteEnhancer.Core.Utils` | Central AI tool installation/detection |
| `UndoRedoManager<T>` | `RoSpriteEnhancer.Core.Utils` | Generic undo/redo manager |
| `WindowStateService` | `RoSpriteEnhancer.Core.Utils` | Window state + recent folders persistence |

### Models

| Class | Namespace | Description |
|-------|-----------|-------------|
| `SpriteFilter` | `RoSpriteEnhancer.Core.Models` | EUC-KR path category matching (16 categories) |

### Scaling

| Class | Namespace | Description |
|-------|-----------|-------------|
| `Scale2x` | `RoSpriteEnhancer.Core.Scaling` | Pure C# pixel-art scaler |
| `XbrzScaler` | `RoSpriteEnhancer.Core.Scaling` | XBRZ 2x/4x scaler |

---

## WPF Namespace: `RoSpriteEnhancer`

### Windows

| Class | File | Description |
|-------|------|-------------|
| `MainWindow` | `MainWindow.xaml.cs` | Main window: file list, preview, batch controls |
| `SettingsWindow` | `SettingsWindow.xaml.cs` | Settings: presets, AI tools, shortcuts, language |
| `AboutWindow` | `AboutWindow.xaml.cs` | About dialog: version, libraries, license |
| `FirstRunWizard` | `FirstRunWizard.xaml.cs` | First-run setup wizard |
| `HelpWindow` | `HelpWindow.xaml.cs` | Built-in tutorials |
| `InputDialog` | `InputDialog.xaml.cs` | Simple modal text input |
| `PluginMarketplaceWindow` | `PluginMarketplaceWindow.xaml.cs` | External plugin management |

### Converters

| Class | File | Description |
|-------|------|-------------|
| `InverseBoolToVisibilityConverter` | `Views/Converters/InverseBoolToVisibilityConverter.cs` | Boolean to visibility converter |
| `RtlSupport` | `Views/Converters/RtlSupport.cs` | RTL language support |

---

## Events & Events

| Event | Namespace | Description |
|-------|-----------|-------------|
| `BatchProcessor.LogMessage` | `RoSpriteEnhancer.Core.Processing` | Log message during batch processing |
| `LanguageService.LanguageChanged` | `RoSpriteEnhancer.Core.Utils` | Language switched at runtime |
| `UndoRedoManager<T>.PropertyChanged` | `RoSpriteEnhancer.Core.Utils` | Undo/Redo state changed |

---

## See Also

- `ARCHITECTURE.md` — System architecture
- `FILE_FORMATS.md` — Supported file formats
- `PLUGINS.md` — Plugin development guide
- `PRESETS.md` — Preset system documentation
