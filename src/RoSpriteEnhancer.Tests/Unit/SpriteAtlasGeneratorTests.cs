using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Xunit;

namespace RoSpriteEnhancer.Core.Tests.Unit;

public class SpriteAtlasGeneratorTests
{
    [Fact]
    public void CreateAtlas_EmptyList_ReturnsEmpty()
    {
        var atlases = RoSpriteEnhancer.Core.Processing.SpriteAtlasGenerator.CreateAtlas(
            new List<string>(), Path.GetTempPath(), "test");

        Assert.Empty(atlases);
    }

    [Fact]
    public void CreateAtlas_ValidImages_CreatesMetadata()
    {
        // Erstelle ein temporäres Test-Bild
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_atlas_{Guid.NewGuid():N}.bmp");
        string outDir = Path.Combine(Path.GetTempPath(), $"atlas_out_{Guid.NewGuid():N}");

        try
        {
            using var bmp = new Bitmap(20, 20, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Blue);
            bmp.Save(tempFile, ImageFormat.Bmp);

            var atlases = RoSpriteEnhancer.Core.Processing.SpriteAtlasGenerator.CreateAtlas(
                new List<string> { tempFile }, outDir, "test");

            Assert.Single(atlases);
            Assert.Equal(1, atlases[0].SpriteCount);
            Assert.Single(atlases[0].Sprites);
            Assert.Equal(Path.GetFileName(tempFile), atlases[0].Sprites[0].FileName);

            // Prüfe dass Dateien erstellt wurden
            string atlasPng = Path.Combine(outDir, "test_000.png");
            string atlasJson = Path.Combine(outDir, "test_000.atlas.json");
            Assert.True(File.Exists(atlasPng), $"Atlas-PNG nicht gefunden: {atlasPng}");
            Assert.True(File.Exists(atlasJson), $"Atlas-JSON nicht gefunden: {atlasJson}");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        }
    }

    [Fact]
    public void ExtractSprite_NonExistentFile_ReturnsNull()
    {
        var result = RoSpriteEnhancer.Core.Processing.SpriteAtlasGenerator.ExtractSprite(
            "nonexistent.png", "nonexistent.json", "test.bmp");

        Assert.Null(result);
    }
}
