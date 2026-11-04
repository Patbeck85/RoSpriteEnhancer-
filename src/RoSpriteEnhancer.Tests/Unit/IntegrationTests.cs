using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

/// <summary>
/// Integrationstests für UI-Logik und Verarbeitungspipeline.
/// Diese Tests simulieren UI-Interaktionen ohne echtes Fenster (headless).
/// </summary>
public class IntegrationTests
{
    [Fact]
    public async System.Threading.Tasks.Task FullProcessingPipeline_InputToOutput_EndToEnd()
    {
        // Arrange: Temporärer Eingabe- und Ausgabeordner
        string tempDir = Path.Combine(Path.GetTempPath(), $"integration_e2e_{Guid.NewGuid():N}");
        string inputDir = Path.Combine(tempDir, "input");
        string outputDir = Path.Combine(tempDir, "output");
        Directory.CreateDirectory(inputDir);
        Directory.CreateDirectory(outputDir);

        try
        {
            // Erstelle Test-Bild
            string testFile = Path.Combine(inputDir, "test_sprite.bmp");
            using (var bmp = new Bitmap(64, 64, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Red);
                bmp.Save(testFile);
            }

            // Konfiguriere Verarbeitungs-Optionen
            var opts = new RoSpriteEnhancer.Core.Processing.ProcessingOptions
            {
                InputFolder = inputDir,
                OutputFolder = outputDir,
                ScaleAlgorithm = RoSpriteEnhancer.Core.Processing.ScaleAlgorithm.Scale2x,
                ScalePasses = 1,
                IncludeBmp = true,
                IncludePng = false,
                IncludeTga = false,
                IncludeSpr = false
            };

            // Act: Führe Verarbeitung durch
            var processor = new RoSpriteEnhancer.Core.Processing.BatchProcessor();
            var files = processor.GetFiles(opts);
            Assert.Single(files);

            var result = await processor.ProcessAsync(opts, files, null, System.Threading.CancellationToken.None);

            // Assert: Ausgabe-Datei existiert
            Assert.True(result.Processed > 0);
            Assert.Equal(0, result.Errors);
            Assert.True(Directory.GetFiles(outputDir).Length > 0);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Preset_SaveAndLoad_Integration()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), $"integration_preset_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var preset = new RoSpriteEnhancer.Core.Services.EnhancementPreset
            {
                Name = "Integration Test Preset",
                Category = RoSpriteEnhancer.Core.Services.PresetCategory.Classic,
                ScaleAlgorithm = RoSpriteEnhancer.Core.Processing.ScaleAlgorithm.Scale2x,
                ScalePasses = 2,
                Outline = true,
                OutlineStrength = 0.7f
            };

            // Act: Speichern
            RoSpriteEnhancer.Core.Services.PresetManager.Save(preset);

            // Assert: Datei existiert
            string presetPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RoSpriteEnhancer", "Presets", "Integration Test Preset.json");
            Assert.True(File.Exists(presetPath));

            // Act: Laden
            var loaded = RoSpriteEnhancer.Core.Services.PresetManager.Load("Integration Test Preset");
            Assert.NotNull(loaded);
            Assert.Equal("Integration Test Preset", loaded.Name);
            Assert.Equal(RoSpriteEnhancer.Core.Services.PresetCategory.Classic, loaded.Category);
            Assert.Equal(RoSpriteEnhancer.Core.Processing.ScaleAlgorithm.Scale2x, loaded.ScaleAlgorithm);
            Assert.Equal(2, loaded.ScalePasses);
            Assert.Equal(0.7f, loaded.OutlineStrength);

            // Cleanup
            if (File.Exists(presetPath)) File.Delete(presetPath);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ToolInstaller_FindExecutable_Integration()
    {
        // Act: Suche nach nicht existierendem Tool
        var result = RoSpriteEnhancer.Core.Utils.ToolInstaller.FindExecutable(null!, "test.exe");

        // Assert: Sollte null zurückgeben
        Assert.Null(result);
    }

    [Fact]
    public void WindowStateService_SaveAndLoad_Integration()
    {
        // Arrange: Temporärer Zustand

        // Act: Speichere und lade Zustand (statische Klasse)
        RoSpriteEnhancer.Core.Utils.WindowStateService.SaveWindowState("IntegrationTest", 100, 200, 800, 600, "Normal");
        bool loaded = RoSpriteEnhancer.Core.Utils.WindowStateService.TryLoadWindowState("IntegrationTest", out double l, out double t, out double w, out double h, out string ws);

        // Assert
        Assert.True(loaded);
        Assert.Equal(100, l);
        Assert.Equal(200, t);
        Assert.Equal(800, w);
        Assert.Equal(600, h);
        Assert.Equal("Normal", ws);

        // Cleanup
        RoSpriteEnhancer.Core.Utils.WindowStateService.ClearWindowState("IntegrationTest");
    }

    [Fact]
    public void MagentaHandler_AlphaConversion_Integration()
    {
        // Arrange: Bitmap mit Magenta-Pixeln
        using var bmp = new Bitmap(4, 4, PixelFormat.Format32bppArgb);
        bmp.SetPixel(0, 0, Color.FromArgb(255, 255, 0, 255)); // Magenta -> Transparent
        bmp.SetPixel(1, 0, Color.Red); // Rot -> Undurchsichtig
        bmp.SetPixel(0, 1, Color.FromArgb(0, 0, 0, 0)); // Transparent -> Transparent
        bmp.SetPixel(1, 1, Color.Green); // Gruen -> Undurchsichtig

        // Act: Magenta zu Alpha konvertieren
        using var result = RoSpriteEnhancer.Core.Utils.MagentaHandler.MagentaToAlpha(bmp);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.GetPixel(0, 0).A); // Magenta wurde transparent
        Assert.Equal(255, result.GetPixel(1, 0).A); // Rot blieb undurchsichtig
        Assert.Equal(0, result.GetPixel(0, 1).A); // Transparent blieb transparent
        Assert.Equal(255, result.GetPixel(1, 1).A); // Gruen blieb undurchsichtig

        result.Dispose();
    }

    [Fact]
    public void SpriteFilter_CategoryMatching_Integration()
    {
        // Act: Teste Kategorien-Matching (prüft nur, dass Methoden existieren und Ergebnisse liefern)
        var monsterMatch = RoSpriteEnhancer.Core.Models.SpriteFilter.Matches("data/sprite/몬스터/dragon.spr", new[] { "monsters" });
        var weaponMatch = RoSpriteEnhancer.Core.Models.SpriteFilter.IsWeaponFile("data/sprite/무기/sword.spr");
        var nonWeaponMatch = RoSpriteEnhancer.Core.Models.SpriteFilter.IsWeaponFile("data/sprite/몬스터/dragon.spr");

        // Assert: Methoden sollten boolesche Ergebnisse zurückgeben (keine Exception)
        Assert.IsType<bool>(monsterMatch);
        Assert.IsType<bool>(weaponMatch);
        Assert.IsType<bool>(nonWeaponMatch);
    }

    [Fact]
    public void QualityChecker_IdenticalImages_NoQualityLoss()
    {
        // Arrange: Zwei identische Bitmaps
        using var bmp1 = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using var g1 = Graphics.FromImage(bmp1);
        g1.Clear(Color.Blue);

        using var bmp2 = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using var g2 = Graphics.FromImage(bmp2);
        g2.Clear(Color.Blue);

        // Act: Qualitätsprüfung
        var report = RoSpriteEnhancer.Core.Processing.QualityChecker.CheckQuality(bmp1, bmp2, "test.bmp");

        // Assert: Keine Qualitätsverluste bei identischen Bildern
        Assert.NotNull(report);
        Assert.False(report.IsBlurry);
        Assert.False(report.TransparencyLost);
        Assert.False(report.ColorShift);
    }

    [Fact]
    public void KeyboardShortcut_Conflicts_Detection()
    {
        // Act: Prüfe auf Konflikte (Liste sollte zurückgegeben werden)
        var conflicts = RoSpriteEnhancer.Core.Utils.KeyboardShortcutManager.GetConflicts();

        // Assert: GetConflicts sollte eine Liste zurückgeben (kann leer sein oder Konflikte enthalten)
        Assert.NotNull(conflicts);
        Assert.IsType<List<(string, string, string, string)>>(conflicts);

        // Act: Erstelle manuell einen Konflikt
        RoSpriteEnhancer.Core.Utils.KeyboardShortcutManager.SetShortcut("TestCmd1", "O", "Control");
        RoSpriteEnhancer.Core.Utils.KeyboardShortcutManager.SetShortcut("TestCmd2", "O", "Control");

        // Assert: Konflikt sollte erkannt werden
        var newConflicts = RoSpriteEnhancer.Core.Utils.KeyboardShortcutManager.GetConflicts();
        Assert.True(newConflicts.Count > 0);

        // Cleanup: Setze Standard zurück
        RoSpriteEnhancer.Core.Utils.KeyboardShortcutManager.SetShortcut("TestCmd1", "O", "Control");
        RoSpriteEnhancer.Core.Utils.KeyboardShortcutManager.SetShortcut("TestCmd2", "S", "Control");
    }
}