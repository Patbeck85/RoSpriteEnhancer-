using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class ToolInstallerTests
{
    [Fact]
    public void FindExecutable_NullSubfolder_ReturnsNull()
    {
        var result = RoSpriteEnhancer.Core.Utils.ToolInstaller.FindExecutable(null!, "test.exe");
        Assert.Null(result);
    }

    [Fact]
    public void FindExecutable_NullExeName_ReturnsNull()
    {
        var result = RoSpriteEnhancer.Core.Utils.ToolInstaller.FindExecutable("tools", null!);
        Assert.Null(result);
    }
}