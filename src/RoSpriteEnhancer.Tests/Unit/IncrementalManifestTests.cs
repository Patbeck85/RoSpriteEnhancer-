using System.IO;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class IncrementalManifestTests
{
    [Fact]
    public void NewManifest_HasEmptyEntries()
    {
        var manifest = new RoSpriteEnhancer.Core.Processing.IncrementalManifest();
        Assert.NotNull(manifest.Entries);
        Assert.Empty(manifest.Entries);
    }

    [Fact]
    public void SaveAndLoad_PreservesEntries()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"manifest_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var manifest = new RoSpriteEnhancer.Core.Processing.IncrementalManifest();
            manifest.Entries["test.spr"] = new RoSpriteEnhancer.Core.Processing.IncrementalManifest.ManifestEntry
            {
                ContentHash = "abc123",
                SettingsHash = "def456"
            };

            manifest.Save(tempDir);

            var loaded = RoSpriteEnhancer.Core.Processing.IncrementalManifest.Load(tempDir);
            Assert.True(loaded.Entries.ContainsKey("test.spr"));
            Assert.Equal("abc123", loaded.Entries["test.spr"].ContentHash);
            Assert.Equal("def456", loaded.Entries["test.spr"].SettingsHash);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_NonExistentFolder_ReturnsEmptyManifest()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"manifest_nonexistent_{Guid.NewGuid():N}");
        var manifest = RoSpriteEnhancer.Core.Processing.IncrementalManifest.Load(tempDir);
        Assert.NotNull(manifest);
        Assert.Empty(manifest.Entries);
    }
}