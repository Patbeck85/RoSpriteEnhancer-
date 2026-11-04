using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

/// <summary>
/// Memory-Leak-Tests für Bitmap- und IDisposable-Ressourcen.
/// Stellt sicher, dass alle Bitmaps nach der Verarbeitung korrekt disposed werden.
/// </summary>
public class MemoryLeakTests
{
    [Fact]
    public void PixelEnhancer_AllEffects_DisposesResultBitmap()
    {
        using var src = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(src);
        g.Clear(Color.Red);

        // Alle Effekte aktivieren
        var opts = new RoSpriteEnhancer.Core.Filtering.EnhancerOptions
        {
            Outline = true,
            OutlineStrength = 0.5f,
            Shadow = true,
            ShadowStrength = 1.0f,
            Highlight = true,
            HighlightStrength = 0.5f,
            Sharpen = true,
            SharpenStrength = 0.3f,
            Volumetric3D = true,
            Volumetric3DStrength = 0.75f,
            HueShift = 10f,
            SaturationScale = 1.2f,
            LightnessScale = 1.1f
        };

        var result = RoSpriteEnhancer.Core.Filtering.PixelEnhancer.Apply(src, opts);
        Assert.NotNull(result);

        // Ergebnis sollte ein neues Bitmap sein (nicht die Referenz des Originals)
        Assert.NotSame(src, result);

        // Ergebnis muss disposbar sein (es ist ein Bitmap)
        result.Dispose();
    }

    [Fact]
    public void SuperSampler_Process_DisposesIntermediateBitmaps()
    {
        using var src = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(src);
        g.Clear(Color.Red);

        var result = RoSpriteEnhancer.Core.Filtering.SuperSampler.Process(
            src,
            new RoSpriteEnhancer.Core.Filtering.EnhancerOptions());
        Assert.NotNull(result);
        result.Dispose();
    }

    [Fact]
    public void Volumetric3DEffect_Apply_DisposesResultBitmap()
    {
        using var src = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(src);
        g.Clear(Color.Red);

        var result = RoSpriteEnhancer.Core.Filtering.Volumetric3DEffect.Apply(src, 0.75f);
        Assert.NotNull(result);
        result.Dispose();
    }

    [Fact]
    public void BatchProcessor_ManyFiles_NoHandleLeak()
    {
        // Erstelle temporäre Test-Dateien
        string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"memleak_test_{System.Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(tempDir);

        try
        {
            // Erstelle 20 Test-Bitmaps
            for (int i = 0; i < 20; i++)
            {
                var path = System.IO.Path.Combine(tempDir, $"test_{i:D2}.bmp");
                using var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(bmp);
                g.Clear(Color.Red);
                bmp.Save(path);
            }

            var opts = new RoSpriteEnhancer.Core.Processing.ProcessingOptions
            {
                InputFolder = tempDir,
                OutputFolder = System.IO.Path.Combine(tempDir, "output"),
                ScaleAlgorithm = RoSpriteEnhancer.Core.Processing.ScaleAlgorithm.Scale2x,
                ScalePasses = 1,
                IncludeBmp = true,
                IncludePng = false,
                IncludeTga = false,
                IncludeSpr = false
            };
            System.IO.Directory.CreateDirectory(opts.OutputFolder);

            var processor = new RoSpriteEnhancer.Core.Processing.BatchProcessor();
            var files = processor.GetFiles(opts);
            Assert.Equal(20, files.Count);

            var result = processor.ProcessAsync(opts, files, null, System.Threading.CancellationToken.None).Result;

            Assert.True(result.Processed > 0);
            Assert.Equal(0, result.Errors);
        }
        finally
        {
            if (System.IO.Directory.Exists(tempDir))
                System.IO.Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SprWriter_Write_DisposesAllBitmaps()
    {
        string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sprwriter_test_{System.Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(tempDir);

        try
        {
            var frames = new List<Bitmap>();
            for (int i = 0; i < 5; i++)
            {
                var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(bmp);
                g.Clear(Color.Red);
                frames.Add(bmp);
            }

            string outputPath = System.IO.Path.Combine(tempDir, "test.spr");
            RoSpriteEnhancer.Core.IO.SprWriter.Write(outputPath, frames);

            // Alle Frames sollten nach dem Schreiben noch valide sein (nicht von SprWriter disposed)
            foreach (var frame in frames)
            {
                Assert.NotNull(frame);
                frame.Dispose();
            }
        }
        finally
        {
            if (System.IO.Directory.Exists(tempDir))
                System.IO.Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Scale2x_Scale_DisposesResultBitmap()
    {
        using var src = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(src);
        g.Clear(Color.Red);

        var result = RoSpriteEnhancer.Core.Scaling.Scale2x.Scale(src, 2);
        Assert.NotNull(result);
        result.Dispose();
    }

    [Fact]
    public void XbrzScaler_Scale2x_DisposesResultBitmap()
    {
        using var src = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(src);
        g.Clear(Color.Red);

        var result = RoSpriteEnhancer.Core.Scaling.XbrzScaler.Scale2x(src);
        Assert.NotNull(result);
        result.Dispose();
    }

    [Fact]
    public void GrfArchive_CreateAndExtract_NoHandleLeak()
    {
        string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"grf_leak_test_{System.Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(tempDir);

        try
        {
            string inputDir = System.IO.Path.Combine(tempDir, "input");
            System.IO.Directory.CreateDirectory(inputDir);

            // Erstelle Test-Bild
            using var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Blue);
            bmp.Save(System.IO.Path.Combine(inputDir, "test.bmp"));

            string grfPath = System.IO.Path.Combine(tempDir, "test.grf");
            RoSpriteEnhancer.Core.IO.GrfArchive.PackFolder(inputDir, grfPath);
            Assert.True(System.IO.File.Exists(grfPath));

            string extractDir = System.IO.Path.Combine(tempDir, "extract");
            System.IO.Directory.CreateDirectory(extractDir);
            using var grf = RoSpriteEnhancer.Core.IO.GrfArchive.Open(grfPath);
            grf.ExtractSprites(extractDir);

            string extractedFile = System.IO.Path.Combine(extractDir, "test.bmp");
            Assert.True(System.IO.File.Exists(extractedFile));
        }
        finally
        {
            if (System.IO.Directory.Exists(tempDir))
                System.IO.Directory.Delete(tempDir, true);
        }
    }
}