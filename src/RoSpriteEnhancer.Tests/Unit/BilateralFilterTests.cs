using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit
{
    public class BilateralFilterTests
    {
        [Fact]
        public void Apply_ReturnsSameSize()
        {
            using var src = new Bitmap(16, 16, PixelFormat.Format32bppArgb);

            using var result = RoSpriteEnhancer.Core.Filtering.BilateralFilter.Apply(src, 2.0f, 20.0f, preserveDetails: true);

            Assert.Equal(src.Width, result.Width);
            Assert.Equal(src.Height, result.Height);
        }

        [Fact]
        public void Apply_UniformImage_ReturnsSimilarImage()
        {
            using var src = new Bitmap(8, 8, PixelFormat.Format32bppArgb);
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    src.SetPixel(x, y, Color.Red);

            using var result = RoSpriteEnhancer.Core.Filtering.BilateralFilter.Apply(src, 2.0f, 20.0f, preserveDetails: true);

            // Einheitliches Bild sollte unverändert bleiben
            Assert.Equal(src.GetPixel(4, 4).ToArgb(), result.GetPixel(4, 4).ToArgb());
        }
    }
}
