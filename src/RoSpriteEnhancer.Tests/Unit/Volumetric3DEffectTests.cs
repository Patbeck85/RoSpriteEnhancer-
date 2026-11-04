using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class Volumetric3DEffectTests
{
    [Fact]
    public void Apply_PreservesDimensions()
    {
        using var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Red);

        using var result = RoSpriteEnhancer.Core.Filtering.Volumetric3DEffect.Apply(bmp, 0.5f);
        Assert.NotNull(result);
        Assert.Equal(32, result.Width);
        Assert.Equal(32, result.Height);
        result.Dispose();
    }

    [Fact]
    public void Apply_NullBitmap_Throws()
    {
        Assert.Throws<NullReferenceException>(() =>
            RoSpriteEnhancer.Core.Filtering.Volumetric3DEffect.Apply(null!, 0.5f));
    }
}