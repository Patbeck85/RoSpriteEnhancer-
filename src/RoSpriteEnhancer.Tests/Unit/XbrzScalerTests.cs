using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class XbrzScalerTests
{
    [Fact]
    public void Scale2x_ReturnsDoubledDimensions()
    {
        using var src = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(src);
        g.Clear(Color.Red);

        using var result = RoSpriteEnhancer.Core.Scaling.XbrzScaler.Scale2x(src);
        Assert.NotNull(result);
        Assert.Equal(64, result.Width);
        Assert.Equal(64, result.Height);
        result.Dispose();
    }

    [Fact]
    public void Scale2x_PreservesPixelColors()
    {
        using var src = new Bitmap(2, 2, PixelFormat.Format32bppArgb);
        src.SetPixel(0, 0, Color.Red);
        src.SetPixel(1, 0, Color.Green);
        src.SetPixel(0, 1, Color.Blue);
        src.SetPixel(1, 1, Color.Yellow);

        using var result = RoSpriteEnhancer.Core.Scaling.XbrzScaler.Scale2x(src);
        Assert.NotNull(result);
        Assert.Equal(Color.Red.ToArgb(), result.GetPixel(0, 0).ToArgb());
        result.Dispose();
    }

    [Fact]
    public void Scale4x_ReturnsQuadrupledDimensions()
    {
        using var src = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        using var result = RoSpriteEnhancer.Core.Scaling.XbrzScaler.Scale4x(src);
        Assert.NotNull(result);
        Assert.Equal(64, result.Width);
        Assert.Equal(64, result.Height);
        result.Dispose();
    }
}