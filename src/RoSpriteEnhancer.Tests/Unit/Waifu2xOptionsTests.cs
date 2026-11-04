using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class Waifu2xOptionsTests
{
    [Fact]
    public void Default_Values_AreCorrect()
    {
        var opts = new RoSpriteEnhancer.Core.Upscaling.Waifu2xOptions();
        Assert.Equal("", opts.ExecutablePath);
        Assert.Equal("models-cunet", opts.ModelPath);
        Assert.Equal(-1, opts.NoiseLevel);
        Assert.Equal(2, opts.ScaleFactor);
        Assert.Equal(60, opts.TimeoutSeconds);
        Assert.False(opts.ForceCpuMode);
    }

    [Fact]
    public void Set_Values_AreStored()
    {
        var opts = new RoSpriteEnhancer.Core.Upscaling.Waifu2xOptions
        {
            ExecutablePath = @"C:\tools\waifu2x.exe",
            ModelPath = "models",
            NoiseLevel = 1,
            ScaleFactor = 4,
            TimeoutSeconds = 30,
            ForceCpuMode = true
        };
        Assert.Equal(@"C:\tools\waifu2x.exe", opts.ExecutablePath);
        Assert.Equal("models", opts.ModelPath);
        Assert.Equal(1, opts.NoiseLevel);
        Assert.Equal(4, opts.ScaleFactor);
        Assert.Equal(30, opts.TimeoutSeconds);
        Assert.True(opts.ForceCpuMode);
    }
}