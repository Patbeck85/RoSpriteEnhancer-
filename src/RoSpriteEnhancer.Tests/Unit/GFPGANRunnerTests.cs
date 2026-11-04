using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit
{
    public class GFPGANRunnerTests
    {
        [Fact]
        public void Restore_WithMissingExecutable_ReturnsNullAndSetsError()
        {
            using var src = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            var opts = new RoSpriteEnhancer.Core.Upscaling.GFGANOptions
            {
                ExecutablePath = @"C:\nonexistent\gfpgan-ncnn-vulkan.exe",
                Upscale = 2.0f
            };

            var result = RoSpriteEnhancer.Core.Upscaling.GFPGANRunner.Restore(src, opts);

            Assert.Null(result);
            Assert.NotEmpty(RoSpriteEnhancer.Core.Upscaling.GFPGANRunner.LastError);
            Assert.Contains("nicht gefunden", RoSpriteEnhancer.Core.Upscaling.GFPGANRunner.LastError);
        }

        [Fact]
        public void Restore_WithNullBitmap_ReturnsNullAndSetsError()
        {
            var opts = new RoSpriteEnhancer.Core.Upscaling.GFGANOptions
            {
                ExecutablePath = @"C:\nonexistent\gfpgan-ncnn-vulkan.exe",
                Upscale = 2.0f
            };

            var result = RoSpriteEnhancer.Core.Upscaling.GFPGANRunner.Restore(null!, opts);

            Assert.Null(result);
            Assert.NotEmpty(RoSpriteEnhancer.Core.Upscaling.GFPGANRunner.LastError);
        }
    }
}
