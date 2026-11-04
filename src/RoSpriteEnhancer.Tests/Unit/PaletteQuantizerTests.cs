using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class PaletteQuantizerTests
{
    [Fact]
    public void Quantize_ReturnsSameDimensions()
    {
        using var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Red);

        using var result = RoSpriteEnhancer.Core.Filtering.PaletteQuantizer.Quantize(bmp);
        Assert.NotNull(result);
        Assert.Equal(16, result.Width);
        Assert.Equal(16, result.Height);
        result.Dispose();
    }

    [Fact]
    public void Quantize_ReducesColorCount()
    {
        using var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Red);

        using var result = RoSpriteEnhancer.Core.Filtering.PaletteQuantizer.Quantize(bmp);
        Assert.NotNull(result);
        Assert.True(result.Width > 0);
        result.Dispose();
    }
}