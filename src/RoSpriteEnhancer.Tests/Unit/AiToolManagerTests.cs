using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class AiToolManagerTests
{
    [Fact]
    public void GetAllTools_ReturnsAllDefinedTools()
    {
        var tools = RoSpriteEnhancer.Core.Utils.AiToolManager.GetAllTools();
        Assert.NotEmpty(tools);
        Assert.Contains(tools, t => t.ToolName == "waifu2x");
        Assert.Contains(tools, t => t.ToolName == "realesrgan");
        Assert.Contains(tools, t => t.ToolName == "realcugan");
        Assert.Contains(tools, t => t.ToolName == "gfpgan");
    }

    [Fact]
    public void GetTool_ReturnsNullForUnknownTool()
    {
        var tool = RoSpriteEnhancer.Core.Utils.AiToolManager.GetTool("nonexistent");
        Assert.Null(tool);
    }

    [Fact]
    public void IsToolInstalled_ReturnsFalseForNonExistentTool()
    {
        var tool = RoSpriteEnhancer.Core.Utils.AiToolManager.GetTool("waifu2x");
        Assert.False(RoSpriteEnhancer.Core.Utils.AiToolManager.IsToolInstalled(tool));
    }
}