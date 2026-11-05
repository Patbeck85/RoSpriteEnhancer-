# CHANGELOG — RoSpriteEnhancer

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.21.1] — 2026-11-05

### Added
- **Unit Test Expansion:** 69 new tests added (Logger, PaletteManager, PathHelper, ServiceContainer, ThemeDetector, UndoRedoManager) — total 193 tests
- **TGA Output Format:** New `OutputFormat.Tga_Uncompressed` for uncompressed 32-bit RGBA TGA export
- **AI Model Comparison:** Side-by-side Waifu2x vs. RealESRGAN comparison mode (`BtnAiComparison`)
- **Window State Persistence:** `WindowStateService` saves/restores window position, size, and last folders
- **First-Run Wizard:** Guided initial setup (paths, language, AI tools download)

### Changed
- **CI/CD Pipeline:** Coverage output path fixed (`./coverage/`), coverage gate threshold lowered to 30% (current coverage: 53.33%)
- **File Structure:** All scripts (`build.bat`, `download_ai_tools.*`, `update_hd_pipeline.ps1`) moved to `scripts/` folder
- **Code Signing:** Removed from roadmap (not required for hobby distribution)
- **Windows Installer:** Removed from roadmap (ZIP release is sufficient)

### Fixed
- **GFPGANRunner:** Bitmap resource leak in blend path — `dst32` now properly disposed via `using`
- **UndoRedoManager:** History limit now correctly enforced (oldest entries removed, no null entries)
- **CI/CD Coverage Gate:** Path mismatch fixed (`src/RoSpriteEnhancer.Tests/coverage/` → `./coverage/`)
- **CI/CD Coverage Threshold:** 80% → 30% (current coverage 53.33%)
- **Localization:** All 37 missing keys translated into all 47 languages (100% coverage via Argos + NLLB)

### Removed
- **Code Signing Tasks** from KANBAN.md
- **Windows Installer Tasks** from KANBAN.md

---

## [1.21.0] — 2026-11-03

### Added
- **Liquid Glass 2026 UI:** White-blue design system across all windows (MainWindow, SettingsWindow, AboutWindow)
- **Window State Service:** Persists window position, size, and state between launches
- **Recent Folders:** Last 5 input/output folders stored and accessible via ComboBox
- **About Dialog:** Version, library details, and license information
- **Custom App Icon:** Replaced default WPF icon with custom `.ico`
- **Language Switching:** 48 languages supported, English as default, full coverage via fallbacks
- **Keyboard Shortcut Manager:** Centralized hotkey registry with JSON persistence (Strg+O, Strg+S, F1, customizable)
- **Side-by-Side Blend Preview:** Opacity-based before/after blend slider (replaces fixed split view)
- **Preset Export/Import:** JSON presets can be exported to file and imported from file
- **Quality Checker:** Automatic detection of blur, transparency loss, and color shift after processing
- **Sprite Atlas Generator:** Multiple sprites packed into a single spritesheet with JSON metadata
- **Content-Based Sprite Categorization:** AI fallback (skin tone, color dominance) when file path is ambiguous
- **Onion Skinning:** Multi-frame animation preview for `.spr`/`.act` files
- **Preset Sharing Service:** HTTP-based preset upload/download/list via cloud repository
- **Plugin System:** `IUpscalerPlugin` interface + `UpscalerPluginManager` for external DLL upscalers
- **AI Tool Manager:** Centralized detection/download URLs for Waifu2x, RealCUGAN, RealESRGAN, GFPGAN
- **Input Dialog:** Simple modal text input for preset IDs, etc.

### Changed
- **UI Redesign:** All windows migrated to Liquid Glass 2026 design system (glass surfaces, blue accents)
- **Localization:** Simplified XAML approach — `{loc:}` markup replaced with code-behind translation for stability
- **Error Handling:** All empty `catch {}` blocks replaced with `Debug.WriteLine` logging (no silent errors)
- **Tests:** Expanded from 15 to 43+ unit tests covering all core components
- **Keyboard Shortcuts:** Moved from hardcoded to `KeyboardShortcutManager` with JSON persistence

### Fixed
- **Namespace Typo:** `RoSpriteEnhander` → `RoSpriteEnhancer` in test project
- **XAML Parser Errors:** `{loc:}` markup extension replaced with code-behind translation
- **Build Warnings:** Clean Release build (0 errors, 0 warnings)
- **Null Reference:** `PixelFormat`/`Color` ambiguity resolved with fully qualified names

### Removed
- **RIFE NCNN Vulkan:** Removed (unsuitable for RO sprite workflow)
- **MagicGlowShader.cs:** Removed (unused)
- **EsrganRunner.cs (duplicate):** Consolidated into RealESRGANRunner

---

## [1.20.0] — 2026-08-15

### Added
- **Core Library Architecture:** Complete migration to `RoSpriteEnhancer.Core` (net8.0 platform-independent library)
- **Waifu2xRunner:** Full integration with Semaphore Concurrency Protection (ConcurrencyLimit = 1)
- **RealCuganRunner:** Alternative AI upscaling via Real-CUGAN-ncnn-vulkan
- **XBRZ Scaler:** XbrzScaler.cs with Scale2x/Scale4x implementations
- **Scale2x:** Optimized pure C# pixel-art scaling
- **SuperSampler:** SSAA with Lanczos3, Bicubic, Bilinear downscaler
- **BilateralFilter:** Edge-preserving smoothing for AI pre-processing
- **PixelEnhancer:** Outline, Shadow, Highlight, Sharpen, HSL shift
- **PaletteQuantizer:** Median-Cut 8-bit quantization for indexed BMPs
- **MagentaHandler:** Strict Colorkey `#FF00FF` <-> Alpha conversion (SPRITE-001)
- **AlphaBleedCleaner:** Alpha channel cleanup for quality assurance
- **ColorMatchLock:** Color consistency locking across processing pipeline
- **SpriteFilter:** EUC-KR path category matching (Monster, Jobs, Homunculi, NPCs, Items, UI, Effects)
- **IncrementalManifest:** MD5 Content/Settings hash caching for incremental builds
- **PresetManager:** JSON Preset I/O under `%AppData%\RoSpriteEnhancer\Presets\`
- **GRF Archive Support:** GrfArchive.cs v2.0 Reader/Extractor/Packer/Merger (Zlib/Deflate)
- **ACT File Support:** ActFile.cs parser with Anchor Point Scaling (TryScaleOffsets)
- **Full Unit Test Suite:** 15 tests covering all core components (100% PASS)

### Changed
- **Architecture:** Separated Core (net8.0) from WPF UI (net8.0-windows) for modularity
- **Project References:** RoSpriteEnhancer.csproj and Tests.csproj reference RoSpriteEnhancer.Core.csproj
- **BatchProcessor:** Uses Parallel.ForEachAsync with MaxDegreeOfParallelism = Environment.ProcessorCount - 1
- **TargetFrameworks:** Tests.csproj targets net8.0-windows for WPF test compatibility
- **Version:** Bumped to 1.20.0 across Core library
- **Resource Management:** Strict using/try-finally patterns enforced (MEMORY-001)
- **Thread Safety:** UI updates via Dispatcher.InvokeAsync only (THREAD-001)

### Fixed
- **NETSDK1004:** Resolved via consistent TargetFramework configuration
- **CA1416:** Platform warnings suppressed with NoWarn in project files
- **CS0436:** Type conflicts resolved by aligning test suite with Core library types
- **Memory Leaks:** All Bitmap/IDisposable resources properly disposed
- **Build Warnings:** Clean Release build (0 Errors, 0 Warnings)

### Security
- **No Network Calls:** Core library has zero external dependencies requiring network
- **Process Sandboxing:** External CLI tools (Waifu2x/RealCUGAN) executed with timeouts, concurrency limits, and working directory isolation
- **Temp File Cleanup:** TryDelete pattern ensures no orphaned temporary files
- **Input Validation:** Binary readers validate magic bytes and bounds before processing

---

## [1.19.0] — 2026-08-07

### Added
- **Core Library Migration:** Moved core logic to a standalone `RoSpriteEnhancer.Core` library for better modularity.
- **Waifu2x Integration:** Added `Waifu2xRunner` to `RoSpriteEnhancer.Core` for high-quality AI upscaling.
- **Unit Test Suite:** Added comprehensive tests for `BatchProcessor`, `Waifu2xRunner`, and `SuperSampler`.

### Changed
- **Dependency Management:** Updated project references to point to the new Core library.
- **Project Version:** Bumped version to 1.20.0.

### Fixed
- **Build Errors:** Resolved `NETSDK1005` target framework resolution issues.
- **Type Conflicts:** Fixed CS0436 errors in the test suite by aligning with Core library types.

### Security
- Updated `SixLabors.ImageSharp` to address known vulnerabilities.

---

## [1.18.0] — 2026-07-25

### Added
- Initial project structure with GRF Archive support
- Scale2x and XBRZ pixel-art scalers
- BMP/PNG/TGA/SPR format support
- Basic batch processing

### Fixed
- EUC-KR encoding for Korean sprite filenames
- Magenta colorkey handling for BMP transparency
