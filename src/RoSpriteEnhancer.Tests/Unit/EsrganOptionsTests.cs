using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class EsrganOptionsTests
{
    [Fact]
    public void Default_Values_AreCorrect()
    {
        var opts = new RoSpriteEnhancer.Core.Upscaling.EsrganOptions();
        Assert.Equal("", opts.ExecutablePath);
        Assert.Equal("realesrgan-x4plus-anime", opts.ModelName);
        Assert.Equal("esrgan", opts.ModelPath);
        Assert.Equal(4, opts.ScaleFactor);
        Assert.Equal(0, opts.TileSize);
        Assert.Equal(4, opts.TilePad);
        Assert.Equal(120, opts.TimeoutSeconds);
        Assert.True(opts.UseGpu);
    }

    [Fact]
    public void Set_Values_AreStored()
    {
        var opts = new RoSpriteEnhancer.Core.Upscaling.EsrganOptions
        {
            ExecutablePath = @"C:\tools\realesrgan.exe",
            ModelName = "my-model",
            ModelPath = "models",
            ScaleFactor = 2,
            TileSize = 512,
            TilePad = 8,
            TimeoutSeconds = 60,
            UseGpu = false
        };
        Assert.Equal(@"C:\tools\realesrgan.exe", opts.ExecutablePath);
        Assert.Equal("my-model", opts.ModelName);
        Assert.Equal("models", opts.ModelPath);
        Assert.Equal(2, opts.ScaleFactor);
        Assert.Equal(512, opts.TileSize);
        Assert.Equal(8, opts.TilePad);
        Assert.Equal(60, opts.TimeoutSeconds);
        Assert.False(opts.UseGpu);
    }
}