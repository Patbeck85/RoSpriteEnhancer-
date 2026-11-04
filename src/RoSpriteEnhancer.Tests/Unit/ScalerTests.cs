using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit
{
    public class ScalerTests
    {
        [Fact]
        public void Scale2x_DoublesDimensionsCorrectly()
        {
            using (var src = new Bitmap(16, 16, PixelFormat.Format32bppArgb))
            {
                using (var scaled = RoSpriteEnhancer.Core.Scaling.Scale2x.Scale(src, 1))
                {
                    Assert.Equal(32, scaled.Width);
                    Assert.Equal(32, scaled.Height);
                }
            }
        }

        [Fact]
        public void XbrzScaler_Scale2x_DoublesDimensionsCorrectly()
        {
            using (var src = new Bitmap(16, 16, PixelFormat.Format32bppArgb))
            {
                using (var scaled = RoSpriteEnhancer.Core.Scaling.XbrzScaler.Scale2x(src))
                {
                    Assert.Equal(32, scaled.Width);
                    Assert.Equal(32, scaled.Height);
                }
            }
        }

        [Fact]
        public void BilateralFilter_Apply_PreservesDimensionsAndMicroDetails()
        {
            using (var src = new Bitmap(16, 16, PixelFormat.Format32bppArgb))
            {
                src.SetPixel(5, 5, Color.FromArgb(255, 255, 0, 0));
                src.SetPixel(5, 6, Color.FromArgb(255, 255, 255, 255));

                using (var filtered = RoSpriteEnhancer.Core.Filtering.BilateralFilter.Apply(src, 2.0f, 20.0f, preserveDetails: true))
                {
                    Assert.Equal(16, filtered.Width);
                    Assert.Equal(16, filtered.Height);
                    Assert.Equal(Color.FromArgb(255, 255, 0, 0).ToArgb(), filtered.GetPixel(5, 5).ToArgb());
                }
            }
        }
    }
}