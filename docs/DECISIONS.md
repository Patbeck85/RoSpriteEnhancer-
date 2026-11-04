# Architecture Decision Record (ADR) — RoSpriteEnhancer

**Project:** RoSpriteEnhancer  
**Domain:** Game Sprite Batch Processing & AI Upscaling Engine  
**Governance Framework:** SOUL.md v2.0 Enterprise  

---

## ADR-001: Clean Architecture mit Dependency Inversion

**Status:** Accepted  
**Date:** 2026-08-03  
**Author:** Architect Agent (`ROLE-001`)

### Context
RoSpriteEnhancer ist eine WPF-Anwendung zur Batch-Verarbeitung von Ragnarok Online Sprites (.spr, .act, .pal). Die Anwendung muss:
- Hohe Performance bei 1000+ Sprites
- Thread-sichere UI-Aktualisierung
- Saubere Trennung zwischen Core-Logik und UI
- Einfache Testbarkeit

### Decision
Wir verwenden **Clean Architecture** mit folgenden Schichten:

```
RoSpriteEnhancer/
├── Core/              # Business Logic (unabhängig von UI/Framework)
│   ├── SpriteFilter.cs
│   ├── SPRParser.cs
│   ├── ACTParser.cs
│   └── PaletteHandler.cs
├── UI/                # WPF Presentation Layer
│   ├── MainWindow.xaml
│   ├── SpriteViewer.xaml
│   └── FilterControls.xaml
└── Infrastructure/    # External Dependencies (optional)
    ├── ImageLoader.cs
    └── FileExporter.cs
```

**WICHTIG:** `Core/` darf **KEINE** Abhängigkeit zu `UI/` haben.  
`Core/` ist framework-agnostisch und kann theoretisch in einer Console-Anwendung laufen.

### Consequences
- Einfache Unit-Tests für Core-Logik ohne UI-Setup
- UI-Änderungen beeinflussen nicht die Business Logic
- Einfache Migration zu anderen Frameworks möglich

---

## ADR-002: Memory Management & Zero-Leak Policy

**Status:** Accepted  
**Date:** 2026-08-03  
**Author:** QA Agent (`ROLE-004`)

### Context
Bei der Verarbeitung von 1000+ Sprites mit jeweils hunderten Frames kann es zu Memory-Leaks kommen, wenn:
- Image-Objekte nicht explizit freigegeben werden
- Stream/Buffer-Handles offen bleiben
- GC nicht zwischen Chunks aktiviert wird

### Decision
Wir implementieren striktes **MEMORY-001**:

```csharp
using (var image = ImageLoader.Load(path))
{
    // Verarbeitung
} // Auto-Freigabe beim Verlassen des using-Blocks

// Nach jedem Chunk von 100 Dateien:
GC.Collect();
```

### Consequences
- Keine Runtime-Crashes durch Memory Exhaustion
- Stabiles Arbeiten mit großen Sprite-Sets
- Einfache Debugging-Möglichkeiten bei Speicherproblemen

---

## ADR-003: Thread-Safe UI Dispatcher Pattern

**Status:** Accepted  
**Date:** 2026-08-03  
**Author:** Frontend Agent (`ROLE-002`)

### Context
Die WPF UI muss immer auf dem UI-Thread aktualisiert werden. Hintergrundthreads dürfen nicht direkt auf UI-Elemente zugreifen.

### Decision
Wir verwenden den **Dispatcher.InvokePattern**:

```csharp
// Hintergrundthread:
private void ProcessImageAsync()
{
    var result = HeavyProcessing();
    
    // UI-Thread:
    Dispatcher.Invoke(() =>
    {
        statusText.Text = $"Fertig: {result}";
        progress.Value = 100;
    });
}
```

### Consequences
- Keine Cross-ThreadExceptions in WPF
- Responsive UI auch bei langen Processing-Zyklen
- Thread-sichere Progress-Bars und Status-Updates

---

## ADR-004: Sprite Category Matching mit Path-Segment Validation

**Status:** Accepted  
**Date:** 2026-08-03  
**Author:** Backend Agent (`ROLE-003`)

### Context
Bei der Extraktion von Sprites aus .grf Dateien muss zwischen verschiedenen Kategorien unterschieden werden (npc, item, monster, etc.). Ein einfacher `Contains()` über den gesamten Pfad führt zu falschen Zuordnungen.

### Decision
Wir verwenden **Path-Segment Validation**:

```csharp
private static bool MatchesCategory(string path, SpriteCategory cat)
{
    string[] segments = path.Split('\\', '/');
    
    foreach (string fragment in cat.PathFragments)
    {
        foreach (string segment in segments)
        {
            if (segment.Length == 0) continue;
            
            // GENAUER Vergleich: Ordner muss EXACT "npc" heißen
            if (segment.Equals(fragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
    }
    
    return false;
}
```

### Consequences
- Keine falschen Kategorie-Zuordnungen
- Vermeidung von gemischten Sprites (NPC-Sprites im Monster-Ordner)
- Zuverlässige Extraktion mit GRF Editor

---

## ADR-005: Transparency & Palette Protection für Indizierte Sprites

**Status:** Accepted  
**Date:** 2026-08-03  
**Author:** Backend Agent (`ROLE-003`)

### Context
Ragnarok Online verwendet indizierte Farben (Color Index). Index 0 ist Magenta (#FF00FF) oder 0% Alpha Transparency. Bei Upscaling darf dieser Index nicht verfälscht werden.

### Decision
Wir implementieren **SPRITE-001**:

```csharp
// Palette-Schutz:
public class PaletteHandler
{
    private byte[] palette = new byte[256 * 4]; // RGBA
    
    public void PreserveIndexZero(byte[] sourcePalette)
    {
        // Index 0 (Magenta/Transparent) NICHT interpolieren
        palette[0] = sourcePalette[0];   // R
        palette[1] = sourcePalette[1];   // G
        palette[2] = sourcePalette[2];   // B
        palette[3] = sourcePalette[3];   // A
    }
}
```

### Consequences
- Keine verfälschten Transparenz-Kanäle
- Korrekte Darstellung von Indizes 0-255
- Kompatibilität mit Classic RO Client

---

## ADR-006: Batch Processing mit Chunking & Memory Isolation

**Status:** Accepted  
**Date:** 2026-08-03  
**Author:** Backend Agent (`ROLE-003`)

### Context
Große Sprite-Sets (1000+ Dateien) müssen in Chunks verarbeitet werden, um Memory-Leaks zu vermeiden.

### Decision
Wir implementieren **Batch Chunking**:

```csharp
public async Task ProcessBatchAsync(IEnumerable<string> files)
{
    const int CHUNK_SIZE = 100; // Max 100 Dateien pro Chunk
    
    var fileGroups = files.Batch(CHUNK_SIZE);
    
    foreach (var chunk in fileGroups)
    {
        await ProcessChunkAsync(chunk);
        
        // Memory Cleanup nach jedem Chunk
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
```

### Consequences
- Stabiles Arbeiten mit großen Sprite-Sets
- Keine Memory Exhaustion bei 1000+ Dateien
- Einfache Fehlerisolierung pro Chunk

---

## ADR-007: Lanczos x4 als Standard-Upscaling Algorithmus

**Status:** Accepted  
**Date:** 2026-08-03  
**Author:** Backend Agent (`ROLE-003`)

### Context
Der User bevorzugt Lanczos x4 (wie Waifu2x Noise 3 Level 3) für beste Ergebnisse: entfernt Rauschen, Aliasing und Farbflackern ohne Details zu verlieren.

### Decision
Wir setzen **Lanczos x4** als Standard:

```csharp
public enum UpscaleAlgorithm
{
    Lanczos4,   // STANDARD - Beste Qualität
    Nearest,    // Pixel-Art (keine Interpolation)
    Bilinear,   // Weiche Skalierung
    xBRZ        // Edge-Aware (für Pixel-Art)
}

public static Image Upscale(Image input, int scale, UpscaleAlgorithm algorithm)
{
    switch (algorithm)
    {
        case UpscaleAlgorithm.Lanczos4:
            return LanczosFilter.Scale(input, scale);
        case UpscaleAlgorithm.Nearest:
            return NearestNeighbor.Scale(input, scale);
        // ...
    }
}
```

### Consequences
- Beste visuelle Qualität für HD-Texturen
- Kompatibel mit User-Erwartungen (Waifu2x Noise 3 Level 3)
- Optionale Pixel-Art-Modi verfügbar

---

## ADR-008: ACT Offset Scaling Formula

**Status:** Accepted  
**Date:** 2026-08-03  
**Author:** Backend Agent (`ROLE-003`)

### Context
Bei der Skalierung von .act Dateien müssen die Offset-Werte proportional skaliert werden, NICHT die Scale/Rotation Werte.

### Decision
Wir implementieren **CORRECT Scaling Formula**:

```csharp
public static void ScaleACTOffset(string actPath, float scaleX, float scaleY)
{
    var header = ReadACTHeader(actPath);
    
    // Offset skaliert proportional zur neuen Breite
    float newOffsetX = header.OffsetX * (scaleX / header.OriginalWidth);
    float newOffsetY = header.OffsetY * (scaleY / header.OriginalHeight);
    
    // ScaleX/ScaleY NICHT skalieren (sind layer-relative zum SPR-Texture)
    // Rotation bleibt unverändert
    
    WriteACTHeader(actPath, newOffsetX, newOffsetY);
}
```

### Consequences
- Korrekte Positionierung nach Skalierung
- Keine verzerrten Sprites
- Kompatibel mit RO Client Asset Loading

---

## ADR-009: File Naming Immunity für RO Sprites

**Status:** Accepted  
**Date:** 2026-08-03  
**Author:** Backend Agent (`ROLE-003`)

### Context
RO Sprite Studio verwendet EUC-KR/ANSI Encoding für Dateinamen. UTF-8 Konvertierung macht Dateien unreadable im Client.

### Decision
Wir **NEVER** RO Sprite Dateinamen ändern:

```csharp
// ORIGINAL NAME (EUC-KR/ANSI) - NIEMALS in UTF-8 konvertieren!
string originalName = "sprite/人間/hair_001.spr"; // EUC-KR
string processedPath = Path.Combine(outputDir, originalName);

// Encoding='euc-kr' erzwingen bei allen Datei-Operationen
using (var reader = new StreamReader(path, Encoding.GetEncoding("euc-kr")))
```

### Consequences
- Kompatibilität mit RO Client Asset Loading
- Keine GATE 4 Hard Blocks durch falsche Dateinamen
- Original-Encoding erhalten

---

## ADR-010: Release Quality Gate Checklist

**Status:** Accepted  
**Date:** 2026-08-03  
**Author:** DevOps Agent (`ROLE-005`)

### Context
Vor jedem Release in `release_final/` muss eine strikte Qualitätssicherung durchgeführt werden.

### Decision
Wir implementieren **RELEASE-001 bis 004**:

```csharp
public static bool ReleaseReady()
{
    // [ ] Build ist 100% reproduzierbar
    var buildResult = RunBuild();
    if (!buildResult.Success) return false;
    
    // [ ] QA-Freigabe liegt schriftlich vor
    var qaApproval = ReadQAApproval();
    if (qaApproval.Status != "APPROVED") return false;
    
    // [ ] Tests stehen auf PASS (0 Fehlschläge)
    var testResult = RunTests();
    if (testResult.FailedCount > 0) return false;
    
    // [ ] Version in Manifest & CHANGELOG.md aktualisiert
    UpdateVersionManifest();
    
    // [ ] Workspace (.workspace_temp/) vollständig geleert
    ClearWorkspace();
    
    // [ ] Keine Debug-Flags oder Entwickler-Keys im Paket
    var package = CreateReleasePackage();
    if (package.HasDebugFlags) return false;
    
    return true;
}
```

### Consequences
- Nur geprüfte Endprodukte in `release_final/`
- Reproduzierbare Builds
- Saubere Releases ohne Debug-Artefakte

---

## NEXT ADRs

Die folgenden Architektur-Entscheidungen werden in den nächsten Sessions dokumentiert:

- **ADR-011:** SPR Binary Parser Implementation Details
- **ADR-012:** Multi-Threading Architecture for Batch Processing
- **ADR-013:** WPF UI Component Design System
- **ADR-014:** Performance Benchmarking Strategy
- **ADR-015:** Error Handling & Logging Framework

---

**END OF DECISIONS.md**
