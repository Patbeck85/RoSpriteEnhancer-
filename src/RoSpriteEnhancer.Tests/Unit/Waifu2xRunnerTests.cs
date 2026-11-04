using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class Waifu2xRunnerTests
{
    [Fact]
    public void Upscale_NullBitmap_ReturnsNull()
    {
        var result = RoSpriteEnhancer.Core.Upscaling.Waifu2xRunner.Upscale(null!, new RoSpriteEnhancer.Core.Upscaling.Waifu2xOptions());
        Assert.Null(result);
    }

    [Fact]
    public void Upscale_NoExecutable_ReturnsNull()
    {
        using var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        var opts = new RoSpriteEnhancer.Core.Upscaling.Waifu2xOptions
        {
            ExecutablePath = @"C:\nonexistent\waifu2x.exe"
        };
        var result = RoSpriteEnhancer.Core.Upscaling.Waifu2xRunner.Upscale(bmp, opts);
        Assert.Null(result);
    }
}