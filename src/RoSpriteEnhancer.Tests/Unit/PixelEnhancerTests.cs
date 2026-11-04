using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class PixelEnhancerTests
{
    [Fact]
    public void Apply_NullOptions_ReturnsOriginal()
    {
        using var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Red);

        var result = RoSpriteEnhancer.Core.Filtering.PixelEnhancer.Apply(bmp, new RoSpriteEnhancer.Core.Filtering.EnhancerOptions());
        Assert.NotNull(result);
        Assert.Equal(32, result.Width);
        Assert.Equal(32, result.Height);
        result.Dispose();
    }

    [Fact]
    public void Apply_AllEffectsDisabled_ReturnsSameReference()
    {
        using var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Blue);

        var opts = new RoSpriteEnhancer.Core.Filtering.EnhancerOptions
        {
            Outline = false,
            Shadow = false,
            Highlight = false,
            Sharpen = false,
            Volumetric3D = false,
            HueShift = 0f
        };

        var result = RoSpriteEnhancer.Core.Filtering.PixelEnhancer.Apply(bmp, opts);
        // Wenn alle Effekte aus sind, wird das Original zurückgegeben
        Assert.Same(bmp, result);
    }
}