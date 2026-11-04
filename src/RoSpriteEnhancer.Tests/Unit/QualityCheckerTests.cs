using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace RoSpriteEnhancer.Core.Tests.Unit;

public class QualityCheckerTests
{
    [Fact]
    public void CheckQuality_IdenticalImages_ReturnsHighScore()
    {
        using var bmp = new Bitmap(50, 50, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Red);

        var report = RoSpriteEnhancer.Core.Processing.QualityChecker.CheckQuality(bmp, bmp, "test.bmp");

        Assert.True(report.OverallScore > 0.9);
        Assert.False(report.IsBlurry);
        Assert.False(report.TransparencyLost);
        Assert.False(report.ColorShift);
    }
}
