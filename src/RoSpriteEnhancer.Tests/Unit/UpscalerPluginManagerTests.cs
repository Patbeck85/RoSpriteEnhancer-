using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class UpscalerPluginManagerTests
{
    [Fact]
    public void Plugins_Default_IsEmpty()
    {
        Assert.Empty(RoSpriteEnhancer.Core.Processing.UpscalerPluginManager.Plugins);
    }
}