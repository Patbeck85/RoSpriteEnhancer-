using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit
{
    public class BatchProcessorTests
    {
        [Fact]
        public void ProcessBitmapPreview_ReturnsScaledBitmap()
        {
            using var src = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
            var opts = new RoSpriteEnhancer.Core.Processing.ProcessingOptions
            {
                ScaleAlgorithm = RoSpriteEnhancer.Core.Processing.ScaleAlgorithm.Scale2x,
                HandleMagentaKey = false
            };

            using var result = RoSpriteEnhancer.Core.Processing.BatchProcessor.ProcessBitmapPreview(src, opts);

            Assert.Equal(64, result.Width);
            Assert.Equal(64, result.Height);
        }

        [Fact]
        public void ProcessBitmapPreview_WithCategory_ReturnsScaledBitmap()
        {
            using var src = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
            var opts = new RoSpriteEnhancer.Core.Processing.ProcessingOptions
            {
                ScaleAlgorithm = RoSpriteEnhancer.Core.Processing.ScaleAlgorithm.Scale2x,
                HandleMagentaKey = false
            };

            using var result = RoSpriteEnhancer.Core.Processing.BatchProcessor.ProcessBitmapPreview(src, opts, "jobs_m");

            // Kategorie-basierte Skalierung wendet den empfohlenen Algorithmus an (kann 4x sein)
            Assert.True(result.Width >= 64 && result.Height >= 64);
        }

        [Fact]
        public void ResolveEnhancerOptions_ReturnsDefaultWhenNoOverrides()
        {
            var opts = new RoSpriteEnhancer.Core.Processing.ProcessingOptions();
            var result = RoSpriteEnhancer.Core.Processing.BatchProcessor.ResolveEnhancerOptions("data/sprite/npc/test.spr", opts);

            Assert.NotNull(result);
            Assert.Equal(opts.Enhancer.Outline, result.Outline);
        }

        [Fact]
        public void ComputeAverageColorDistance_ReturnsNonNegative()
        {
            using var before = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            using var after = new Bitmap(16, 16, PixelFormat.Format32bppArgb);

            float distance = RoSpriteEnhancer.Core.Processing.BatchProcessor.ComputeAverageColorDistance(before, after);

            Assert.True(distance >= 0);
        }
    }
}
