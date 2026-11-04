using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Core.Tests.Unit;

/// <summary>
/// Performance-Benchmarks für die Upscaling-Pipeline.
/// Diese Tests messen die Verarbeitungszeit verschiedener Skalierungs-Algorithmen
/// und liefern Referenzwerte für Performance-Regressionstests.
/// </summary>
public class UpscalePerformanceTests
{
    private static readonly Random Random = new(42);

    private static Bitmap CreateTestBitmap(int width, int height)
    {
        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.CornflowerBlue);
        for (int i = 0; i < width * height / 100; i++)
        {
            g.FillEllipse(Brushes.Red, Random.Next(width), Random.Next(height), 20, 20);
        }
        return bmp;
    }

    [Fact]
    public void Benchmark_Scale2x_2x()
    {
        using var bmp = CreateTestBitmap(256, 256);
        var sw = Stopwatch.StartNew();
        var result = BatchProcessor.ProcessBitmapPreview(bmp, new ProcessingOptions
        {
            ScaleAlgorithm = ScaleAlgorithm.Scale2x,
            ScalePasses = 1
        }, null, null);
        sw.Stop();

        Console.WriteLine($"Scale2x 2x (256x256): {sw.ElapsedMilliseconds}ms, Result: {result.Width}x{result.Height}");
        result.Dispose();
        bmp.Dispose();

        // Performance sollte unter 500ms für 256x256 liegen
        Assert.True(sw.ElapsedMilliseconds < 500, $"Scale2x 2x zu langsam: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Benchmark_Xbrz_2x()
    {
        using var bmp = CreateTestBitmap(256, 256);
        var sw = Stopwatch.StartNew();
        var result = BatchProcessor.ProcessBitmapPreview(bmp, new ProcessingOptions
        {
            ScaleAlgorithm = ScaleAlgorithm.XbrzX2,
            ScalePasses = 1
        }, null, null);
        sw.Stop();

        Console.WriteLine($"Xbrz 2x (256x256): {sw.ElapsedMilliseconds}ms, Result: {result.Width}x{result.Height}");
        result.Dispose();
        bmp.Dispose();

        Assert.True(sw.ElapsedMilliseconds < 1000, $"Xbrz 2x zu langsam: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Benchmark_Batch_100Sprites()
    {
        var files = new List<string>();
        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"rosprite_bench_{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(tempDir);

        try
        {
            for (int i = 0; i < 100; i++)
            {
                var path = System.IO.Path.Combine(tempDir, $"sprite_{i:D3}.bmp");
                using var bmp = CreateTestBitmap(64, 64);
                bmp.Save(path);
                files.Add(path);
            }

            var sw = Stopwatch.StartNew();
            var opts = new ProcessingOptions
            {
                InputFolder = tempDir,
                OutputFolder = System.IO.Path.Combine(tempDir, "output"),
                ScaleAlgorithm = ScaleAlgorithm.Scale2x,
                ScalePasses = 1
            };
            System.IO.Directory.CreateDirectory(opts.OutputFolder);

            var processor = new BatchProcessor();
            var result = processor.ProcessAsync(opts, files, null, System.Threading.CancellationToken.None).Result;
            sw.Stop();

            Console.WriteLine($"Batch 100x64x64 Sprites Scale2x: {sw.ElapsedMilliseconds}ms, Processed: {result.Processed}");
            Assert.True(result.Processed == 100, $"Nicht alle Sprites verarbeitet: {result.Processed}/100");
        }
        finally
        {
            if (System.IO.Directory.Exists(tempDir))
                System.IO.Directory.Delete(tempDir, true);
        }
    }
}