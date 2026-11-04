using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit
{
    public class QuantizerAndMagentaTests
    {
        [Fact]
        public void MagentaHandler_ToAlphaAndBack_PreservesTransparency()
        {
            using (var src = new Bitmap(8, 8, PixelFormat.Format32bppArgb))
            {
                src.SetPixel(2, 2, Color.FromArgb(255, 255, 0, 255));

                using (var alphaBmp = RoSpriteEnhancer.Core.Utils.MagentaHandler.MagentaToAlpha(src))
                {
                    Assert.Equal(0, alphaBmp.GetPixel(2, 2).A);

                    using (var magentaBmp = RoSpriteEnhancer.Core.Utils.MagentaHandler.AlphaToMagenta(alphaBmp))
                    {
                        Color p = magentaBmp.GetPixel(2, 2);
                        Assert.Equal(255, p.R);
                        Assert.Equal(0, p.G);
                        Assert.Equal(255, p.B);
                    }
                }
            }
        }

        [Fact]
        public void PaletteQuantizer_Quantize_Returns8BitIndexedBitmap()
        {
            using (var src = new Bitmap(16, 16, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(src)) g.Clear(Color.Blue);

                using (var indexed = RoSpriteEnhancer.Core.Filtering.PaletteQuantizer.Quantize(src))
                {
                    Assert.Equal(PixelFormat.Format8bppIndexed, indexed.PixelFormat);
                    Assert.Equal(16, indexed.Width);
                    Assert.Equal(16, indexed.Height);
                }
            }
        }
    }
}