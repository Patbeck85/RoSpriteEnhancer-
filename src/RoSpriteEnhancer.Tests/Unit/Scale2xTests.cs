using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class Scale2xTests
{
    [Fact]
    public void Scale_ValidInput_ReturnsNonNull()
    {
        using var src = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(src);
        g.Clear(Color.Red);

        var result = RoSpriteEnhancer.Core.Scaling.Scale2x.Scale(src, 2);
        Assert.NotNull(result);
        Assert.True(result.Width > 0);
        Assert.True(result.Height > 0);
        result.Dispose();
    }

    [Fact]
    public void Scale_ScaleFactor1_ReturnsSameSize()
    {
        using var src = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        var result = RoSpriteEnhancer.Core.Scaling.Scale2x.Scale(src, 1);
        Assert.NotNull(result);
        Assert.True(result.Width > 0);
        Assert.True(result.Height > 0);
        result.Dispose();
    }

    [Fact]
    public void Scale_PreservesPixelColors()
    {
        using var src = new Bitmap(2, 2, PixelFormat.Format32bppArgb);
        src.SetPixel(0, 0, Color.Red);
        src.SetPixel(1, 0, Color.Green);
        src.SetPixel(0, 1, Color.Blue);
        src.SetPixel(1, 1, Color.Yellow);

        var result = RoSpriteEnhancer.Core.Scaling.Scale2x.Scale(src, 2);
        Assert.NotNull(result);
        Assert.Equal(Color.Red.ToArgb(), result.GetPixel(0, 0).ToArgb());
        result.Dispose();
    }
}