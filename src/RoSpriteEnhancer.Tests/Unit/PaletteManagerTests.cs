using System.Drawing;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class PaletteManagerTests
{
    [Fact]
    public void GetAvailablePaletteNames_ReturnsList()
    {
        var names = RoSpriteEnhancer.Core.Utils.PaletteManager.GetAvailablePaletteNames();
        Assert.NotNull(names);
    }

    [Fact]
    public void LoadPalette_NullName_ReturnsNull()
    {
        Assert.Null(RoSpriteEnhancer.Core.Utils.PaletteManager.LoadPalette(null));
    }

    [Fact]
    public void LoadPalette_EmptyName_ReturnsNull()
    {
        Assert.Null(RoSpriteEnhancer.Core.Utils.PaletteManager.LoadPalette(""));
    }

    [Fact]
    public void LoadPalette_NonExistentName_ReturnsNull()
    {
        Assert.Null(RoSpriteEnhancer.Core.Utils.PaletteManager.LoadPalette("NonExistentPalette_xyz123"));
    }

    [Fact]
    public void SavePalette_NullName_ReturnsFalse()
    {
        Assert.False(RoSpriteEnhancer.Core.Utils.PaletteManager.SavePalette(null, new Color[] { Color.Red }));
    }

    [Fact]
    public void SavePalette_EmptyName_ReturnsFalse()
    {
        Assert.False(RoSpriteEnhancer.Core.Utils.PaletteManager.SavePalette("", new Color[] { Color.Red }));
    }

    [Fact]
    public void SavePalette_NullColors_ReturnsFalse()
    {
        Assert.False(RoSpriteEnhancer.Core.Utils.PaletteManager.SavePalette("test", null));
    }

    [Fact]
    public void SavePalette_ValidColors_SavesAndLoads()
    {
        string paletteName = $"rotest_palette_{Guid.NewGuid():N}";
        Color[] colors = { Color.Red, Color.Green, Color.Blue, Color.Yellow };

        try
        {
            Assert.True(RoSpriteEnhancer.Core.Utils.PaletteManager.SavePalette(paletteName, colors));

            var loaded = RoSpriteEnhancer.Core.Utils.PaletteManager.LoadPalette(paletteName);
            Assert.NotNull(loaded);
            Assert.Equal(colors.Length, loaded.Length);

            for (int i = 0; i < colors.Length; i++)
            {
                Assert.Equal(colors[i].ToArgb(), loaded[i].ToArgb());
            }
        }
        finally
        {
            string paletteDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RoSpriteEnhancer", "Palettes");
            string filePath = System.IO.Path.Combine(paletteDir, $"{paletteName}.palette.json");
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }
    }

    [Fact]
    public void SaveAndLoadPalette_RoundTrip_PreservesColors()
    {
        string paletteName = $"rotest_roundtrip_{Guid.NewGuid():N}";
        Color[] colors = {
            Color.FromArgb(255, 0, 0),
            Color.FromArgb(0, 255, 0),
            Color.FromArgb(0, 0, 255),
            Color.FromArgb(128, 128, 128),
            Color.FromArgb(255, 255, 0)
        };

        try
        {
            Assert.True(RoSpriteEnhancer.Core.Utils.PaletteManager.SavePalette(paletteName, colors));

            var names = RoSpriteEnhancer.Core.Utils.PaletteManager.GetAvailablePaletteNames();
            Assert.Contains(names, n => n.StartsWith(paletteName));

            var loaded = RoSpriteEnhancer.Core.Utils.PaletteManager.LoadPalette(paletteName);
            Assert.NotNull(loaded);
            Assert.Equal(colors.Length, loaded.Length);

            for (int i = 0; i < colors.Length; i++)
            {
                Assert.True(colors[i].ToArgb() == loaded[i].ToArgb(), $"Farbe {i} stimmt nicht überein");
            }
        }
        finally
        {
            string paletteDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RoSpriteEnhancer", "Palettes");
            string filePath = System.IO.Path.Combine(paletteDir, $"{paletteName}.palette.json");
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }
    }
}
