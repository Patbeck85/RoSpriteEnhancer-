using System.IO;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class PresetManagerTests
{
    [Fact]
    public void Save_ValidPreset_CreatesFile()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"preset_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var preset = new RoSpriteEnhancer.Core.Services.EnhancementPreset
            {
                Name = "Test Preset",
                ScaleAlgorithm = RoSpriteEnhancer.Core.Processing.ScaleAlgorithm.Scale2x,
                ScalePasses = 2
            };

            RoSpriteEnhancer.Core.Services.PresetManager.Save(preset);

            string presetPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RoSpriteEnhancer", "Presets", "Test Preset.json");
            Assert.True(File.Exists(presetPath));

            // Aufräumen
            if (File.Exists(presetPath)) File.Delete(presetPath);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_NotExisting_ReturnsNull()
    {
        var result = RoSpriteEnhancer.Core.Services.PresetManager.Load("NonExistentPreset_" + Guid.NewGuid());
        Assert.Null(result);
    }
}