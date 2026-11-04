using System.IO;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class SprReaderTests
{
    [Fact]
    public void Load_NonExistentFile_ThrowsFileNotFound()
    {
        string nonExistent = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.spr");
        Assert.Throws<FileNotFoundException>(() =>
            RoSpriteEnhancer.Core.IO.SprFile.Load(nonExistent));
    }

    [Fact]
    public void Load_InvalidMagic_ThrowsInvalidData()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"invalid_{Guid.NewGuid():N}.spr");
        try
        {
            File.WriteAllBytes(tempPath, new byte[] { 0x00, 0x00, 0x01, 0x02 });
            Assert.Throws<InvalidDataException>(() =>
                RoSpriteEnhancer.Core.IO.SprFile.Load(tempPath));
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void SprFrame_Disposable_ImplementsIDisposable()
    {
        Assert.True(typeof(RoSpriteEnhancer.Core.IO.SprFrame).IsAssignableTo(typeof(System.IDisposable)));
    }

    [Fact]
    public void SprFile_Disposable_ImplementsIDisposable()
    {
        Assert.True(typeof(RoSpriteEnhancer.Core.IO.SprFile).IsAssignableTo(typeof(System.IDisposable)));
    }
}