using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit
{
    public class DetailExtractorTests
    {
        [Fact]
        public void ExtractDetailMap_ReturnsSameSize()
        {
            using var src = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            using var result = RoSpriteEnhancer.Core.Processing.DetailExtractor.ExtractDetailMap(src);

            Assert.Equal(src.Width, result.Width);
            Assert.Equal(src.Height, result.Height);
        }

        [Fact]
        public void ExtractDetailMap_HighContrastEdge_DetectsDetail()
        {
            using var src = new Bitmap(4, 4, PixelFormat.Format32bppArgb);
            // Starke Kontrastkante: linke Hälfte schwarz, rechte Hälfte weiß
            for (int y = 0; y < 4; y++)
            {
                src.SetPixel(0, y, Color.Black);
                src.SetPixel(1, y, Color.Black);
                src.SetPixel(2, y, Color.White);
                src.SetPixel(3, y, Color.White);
            }

            using var result = RoSpriteEnhancer.Core.Processing.DetailExtractor.ExtractDetailMap(src);

            // Mindestens ein Pixel im verarbeiteten Bereich sollte nicht neutral (128) sein
            bool foundNonNeutral = false;
            for (int y = 1; y < 3; y++)
            {
                for (int x = 1; x < 3; x++)
                {
                    if (result.GetPixel(x, y).R != 128)
                    {
                        foundNonNeutral = true;
                        break;
                    }
                }
                if (foundNonNeutral) break;
            }

            Assert.True(foundNonNeutral, "Detail-Karte sollte hochfrequente Kanten erkennen");
        }

        [Fact]
        public void InjectDetail_ReturnsSameSize()
        {
            using var src = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            using var detail = new Bitmap(16, 16, PixelFormat.Format32bppArgb);

            using var result = RoSpriteEnhancer.Core.Processing.DetailExtractor.InjectDetail(src, detail, 0.5f);

            Assert.Equal(src.Width, result.Width);
            Assert.Equal(src.Height, result.Height);
        }

        [Fact]
        public void InjectDetail_StrengthZero_ReturnsOriginal()
        {
            using var src = new Bitmap(8, 8, PixelFormat.Format32bppArgb);
            src.SetPixel(4, 4, Color.Red);
            using var detail = new Bitmap(8, 8, PixelFormat.Format32bppArgb);

            using var result = RoSpriteEnhancer.Core.Processing.DetailExtractor.InjectDetail(src, detail, 0.0f);

            Assert.Equal(src.GetPixel(4, 4).ToArgb(), result.GetPixel(4, 4).ToArgb());
        }
    }
}
