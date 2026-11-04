using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit
{
    public class RealESRGANRunnerTests
    {
        [Fact]
        public void Upscale_WithMissingExecutable_ReturnsNullAndSetsError()
        {
            using var src = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            var opts = new RoSpriteEnhancer.Core.Upscaling.RealESRGANOptions
            {
                ExecutablePath = @"C:\nonexistent\realesrgan-ncnn-vulkan.exe",
                ScaleFactor = 2
            };

            var result = RoSpriteEnhancer.Core.Upscaling.RealESRGANRunner.Upscale(src, opts);

            Assert.Null(result);
            Assert.NotEmpty(RoSpriteEnhancer.Core.Upscaling.RealESRGANRunner.LastError);
            Assert.Contains("nicht gefunden", RoSpriteEnhancer.Core.Upscaling.RealESRGANRunner.LastError);
        }

        [Fact]
        public void Upscale_WithNullBitmap_ReturnsNullAndSetsError()
        {
            var opts = new RoSpriteEnhancer.Core.Upscaling.RealESRGANOptions
            {
                ExecutablePath = @"C:\nonexistent\realesrgan-ncnn-vulkan.exe",
                ScaleFactor = 2
            };

            var result = RoSpriteEnhancer.Core.Upscaling.RealESRGANRunner.Upscale(null!, opts);

            Assert.Null(result);
            Assert.NotEmpty(RoSpriteEnhancer.Core.Upscaling.RealESRGANRunner.LastError);
        }
    }
}
