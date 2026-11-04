using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Core.Tests.Unit;

public class MagentaHandlerExtendedTests
{
    [Fact]
    public void MagentaToAlpha_ConvertsMagentaToTransparent()
    {
        using var bmp = new Bitmap(2, 1, PixelFormat.Format32bppArgb);
        bmp.SetPixel(0, 0, Color.FromArgb(255, 255, 0, 255)); // Magenta
        bmp.SetPixel(1, 0, Color.Red);

        var result = RoSpriteEnhancer.Core.Utils.MagentaHandler.MagentaToAlpha(bmp);

        try
        {
            var alpha0 = result.GetPixel(0, 0).A;
            var alpha1 = result.GetPixel(1, 0).A;

            Assert.Equal(0, alpha0); // Magenta -> transparent
            Assert.Equal(255, alpha1); // Red -> undurchsichtig
        }
        finally
        {
            result.Dispose();
        }
    }

    [Fact]
    public void ContainsMagenta_DetectsMagentaPixel()
    {
        using var bmp = new Bitmap(2, 1, PixelFormat.Format32bppArgb);
        bmp.SetPixel(0, 0, Color.Red);
        bmp.SetPixel(1, 0, Color.FromArgb(255, 255, 0, 255)); // Magenta

        Assert.True(RoSpriteEnhancer.Core.Utils.MagentaHandler.ContainsMagenta(bmp));
    }

    [Fact]
    public void ContainsMagenta_NoMagenta_ReturnsFalse()
    {
        using var bmp = new Bitmap(2, 1, PixelFormat.Format32bppArgb);
        bmp.SetPixel(0, 0, Color.Red);
        bmp.SetPixel(1, 0, Color.Blue);

        Assert.False(RoSpriteEnhancer.Core.Utils.MagentaHandler.ContainsMagenta(bmp));
    }
}
