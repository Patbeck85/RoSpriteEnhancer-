using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class AlphaBleedCleanerTests
{
    [Fact]
    public void CleanAlpha_DoesNotThrowOnNull()
    {
        // Should not throw on null
        var result = RoSpriteEnhancer.Core.Utils.AlphaBleedCleaner.CleanAlpha(null);
        Assert.Null(result);
    }

    [Fact]
    public void CleanAlpha_PreservesDimensions()
    {
        using var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(255, 100, 150, 200));

        using var cleaned = RoSpriteEnhancer.Core.Utils.AlphaBleedCleaner.CleanAlpha(bmp);
        Assert.NotNull(cleaned);
        Assert.Equal(16, cleaned.Width);
        Assert.Equal(16, cleaned.Height);
    }
}
