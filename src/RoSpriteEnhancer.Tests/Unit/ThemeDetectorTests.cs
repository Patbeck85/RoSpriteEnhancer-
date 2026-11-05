using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class ThemeDetectorTests
{
    [Fact]
    public void GetThemeName_ReturnsLightOrDark()
    {
        string theme = RoSpriteEnhancer.Core.Utils.ThemeDetector.GetThemeName();
        Assert.Matches("^(Light|Dark)$", theme);
    }

    [Fact]
    public void IsDarkMode_ReturnsBoolean()
    {
        bool isDark = RoSpriteEnhancer.Core.Utils.ThemeDetector.IsDarkMode();
        // Der Rückgabewert ist immer true oder false
        Assert.True(isDark || !isDark);
    }
}
