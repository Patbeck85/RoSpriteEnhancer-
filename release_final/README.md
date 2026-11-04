# RO Sprite Enhancer v1.20.0
### GPU-freier Pixel-Art Upscaler für Ragnarok Online Sprites

**Speziell entwickelt für alte Hardware — läuft auf Nvidia 8600 GT und schwächer.**

---

## ✨ Features

| Feature | Beschreibung |
|---|---|
| **Scale2x / Scale4x Algorithmus** | Pixel-Art-optimierter Upscaler — kein GPU nötig |
| **XBRZ 2x / 4x** | Advanced Pixel-Art Scaling mit Edge Detection |
| **Waifu2x-ncnn-vulkan Integration** | AI-Anime-Upscaling (2x/4x) via lokalem CLI Tool |
| **Real-CUGAN Integration** | Alternative AI-Upscaling für Anime-Sprites |
| **Bilateral Pre-Filter** | Rauschreduktion vor AI-Upscaling für sauberere Ergebnisse |
| **2x oder 4x Skalierung** | 2x empfohlen für alte Hardware |
| **Outline-Verstärkung** | Konturen werden schärfer und definierter |
| **Drop-Shadow** | Subtiler Schatten für mehr Tiefenwirkung |
| **Highlight-Boost** | Glanzeffekte auf Rüstungen verstärkt |
| **Nachschärfung** | Optionaler Schärfefilter |
| **HSL Farbkorrektur** | Hue/Saturation/Lightness Anpassung |
| **Palette-Quantisierung** | Median-Cut 8-bit für indizierte BMPs |
| **Magenta Colorkey Schutz** | `#FF00FF` Transparenz bleibt strikt erhalten |
| **ACT Animation Anker-Skalierung** | Offsets werden proportional mitgeskaliert |
| **Inkrementelles Caching** | MD5-basiert - unveränderte Dateien werden übersprungen |
| **Before/After Vorschau** | Slider-Vergleich in Echtzeit mit Zoom/Pan |
| **Batch-Verarbeitung** | Ganze Ordner automatisch verarbeiten (Multi-Thread) |
| **BMP / PNG / TGA / SPR** | Alle gängigen RO-Sprite-Formate |
| **GRF Archive Support** | Lesen, Extrahieren, Packen, Mergen (v2.0 Zlib/Deflate) |
| **Kategorie-Presets** | Monster, Jobs, Homunculi, NPCs, Items, UI, Effects |
| **JSON Preset System** | Speichern/Laden von Konfigurationen |

---

## 🔧 Workflow

### 1. Sprites extrahieren (mit GRF Editor)
```
GRF Editor öffnen → deine .grf Datei laden
→ data/sprite Ordner auswählen
→ Rechtsklick → Extract → Ordner wählen
```

### 2. RO Sprite Enhancer
```
1. Eingabe-Ordner: extrahierter sprite Ordner
2. Ausgabe-Ordner: z.B. sprite_enhanced
3. Einstellungen anpassen (2x Scale2x empfohlen für alte HW, Waifu2x/RealCUGAN für moderne GPU)
4. Sprite aus Liste auswählen → Vorschau prüfen
5. "BATCH VERARBEITUNG STARTEN"
```

### 3. Zurück in GRF einpacken (GRF Editor)
```
GRF Editor → Merge/Add Files
→ sprite_enhanced Ordner auswählen
→ Save GRF
```

---

## ⚡ Performance

| Hardware | Algorithmus | Geschwindigkeit (ca.) | RAM |
|---|---|---|---|
| **Nvidia 8600 GT** | Scale2x 2x | ~50-200 Sprites/Min | ~256 MB |
| **Nvidia 8600 GT** | Scale2x 4x | ~10-30 Sprites/Min | ~512 MB |
| **Moderne GPU (RTX 2060+)** | Waifu2x 2x | ~100-300 Sprites/Min | ~1-2 GB VRAM |
| **Moderne GPU (RTX 2060+)** | Waifu2x 4x (2-pass) | ~30-80 Sprites/Min | ~2-4 GB VRAM |
| **Moderne GPU (RTX 2060+)** | RealCUGAN 2x | ~80-200 Sprites/Min | ~1-2 GB VRAM |

**Empfehlung für alte Hardware:** Scale2x 2x ist für RO mehr als ausreichend und läuft flüssig ohne GPU.
**Empfehlung für moderne Hardware:** Waifu2x (Noise: -1, Model: models-cunet) für beste Anime-Qualität.

---

## 🏗️ Build

Voraussetzungen: .NET 8 SDK

```batch
build.bat
```

Oder manuell:
```batch
dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -o bin\Release
```

---

## 📦 Release Package

Das Release-Paket enthält:
- `RoSpriteEnhancer.exe` — Standalone Single-File Executable (~144 MB)
- Native .NET 8 Runtime Libraries (D3DCompiler, PenImc, PresentationNative, vcruntime, wpfgfx)
- Keine .NET Installation auf Zielsystem erforderlich

---

## ⚙️ Architecture

```
RoSpriteEnhancer.sln
├── RoSpriteEnhancer/              # WPF UI (net8.0-windows)
│   ├── MainWindow.xaml/.cs        # Hauptfenster, Split-Slider, Zoom/Pan
│   ├── App.xaml/.cs               # Resources, EUC-KR Encoding
│   └── FileItem.cs                # Data Binding Model
├── RoSpriteEnhancer.Core/         # Core Engine (net8.0 - plattformunabhängig)
│   ├── GrfArchive.cs              # GRF v2.0 Reader/Writer
│   ├── SprReader/SprWriter.cs     # SPR v1.1-v2.1 Parser/Encoder
│   ├── ActFile.cs                 # ACT Animation Parser & Anchor Scaler
│   ├── BatchProcessor.cs          # Parallel.ForEachAsync Pipeline
│   ├── Waifu2xRunner.cs           # Waifu2x-ncnn-vulkan Wrapper
│   ├── RealCuganRunner.cs         # Real-CUGAN-ncnn-vulkan Wrapper
│   ├── XbrzScaler.cs / Scale2x.cs # Pure C# Pixel-Art Scaler
│   ├── SuperSampler.cs            # SSAA (Lanczos3, Bicubic, Bilinear)
│   ├── PixelEnhancer.cs           # Shading (Outline, Shadow, Highlight, Sharpen, HSL)
│   ├── PaletteQuantizer.cs        # Median-Cut 8-bit Quantizer
│   ├── MagentaHandler.cs          # Colorkey #FF00FF <-> Alpha
│   ├── SpriteFilter.cs            # EUC-KR Path Category Matching
│   ├── IncrementalManifest.cs     # MD5 Content/Settings Hash Caching
│   ├── PresetManager.cs           # JSON Preset I/O
│   ├── BilateralFilter.cs         # Edge-preserving Smoothing
│   ├── AlphaBleedCleaner.cs       # Alpha Channel Cleanup
│   └── ColorMatchLock.cs          # Color Consistency Locking
└── tests/Unit/                    # xUnit Test Suite (15 Tests)
    ├── SprTests.cs
    ├── AiRunnerTests.cs
    ├── ScalerTests.cs
    ├── QuantizerAndMagentaTests.cs
    ├── ManifestAndFilterTests.cs
    ├── ActTests.cs
    └── GrfTests.cs
```

---

## 📌 Bekannte Einschränkungen

- **SPR-Format** (RO natives Format): Muss zuerst mit GRF Editor zu BMP/PNG extrahiert werden, oder direkt via BatchProcessor verarbeitet werden (Read/Write SPR implementiert)
- **Magenta Colorkey** (`0x00FF00FF`): Wird als Transparenz erkannt — bleibt durch MagentaHandler strikt erhalten
- **Animationen**: Jedes Frame wird einzeln verarbeitet — funktioniert korrekt
- **ACT Dateien**: Ankerskalierung nur bei aktivierter Option `ScaleActAnchors` + korrektem Skalierungsfaktor
- **EUC-KR Dateinamen**: Koreanische Pfade werden via `SpriteFilter` korrekt gematcht (nicht umbenennen!)
- **Waifu2x/RealCUGAN**: Benötigen lokale CLI Tools (`waifu2x-ncnn-vulkan.exe`, `realcugan-ncnn-vulkan.exe`) im App-Verzeichnis oder `tools/waifu2x/`, `tools/realcugan/`
- **4K Texturen**: Client-seitig auf 512x512 oder 1024x1024 gedrosselt empfohlen (Texture Atlas Limits)

---

## 📋 Changelog v1.20.0

### Added
- Core Library Architecture (net8.0 platform-independent)
- Waifu2xRunner with Semaphore Concurrency Protection
- RealCuganRunner for alternative AI upscaling
- BilateralFilter for pre-processing noise reduction
- XBRZ 2x/4x Scaler implementations
- SuperSampler with Lanczos3/Bicubic/Bilinear filters
- AlphaBleedCleaner & ColorMatchLock for quality assurance
- IncrementalManifest MD5 caching system
- SpriteFilter with EUC-KR category matching (Monster, Jobs, Homunculi, NPCs, Items, UI, Effects)
- Full Unit Test Suite (15 tests, 100% passing)
- GRF Archive v2.0 Read/Extract/Pack/Merge support

### Changed
- Migrated all core logic from WPF project to RoSpriteEnhancer.Core library
- Unit.csproj targets net8.0-windows for WPF test compatibility
- BatchProcessor uses Parallel.ForEachAsync with MaxDegreeOfParallelism = CPU-1
- MagentaHandler strictly enforces Colorkey integrity (SPRITE-001)
- ACT Offset scaling formula: New_Offset = Original_Offset * (New_Width / Original_Width)

### Fixed
- NETSDK1004 build error resolved via consistent TargetFrameworks
- CA1416 Platform warnings suppressed where appropriate
- Resource disposal patterns enforced (MEMORY-001)
- UI Thread protection via Dispatcher.InvokeAsync (THREAD-001)

### Security
- No external network calls in core library
- Process execution sandboxed with timeouts and concurrency limits
- Temporary files cleaned up via TryDelete pattern

---

## 📄 License

Teil des Yggdrasil.Studio Ökosystems — Open Source für die RO Community.