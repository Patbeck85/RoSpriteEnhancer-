using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Core.Tests.Unit;

public class SpriteContentClassifierTests
{
    [Fact]
    public void Classify_NullBitmap_ReturnsOther()
    {
        var result = RoSpriteEnhancer.Core.Processing.SpriteContentClassifier.Classify(null!);

        Assert.Equal("other", result.PredictedCategoryId);
        Assert.Equal("Sonstige", result.PredictedCategoryName);
        Assert.Equal(0f, result.Confidence);
    }

    [Fact]
    public void Classify_RedImage_DetectsRedDominance()
    {
        using var bmp = new Bitmap(20, 20, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 20; y++)
            for (int x = 0; x < 20; x++)
                bmp.SetPixel(x, y, Color.Red);

        var result = RoSpriteEnhancer.Core.Processing.SpriteContentClassifier.Classify(bmp);

        Assert.NotNull(result.PredictedCategoryId);
        Assert.NotEmpty(result.DetectedFeatures);
    }

    [Fact]
    public void Classify_BlueImage_DetectsBlueDominance()
    {
        using var bmp = new Bitmap(20, 20, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 20; y++)
            for (int x = 0; x < 20; x++)
                bmp.SetPixel(x, y, Color.Blue);

        var result = RoSpriteEnhancer.Core.Processing.SpriteContentClassifier.Classify(bmp);

        Assert.NotNull(result.PredictedCategoryId);
        Assert.NotEmpty(result.DetectedFeatures);
    }

    [Fact]
    public void Classify_GreenImage_DetectsGreenDominance()
    {
        using var bmp = new Bitmap(20, 20, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 20; y++)
            for (int x = 0; x < 20; x++)
                bmp.SetPixel(x, y, Color.Green);

        var result = RoSpriteEnhancer.Core.Processing.SpriteContentClassifier.Classify(bmp);

        Assert.NotNull(result.PredictedCategoryId);
        Assert.NotEmpty(result.DetectedFeatures);
    }
}
