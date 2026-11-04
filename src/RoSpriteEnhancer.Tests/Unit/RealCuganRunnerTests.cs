using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class RealCuganRunnerTests
{
    [Fact]
    public void Upscale_NullBitmap_ReturnsNull()
    {
        var result = RoSpriteEnhancer.Core.Upscaling.RealCuganRunner.Upscale(null!, new RoSpriteEnhancer.Core.Upscaling.RealCuganOptions());
        Assert.Null(result);
    }

    [Fact]
    public void Upscale_NoExecutable_ReturnsNull()
    {
        using var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        var opts = new RoSpriteEnhancer.Core.Upscaling.RealCuganOptions
        {
            ExecutablePath = @"C:\nonexistent\realcugan.exe"
        };
        var result = RoSpriteEnhancer.Core.Upscaling.RealCuganRunner.Upscale(bmp, opts);
        Assert.Null(result);
    }
}