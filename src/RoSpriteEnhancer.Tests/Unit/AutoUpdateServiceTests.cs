using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class AutoUpdateServiceTests
{
    [Fact]
    public void CurrentVersion_HasDefaultValue()
    {
        Assert.Equal("1.21.0", RoSpriteEnhancer.Core.Services.AutoUpdateService.CurrentVersion);
    }

    [Fact]
    public void GitHubRepo_HasDefaultValue()
    {
        Assert.Equal("YggdrasilStudio/RoSpriteEnhancer", RoSpriteEnhancer.Core.Services.AutoUpdateService.GitHubRepo);
    }

    [Fact]
    public void CheckForUpdateAsync_ReturnsResult()
    {
        var result = RoSpriteEnhancer.Core.Services.AutoUpdateService.CheckForUpdateAsync().Result;
        Assert.NotNull(result);
        Assert.Equal("1.21.0", result.CurrentVersion);
    }
}