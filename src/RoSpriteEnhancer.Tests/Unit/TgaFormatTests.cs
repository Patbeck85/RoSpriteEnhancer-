using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class TgaFormatTests
{
    [Fact]
    public void OutputFormat_IncludesTga_Uncompressed()
    {
        // OutputFormat.Tga_Uncompressed muss im Enum existieren
        var fmt = RoSpriteEnhancer.Core.Processing.OutputFormat.Tga_Uncompressed;
        Assert.Equal(3, (int)fmt);
    }

    [Fact]
    public void OutputQuality_DefaultIs9()
    {
        var opts = new RoSpriteEnhancer.Core.Processing.ProcessingOptions();
        Assert.Equal(9, opts.OutputQuality);
    }

    [Fact]
    public void SaveTga_RoundTrip_PreservesPixels()
    {
        // Arrange: 4x4 Test-Bitmap mit bekannten Farben
        using var src = new Bitmap(4, 4, PixelFormat.Format32bppArgb);
        src.SetPixel(0, 0, Color.Red);
        src.SetPixel(1, 0, Color.Green);
        src.SetPixel(2, 0, Color.Blue);
        src.SetPixel(3, 0, Color.Yellow);
        src.SetPixel(0, 1, Color.Magenta);
        src.SetPixel(1, 1, Color.Cyan);
        src.SetPixel(2, 1, Color.White);
        src.SetPixel(3, 1, Color.Black);
        src.SetPixel(0, 2, Color.FromArgb(128, 255, 0, 0)); // semi-transparent red
        src.SetPixel(1, 2, Color.FromArgb(64, 0, 255, 0));  // semi-transparent green
        src.SetPixel(2, 2, Color.FromArgb(32, 0, 0, 255));  // semi-transparent blue
        src.SetPixel(3, 2, Color.FromArgb(255, 128, 128, 128)); // opaque gray
        src.SetPixel(0, 3, Color.Transparent);
        src.SetPixel(1, 3, Color.Transparent);
        src.SetPixel(2, 3, Color.Transparent);
        src.SetPixel(3, 3, Color.Transparent);

        string tempPath = Path.Combine(Path.GetTempPath(), $"rotest_{Guid.NewGuid():N}.tga");

        try
        {
            // Act: TGA speichern und wieder laden
            RoSpriteEnhancer.Core.Processing.BatchProcessor.SaveTga(src, tempPath);

            Assert.True(File.Exists(tempPath));
            Assert.True(new FileInfo(tempPath).Length > 0);

            using var loaded = RoSpriteEnhancer.Core.Processing.BatchProcessor.LoadTga(tempPath);

            // Assert: Pixel-Farben müssen übereinstimmen
            Assert.Equal(src.Width, loaded.Width);
            Assert.Equal(src.Height, loaded.Height);

            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    var expected = src.GetPixel(x, y);
                    var actual = loaded.GetPixel(x, y);
                    Assert.True(expected.ToArgb() == actual.ToArgb(), $"Pixel ({x},{y}): erwartet {expected.ToArgb()}, erhalten {actual.ToArgb()}");
                }
            }
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public void SaveTga_Header_IsValid()
    {
        using var src = new Bitmap(16, 16, PixelFormat.Format32bppArgb);

        string tempPath = Path.Combine(Path.GetTempPath(), $"rotest_header_{Guid.NewGuid():N}.tga");

        try
        {
            RoSpriteEnhancer.Core.Processing.BatchProcessor.SaveTga(src, tempPath);

            using var fs = new FileStream(tempPath, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);

            // TGA Header prüfen (18 Bytes)
            byte idLength = br.ReadByte();        // sollte 0 sein
            byte colorMapType = br.ReadByte();     // sollte 0 sein
            byte imageType = br.ReadByte();        // sollte 2 (uncompressed true-color)
            br.ReadBytes(5);                       // color map spec
            ushort xOrigin = br.ReadUInt16();
            ushort yOrigin = br.ReadUInt16();
            ushort width = br.ReadUInt16();
            ushort height = br.ReadUInt16();
            byte pixelDepth = br.ReadByte();       // sollte 32 sein
            byte descriptor = br.ReadByte();       // sollte 0x20 (top-left)

            Assert.Equal(0, idLength);
            Assert.Equal(0, colorMapType);
            Assert.Equal(2, imageType);
            Assert.Equal(16, width);
            Assert.Equal(16, height);
            Assert.Equal(32, pixelDepth);
            Assert.Equal(0x20, descriptor);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
