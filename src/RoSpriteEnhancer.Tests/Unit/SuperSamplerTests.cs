using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class SuperSamplerTests
{
    [Fact]
    public void Process_NullEnhancerOpts_Throws()
    {
        using var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        Assert.Throws<NullReferenceException>(() =>
            RoSpriteEnhancer.Core.Filtering.SuperSampler.Process(bmp, null!));
    }

    [Fact]
    public void Process_ValidInput_ReturnsBitmap()
    {
        using var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Red);

        using var result = RoSpriteEnhancer.Core.Filtering.SuperSampler.Process(
            bmp,
            new RoSpriteEnhancer.Core.Filtering.EnhancerOptions());
        Assert.NotNull(result);
        result.Dispose();
    }
}