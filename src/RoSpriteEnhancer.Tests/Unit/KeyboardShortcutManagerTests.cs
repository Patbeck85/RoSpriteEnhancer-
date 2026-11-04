using System.IO;
using Xunit;

namespace RoSpriteEnhancer.Core.Tests.Unit;

public class KeyboardShortcutManagerTests
{
    [Fact]
    public void GetShortcut_UnknownCommand_ReturnsEmpty()
    {
        var sc = RoSpriteEnhancer.Core.Utils.KeyboardShortcutManager.GetShortcut("NonExistent");

        Assert.Equal("NonExistent", sc.CommandName);
        Assert.Equal("", sc.Key);
        Assert.Equal("None", sc.Modifiers);
    }

    [Fact]
    public void GetShortcut_KnownCommand_ReturnsDefault()
    {
        var sc = RoSpriteEnhancer.Core.Utils.KeyboardShortcutManager.GetShortcut("OpenInput");

        Assert.Equal("OpenInput", sc.CommandName);
        Assert.Equal("O", sc.Key);
        Assert.Equal("Control", sc.Modifiers);
        Assert.Contains("Strg", sc.DisplayText);
    }

    [Fact]
    public void SetAndGetShortcut_RoundTrip()
    {
        RoSpriteEnhancer.Core.Utils.KeyboardShortcutManager.SetShortcut("TestCmd", "F5", "Control+Shift");

        var sc = RoSpriteEnhancer.Core.Utils.KeyboardShortcutManager.GetShortcut("TestCmd");

        Assert.Equal("F5", sc.Key);
        Assert.Equal("Control+Shift", sc.Modifiers);
    }

    [Fact]
    public void GetAllShortcuts_ReturnsNonEmpty()
    {
        var all = RoSpriteEnhancer.Core.Utils.KeyboardShortcutManager.GetAllShortcuts();

        Assert.NotEmpty(all);
    }
}
