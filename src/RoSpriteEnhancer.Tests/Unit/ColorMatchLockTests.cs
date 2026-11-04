using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class ColorMatchLockTests
{
    [Fact]
    public void LockColors_ReturnsSameDimensions()
    {
        using var src = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(src);
        g.Clear(Color.Red);

        using var result = RoSpriteEnhancer.Core.Utils.ColorMatchLock.LockColors(src, Color.Red, Color.FromArgb(255, 0, 255, 0));
        Assert.NotNull(result);
        Assert.Equal(32, result.Width);
        Assert.Equal(32, result.Height);
        result.Dispose();
    }
}