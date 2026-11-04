# CHANGELOG - RoSpriteEnhancer

## [1.20.0] - 2026-08-15
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
- **Project References:** RoSpriteEnhancer.csproj and Unit.csproj reference RoSpriteEnhancer.Core.csproj
- **BatchProcessor:** Uses Parallel.ForEachAsync with MaxDegreeOfParallelism = Environment.ProcessorCount - 1
- **TargetFrameworks:** Unit.csproj targets net8.0-windows for WPF test compatibility
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

## [1.19.0] - 2026-08-07
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