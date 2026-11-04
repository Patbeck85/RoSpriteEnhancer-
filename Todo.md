# 🚀 RoSpriteEnhancer - Entwicklung Todo Liste

## 📋 Sprint 1: Memory Safety & Thread Compliance (Priorität: KRITISCH)

### 🔴 Memory Leak Fixes
- [ ] **MainWindow.xaml.cs** - Vorschau-Timer prüft auf alte Bitmaps vor neuem Frame laden
- [ ] **SuperSampler.cs** - Zwischen-Bitmaps (`preScaled`, `scaled`) sofort dispose nach Verarbeitung
- [ ] **BatchProcessor.cs** - Temporäre Puffer im `finally`-Block freigeben
- [ ] **EsrganRunner.cs** - GPU-Handles vor Abschluss des AI-Jobs schließen

### 🔴 Thread Safety Audit
- [ ] **MainWindow.xaml.cs** - Alle WPF-Zugriffe aus Hintergrund-Threads über `Dispatcher.InvokeAsync`
- [ ] **BatchProcessor.cs** - `Parallel.ForEachAsync` mit korrektem `CancellationToken`
- [ ] **SuperSampler.cs** - Keine statischen Felder, die thread-sicher sein müssen

### 🔴 RO Compatibility Checks
- [ ] **GrfArchive.cs** - EUC-KR Encoding bei allen Datei-Pfad-Operationen erzwingen
- [ ] **SprWriter.cs** - Magenta `#FF00FF` Slot 0 Transparenz validieren vor Export
- [ ] **ActFile.cs** - Proportionale ACT-Anker-Skalierung prüfen

---

## 📋 Sprint 2: Performance Optimization (Priorität: HOCH)

### ⚡ GPU Concurrency
- [ ] **EsrganRunner.cs** - `SemaphoreSlim` für GPU-Queue-Limit (max 2 parallele Jobs)
- [ ] **BatchProcessor.cs** - MD5 Hash Caching in `IncrementalManifest.cs` nutzen

### ⚡ Memory Pooling
- [ ] **SuperSampler.cs** - Bitmap-Pool für wiederkehrende Größen (128x128, 256x256, 512x512)
- [ ] **PixelEnhancer.cs** - Temporäre Puffer im `using`-Block

---

## 📋 Sprint 3: Feature Enhancements (Priorität: MITTEL)

### ✨ Neue Upscaler
- [ ] **ESRGAN Runner** - Option für Real-ESRGAN CLI als Alternative
- [ ] **SuperSampler** - Lanczos4 als Downscaler-Option hinzufügen

### ✨ UI Improvements
- [ ] **MainWindow.xaml** - Progress Bar für Batch-Jobs
- [ ] **MainWindow.xaml** - Context Menu für "Export as HD BMP" vs "Export as GRF"

### ✨ Preset System
- [ ] **PresetManager.cs** - JSON-Preset mit allen Parametern (Upscaler, Dithering, Quantization)
- [ ] **App.xaml** - Preset-Ordner `%AppData%/RoSpriteEnhancer/Presets` registrieren

---

## 📋 Sprint 4: Bug Fixes & Edge Cases (Priorität: MITTEL)

### 🐛 Fehlerhafte Dateien
- [ ] **GrfArchive.cs** - Korrupte GRF-Archive gracefully skippen (nicht entire Batch stoppen)
- [ ] **SprReader.cs** - Unbekannte SPR-Versionen mit Warnung statt Exception

### 🐛 Encoding Issues
- [ ] **SpriteFilter.cs** - EUC-KR Decodierung bei allen Datei-Pfaden erzwingen
- [ ] **BatchProcessor.cs** - `surrogateescape` bei allen String-Operationen

---

## 📋 Sprint 5: Code Quality & Architecture (Priorität: NIEDRIG)

### 🧹 Refactoring
- [ ] **SuperSampler.cs** - Extrahiere `DownscaleToShading()` als separate Methode
- [ ] **PixelEnhancer.cs** - Extrahiere `ApplyDropShadow()` und `ApplyRimLight()`

### 📚 Documentation
- [ ] **README.md** - Setup Guide für ESRGAN GPU-Installation
- [ ] **GUIDE.md** - API-Dokumentation für alle Core-Module

---

## 📋 Sprint 6: Hermes Agent SOUL.md Enhancement (Priorität: HOCH)

### 🤖 SOUL.md Verbesserungen
- [ ] **Persona Section** - Klare Definition von Hermes Agent vs Gemini Role
- [ ] **Consultation Protocol** - Konkrete Prompt-Templates für jede Konsultationsart
- [ ] **Technical DNA** - Vollständige Codebase-Struktur mit Dateigrößen und Abhängigkeiten
- [ ] **Unbreakable Rules** - Memory Safety, Thread Safety, RO Compatibility als Checkliste

### 🔄 SOUL.md Maintenance
- [ ] Nach jedem Sprint: SOUL.md aktualisieren mit neuen Features/Regeln
- [ ] `.hermes_cache/soul_backup.md` als Referenz für Versionierung
- [ ] SOUL.md Größe limitieren auf < 20KB (automatische Komprimierung bei Wachstum)

---

## 📋 Sprint 7: Testing & Validation (Priorität: MITTEL)

### 🧪 Unit Tests
- [ ] **GrfArchiveTests.cs** - GRF Read/Write/Extract Tests
- [ ] **SuperSamplerTests.cs** - SSAA Pipeline mit Test-Bildern
- [ ] **MagentaHandlerTests.cs** - Color-Key ↔ Alpha Konvertierung

### 🧪 Integration Tests
- [ ] **BatchProcessorTests.cs** - Parallel Processing mit Test-Dateien
- [ ] **EsrganRunnerTests.cs** - GPU Concurrency Limits

---

## 📋 Sprint 8: Advanced Features (Priorität: NIEDRIG)

### 🔮 AI Enhancements
- [ ] **SuperSampler** - Option für Stable Diffusion Inpainting bei kaputten Sprites
- [ ] **PixelEnhancer** - Neural Style Transfer für konsistentes Look-and-Feel

### 🎨 Creative Tools
- [ ] **SpriteFilter** - KI-basierte Kategorie-Erkennung (Monster, NPC, Item)
- [ ] **PresetManager** - KI-generierte Presets basierend auf Sprite-Kategorien

---

## 📋 Sprint 9: Performance Monitoring (Priorität: NIEDRIG)

### 📊 Metrics
- [ ] **BatchProcessor.cs** - Memory Usage Tracking pro Frame
- [ ] **EsrganRunner.cs** - GPU Load Monitoring
- [ ] **MainWindow.xaml** - FPS Counter für Live Preview

---

## 📋 Sprint 10: Deployment & Distribution (Priorität: MITTEL)

### 📦 Packaging
- [ ] **build.bat** - Signatur für Code Signing Certificate
- [ ] **Installer** - NSIS Installer mit ESRGAN GPU-Option
- [ ] **Portable Version** - Alle Dependencies in self-contained ZIP

---

## 🔄 Fortschritts-Tracking

| Sprint | Status | % Fertig | Nächste Aufgabe |
|--------|--------|----------|-----------------|
| 1 | ⏳ Pending | 0% | Memory Leak Fixes |
| 2 | ⏳ Pending | 0% | GPU Concurrency |
| 3 | ⏳ Pending | 0% | Neue Upscaler |
| 4 | ⏳ Pending | 0% | Fehlerhafte Dateien |
| 5 | ⏳ Pending | 0% | Refactoring |
| 6 | 🔄 In Progress | 10% | SOUL.md Enhancement |
| 7 | ⏳ Pending | 0% | Unit Tests |
| 8 | ⏳ Pending | 0% | AI Enhancements |
| 9 | ⏳ Pending | 0% | Performance Monitoring |
| 10 | ⏳ Pending | 0% | Deployment |

---

## 📝 Anmerkungen

- **Sprint 1** ist KRITISCH - Memory Leaks können Windows Runtime Crashes verursachen
- **SOUL.md Enhancement** (Sprint 6) ist Voraussetzung für optimale Hermes Agent Entwicklung
- Nach jedem Sprint: SOUL.md in `.hermes_cache/` versionieren und im Hauptordner aktualisieren
