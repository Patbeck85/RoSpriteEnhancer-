using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit
{
    public class AiRunnerTests
    {
        [Fact]
        public void Waifu2xRunner_InvalidPath_ReturnsNullAndSetsLastError()
        {
            using (var src = new Bitmap(16, 16, PixelFormat.Format32bppArgb))
            {
                var opts = new RoSpriteEnhancer.Core.Upscaling.Waifu2xOptions { ExecutablePath = @"C:\invalid_path_waifu2x.exe" };
                var result = RoSpriteEnhancer.Core.Upscaling.Waifu2xRunner.Upscale(src, opts);

                Assert.Null(result);
                Assert.NotEmpty(RoSpriteEnhancer.Core.Upscaling.Waifu2xRunner.LastError);
            }
        }

        [Fact]
        public void RealCuganRunner_InvalidPath_ReturnsNullAndSetsLastError()
        {
            using (var src = new Bitmap(16, 16, PixelFormat.Format32bppArgb))
            {
                var opts = new RoSpriteEnhancer.Core.Upscaling.RealCuganOptions { ExecutablePath = @"C:\invalid_path_realcugan.exe" };
                var result = RoSpriteEnhancer.Core.Upscaling.RealCuganRunner.Upscale(src, opts);

                Assert.Null(result);
                Assert.NotEmpty(RoSpriteEnhancer.Core.Upscaling.RealCuganRunner.LastError);
            }
        }
    }
}