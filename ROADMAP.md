# ROADMAP.md — RoSpriteEnhancer v1.20.0

## 1. AKTUELLE TASKS

### ✅ TASK-0011: Erstellen von RoSpriteEnhancer.Core/RoSpriteEnhancer.Core.csproj
**Status:** COMPLETED  
**Ergebnis:** Plattformunabhängige Library erstellt  
**Details:**
- TargetFramework: net8.0
- AssemblyName: RoSpriteEnhancer.Core
- Version: 1.20.0

### ✅ TASK-0012: Verschieben aller Core-Dateien in RoSpriteEnhancer.Core/
**Status:** COMPLETED  
**Ergebnis:** Alle Core-Dateien migriert  
**Details:**
- GrfArchive.cs, SprReader.cs, SprWriter.cs, ActFile.cs
- BatchProcessor.cs, Waifu2xRunner.cs, EsrganRunner.cs, RealCuganRunner.cs
- XbrzScaler.cs, Scale2x.cs, SuperSampler.cs, PixelEnhancer.cs
- PaletteQuantizer.cs, MagentaHandler.cs, SpriteFilter.cs
- IncrementalManifest.cs, PresetManager.cs
- BilateralFilter.cs, AlphaBleedCleaner.cs, ColorMatchLock.cs

### ✅ TASK-0013: Aktualisieren von RoSpriteEnhancer.csproj mit Core-Referenz
**Status:** COMPLETED  
**Ergebnis:** WPF-Projekt referenziert Core-Bibliothek  
**Details:**
- ProjectReference auf RoSpriteEnhancer.Core.csproj hinzugefügt
- System.Drawing.Common entfernt aus ItemGroup

### ✅ TASK-0014: Aktualisieren von Unit.csproj mit Core-Referenz
**Status:** COMPLETED  
**Ergebnis:** Test-Projekt konfiguriert  
**Details:**
- ProjectReference auf RoSpriteEnhancer.Core.csproj hinzugefügt
- xUnit, Microsoft.NET.Test.Sdk installiert
- TargetFramework: net8.0-windows (WPF-kompatibel)

### ✅ TASK-0015: Build & Test Validierung
**Status:** COMPLETED  
**Ergebnis:** BUILD SUCCESS (nur Warnungen)  
**Details:**
- RoSpriteEnhancer.Core: Build erfolgreich
- RoSpriteEnhancer: Build erfolgreich
- Unit Tests: 15/15 PASSED
- Nur CA1416 und CS0436 Warnungen (nicht kritisch)

### ✅ TASK-0016: Waifu2x Neuintegration in Core-Bibliothek
**Status:** COMPLETED  
**Ergebnis:** Waifu2xRunner.cs mit Upscale() Methode implementiert  
**Details:**
- Statische Methoden für Tool-Pfade und Process-Ausführung
- Waifu2xOptions-Klasse integriert
- SuperSampler und PixelEnhancer Integration
- Semaphore Concurrency Protection (ConcurrencyLimit = 1)

### ✅ TASK-0017: Unit-Tests für Core-Komponenten erstellen
**Status:** COMPLETED  
**Ergebnis:** Alle Core-Tests implementiert und validiert  
**Details:**
- SprTests.cs: 2 Tests (SprFile_Load_InvalidMagic_ThrowsInvalidDataException, SprWriter_WriteAndLoad_32BitRgbaRoundtrip_Success)
- AiRunnerTests.cs: 2 Tests (Waifu2xRunner_InvalidPath, RealCuganRunner_InvalidPath)
- ScalerTests.cs: 3 Tests (Scale2x, XbrzScaler, BilateralFilter)
- QuantizerAndMagentaTests.cs: 2 Tests (MagentaHandler, PaletteQuantizer)
- ManifestAndFilterTests.cs: 2 Tests (IncrementalManifest, SpriteFilter)
- ActTests.cs: 2 Tests (TryScaleOffsets_InvalidMagic, TryScaleOffsets_ValidMinimalAct)
- GrfTests.cs: 2 Tests (GrfArchive_PackAndOpenFolder, GrfArchive_Open_InvalidMagic)
- **Total: 15 Tests - ALL PASSING**

### ✅ TASK-0018: Unit-Tests ausführen und validieren
**Status:** COMPLETED  
**Ergebnis:** Alle Tests bestanden  
**Details:**
- `dotnet test tests/Unit/Unit.csproj -c Debug` → 15/15 PASSED
- Code Coverage: Kernkomponenten abgedeckt
- Keine neuen Test-Fehler

---

## 2. ZUKÜNFTIGE TASKS

### 🔄 TASK-0019: Release-Vorbereitung v1.20.0
**Status:** COMPLETED  
**Priorität:** HIGH  
**Abhängigkeiten:** TASK-0018  
**Definition of Done:**
- ✅ Build von Release-Profil (dotnet publish -c Release -r win-x86 --self-contained true)
- ✅ Workspace bereinigt (.workspace_temp/)
- ✅ CHANGELOG.md aktualisiert (Added, Changed, Fixed, Security)
- ✅ Version auf 1.20.0 gesetzt (bereits in Core.csproj)
- ✅ Release-Paket erstellt in release_final/ReleasePackage.zip (60.6 MB, verifiziert)

### ⏳ TASK-0020: Performance-Optimierung
**Priorität:** MEDIUM  **Abhängigkeiten:** TASK-0019  **Definition of Done:**
- Batch-Processing optimiert
- Memory Leaks behoben
- Thread-Safety verifiziert
- Benchmarks in .workspace_temp/benchmarks/

### ✅ TASK-0021: SettingsWindow erstellen (Grundgerüst)
**Status:** COMPLETED  **Priorität:** HIGH  **Abhängigkeiten:** TASK-0020  **Ergebnis:** SettingsWindow mit 5 Tabs erstellt  **Details:**
- SettingsWindow.xaml mit TabControl (5 Tabs) erstellt
- SettingsWindow.xaml.cs Grundgerüst mit Fenster-Logik
- Fenster öffnet sich als Dialog über MainWindow

### ✅ TASK-0022: Tab "Allgemein" umsetzen
**Priorität:** HIGH  **Abhängigkeiten:** TASK-0021  **Definition of Done:**
- Presets (Speichern/Laden/Löschen)
- Ordner-Pfade (Eingabe/Ausgabe)
- Dateitypen-Filter (BMP/PNG/TGA/SPR, Unterordner)
- Ausgabeformat (PNG/BMP Magenta/8-bit)
- Inkrementeller Modus

### ✅ TASK-0023: Tab "Skalierung & KI" umsetzen
**Priorität:** HIGH  **Abhängigkeiten:** TASK-0022  **Definition of Done:**
- CmbScale (Scale2x/4x, XBRZ, ESRGAN, Waifu2x, RealCUGAN, AI-First)
- Waifu2x-Einstellungen (Pfad, Modell, Noise)
- RealCUGAN-Einstellungen (Pfad, Modell, Denoise)
- ESRGAN-Einstellungen (Pfad, Modell)
- AI-First-Einstellungen (Primär/Fallback, Modelle, Zielgröße, Logging)
- SSAA (Super-Sampler) Einstellungen

### ✅ TASK-0024: Tab "Pixel-Shading & Effekte" umsetzen
**Priorität:** HIGH  **Abhängigkeiten:** TASK-0023  **Definition of Done:**
- Outline (An, Stärke, Modus)
- Shadow (An, Stärke)
- Highlight (An, Stärke)
- Sharpen (An, Stärke)
- Volumetric3D (An, Stärke)
- HSL (Hue, Sättigung, Helligkeit)
- Ausschluss-Farbe (ARGB Hex)

### ✅ TASK-0025: Tab "Kategorien" umsetzen
**Priorität:** HIGH  **Abhängigkeiten:** TASK-0024  **Definition of Done:**
- Kategorie-Overrides aktivieren/deaktivieren
- Kategorie-Picker mit eigenen Werten
- CategoryOverride-Pro-Kategorie Slider (Outline, Shadow, Highlight, Sharpen, Volumetric3D)

### ✅ TASK-0026: Tab "Animation" umsetzen
**Priorität:** MEDIUM  **Abhängigkeiten:** TASK-0025  **Definition of Done:**
- ACT-Anker mitskalieren Checkbox

### ✅ TASK-0027: BuildOptions() auslagern und Settings-Persistence
**Priorität:** HIGH  **Abhängigkeiten:** TASK-0026  **Definition of Done:**
- BuildOptions() vollständig in SettingsWindow.xaml.cs
- Preset laden/speichern/anwenden funktioniert
- SettingsWindow gibt ProcessingOptions an MainWindow zurück

### ✅ TASK-0028: MainWindow entzerren (Settings-Panel entfernen)
**Priorität:** HIGH  **Abhängigkeiten:** TASK-0027  **Definition of Done:**
- Linkes Settings-Panel aus MainWindow.xaml entfernt
- Grid neu aufgeteilt (breiterer Vorschau-Bereich)
- ⚙️ Einstellungen-Button in Titel-/Statusleiste hinzugefügt

### ✅ TASK-0029: MainWindow Code-Behind anpassen
**Priorität:** HIGH  **Abhängigkeiten:** TASK-0028  **Definition of Done:**
- SettingsWindow öffnen/schließen über Button
- BuildOptions von SettingsWindow beziehen
- Alle Event-Handler korrekt verbunden

### ✅ TASK-0030: Unit-Tests für Settings
**Priorität:** MEDIUM  **Abhängigkeiten:** TASK-0029  **Definition of Done:**
- Preset Roundtrip-Test (Speichern → Laden → Gleich)
- BuildOptions Mapping-Test (UI-Werte → ProcessingOptions)
- 100% Tests PASS

### ✅ TASK-0031: Build & Test Validierung
**Priorität:** HIGH  **Abhängigkeiten:** TASK-0030  **Definition of Done:**
- dotnet build 0 Errors
- dotnet test 100% PASS
- QA-Freigabe erteilt

---

## 2. ZUKÜNFTIGE TASKS

### ⏳ TASK-0032: Performance-Optimierung (nach Refactoring)
**Priorität:** MEDIUM  **Abhängigkeiten:** TASK-0031  **Definition of Done:**
- Batch-Processing optimiert
- Memory Leaks behoben
- Thread-Safety verifiziert

---

## 3. VERSION HISTORY

### v1.20.0 (Aktuell - In Release-Vorbereitung)
- Migration zur Core-Bibliothek abgeschlossen (net8.0)
- WPF-GUI bleibt plattformabhängig (net8.0-windows)
- Waifu2x-Integration vollständig in Core
- RealCuganRunner hinzugefügt
- BilateralFilter für Pre-Processing integriert
- Alle Unit Tests implementiert (15 Tests, 100% PASS)
- Build erfolgreich - nur Warnungen
- .NET 8.0 Windows Targeting konsistent

### v1.19.0 (Vorgänger)
- Migration zur Core-Bibliothek abgeschlossen
- Waifu2x-Integration in Core
- Build erfolgreich

### v1.18.0
- WPF-GUI mit Split-Slider, Zoom/Pan, GRF-Steuerung
- Batch-Processing mit Progress-Indikator
- Pixel-Shading und Palette-Quantisierung

---

## 4. KRITISCHE NOTIZEN

- **Core-Bibliothek ist plattformunabhängig** (net8.0)
- **WPF-GUI bleibt plattformabhängig** (net8.0-windows)
- **Waifu2x-Integration ist vollständig** in Core
- **RealCuganRunner** für alternative AI-Upscaling verfügbar
- **Build ist erfolgreich** - nur Warnungen vorhanden (CA1416, CS0436)
- **Alle 15 Unit Tests PASS** - Quality Gate erfüllt
- **TargetFramework Konsistenz:** Unit.csproj nutzt net8.0-windows für WPF-Tests

---

## 5. QUALITY GATES STATUS

| Gate | Status | Details |
|------|--------|---------|
| **DoR (QUALITY-001)** | ✅ PASSED | Alle Tasks hatten klare Ziele, Dependencies, Teststrategie |
| **DoD (QUALITY-002)** | ✅ PASSED | Code compiles, 15/15 Tests PASS, keine Temp-Files, Docs updated |
| **Build** | ✅ PASSED | `dotnet build RoSpriteEnhancer.sln` - 0 Errors |
| **Tests** | ✅ PASSED | `dotnet test tests/Unit/Unit.csproj` - 15/15 PASS |
| **Architecture** | ✅ PASSED | Core/UI Trennung gewahrt, keine Breaking Changes |

---

**Letzte Aktualisierung:** 2026-08-15  
**Version:** 1.20.0  
**Status:** Core Engine Verified, All Tests Passing, Ready for Release Prep (TASK-0019)