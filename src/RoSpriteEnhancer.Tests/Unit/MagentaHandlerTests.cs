using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit
{
    public class MagentaHandlerTests
    {
        [Fact]
        public void MagentaToAlpha_SetsMagentaToTransparent()
        {
            using var src = new Bitmap(2, 2, PixelFormat.Format32bppArgb);
            src.SetPixel(0, 0, Color.FromArgb(255, 255, 0, 255)); // Magenta #FF00FF
            src.SetPixel(1, 0, Color.FromArgb(255, 255, 0, 0));   // Rot

            using var result = RoSpriteEnhancer.Core.Utils.MagentaHandler.MagentaToAlpha(src);

            Assert.Equal(0, result.GetPixel(0, 0).A); // Magenta -> Transparent
            Assert.Equal(255, result.GetPixel(1, 0).A); // Rot -> Opaque
        }

        [Fact]
        public void AlphaToMagenta_SetsTransparentToMagenta()
        {
            using var src = new Bitmap(2, 2, PixelFormat.Format32bppArgb);
            src.SetPixel(0, 0, Color.FromArgb(0, 255, 0, 0));   // Transparent Rot
            src.SetPixel(1, 0, Color.FromArgb(255, 255, 0, 0)); // Opaque Rot

            using var result = RoSpriteEnhancer.Core.Utils.MagentaHandler.AlphaToMagenta(src);

            Assert.Equal(Color.FromArgb(255, 255, 0, 255), result.GetPixel(0, 0)); // Magenta
            Assert.Equal(Color.FromArgb(255, 255, 0, 0), result.GetPixel(1, 0));    // Rot bleibt Rot
        }
    }
}
