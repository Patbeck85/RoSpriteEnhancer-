using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class ServiceContainerTests
{
    [Fact]
    public void Instance_IsNotNull()
    {
        Assert.NotNull(RoSpriteEnhancer.Core.Utils.ServiceContainer.Instance);
    }

    [Fact]
    public void GetService_EsrganOptions_ReturnsInstance()
    {
        var options = RoSpriteEnhancer.Core.Utils.ServiceContainer.GetService<RoSpriteEnhancer.Core.Upscaling.EsrganOptions>();
        Assert.NotNull(options);
    }

    [Fact]
    public void GetService_Waifu2xOptions_ReturnsInstance()
    {
        var options = RoSpriteEnhancer.Core.Utils.ServiceContainer.GetService<RoSpriteEnhancer.Core.Upscaling.Waifu2xOptions>();
        Assert.NotNull(options);
    }

    [Fact]
    public void GetService_RealCuganOptions_ReturnsInstance()
    {
        var options = RoSpriteEnhancer.Core.Utils.ServiceContainer.GetService<RoSpriteEnhancer.Core.Upscaling.RealCuganOptions>();
        Assert.NotNull(options);
    }

    [Fact]
    public void GetService_AiFirstOptions_ReturnsInstance()
    {
        var options = RoSpriteEnhancer.Core.Utils.ServiceContainer.GetService<RoSpriteEnhancer.Core.Upscaling.AiFirstOptions>();
        Assert.NotNull(options);
    }

    [Fact]
    public void GetService_IncrementalManifest_ReturnsInstance()
    {
        var manifest = RoSpriteEnhancer.Core.Utils.ServiceContainer.GetService<RoSpriteEnhancer.Core.Processing.IncrementalManifest>();
        Assert.NotNull(manifest);
    }

    [Fact]
    public void GetService_SameType_ReturnsSameInstance()
    {
        var options1 = RoSpriteEnhancer.Core.Utils.ServiceContainer.GetService<RoSpriteEnhancer.Core.Upscaling.EsrganOptions>();
        var options2 = RoSpriteEnhancer.Core.Utils.ServiceContainer.GetService<RoSpriteEnhancer.Core.Upscaling.EsrganOptions>();
        Assert.Same(options1, options2);
    }

    [Fact]
    public void GetService_DifferentTypes_ReturnsDifferentInstances()
    {
        var esrgan = RoSpriteEnhancer.Core.Utils.ServiceContainer.GetService<RoSpriteEnhancer.Core.Upscaling.EsrganOptions>();
        var waifu2x = RoSpriteEnhancer.Core.Utils.ServiceContainer.GetService<RoSpriteEnhancer.Core.Upscaling.Waifu2xOptions>();
        Assert.NotSame(esrgan, waifu2x);
    }
}
