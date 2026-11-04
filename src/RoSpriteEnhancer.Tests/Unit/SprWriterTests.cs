using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class SprWriterTests
{
    [Fact]
    public void Write_NullFrames_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            RoSpriteEnhancer.Core.IO.SprWriter.Write(Path.GetTempPath() + "\\test.spr", null!));
    }

    [Fact]
    public void Write_EmptyFrames_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            RoSpriteEnhancer.Core.IO.SprWriter.Write(Path.GetTempPath() + "\\test.spr", new List<Bitmap>()));
    }
}