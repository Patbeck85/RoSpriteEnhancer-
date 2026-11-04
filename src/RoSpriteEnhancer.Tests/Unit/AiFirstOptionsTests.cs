using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class AiFirstOptionsTests
{
    [Fact]
    public void Default_Values_AreCorrect()
    {
        var opts = new RoSpriteEnhancer.Core.Upscaling.AiFirstOptions();
        Assert.Equal(0.35f, opts.DetailStrength);
        Assert.False(opts.EnableDetailInjection);
        Assert.False(opts.EnableHighResShading);
    }

    [Fact]
    public void Set_Values_AreStored()
    {
        var opts = new RoSpriteEnhancer.Core.Upscaling.AiFirstOptions
        {
            EnableDetailInjection = true,
            DetailStrength = 0.7f,
            EnableHighResShading = true
        };
        Assert.True(opts.EnableDetailInjection);
        Assert.Equal(0.7f, opts.DetailStrength);
        Assert.True(opts.EnableHighResShading);
    }
}