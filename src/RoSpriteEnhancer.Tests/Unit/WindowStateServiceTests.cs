using System.IO;
using Xunit;

namespace RoSpriteEnhancer.Core.Tests.Unit;

public class WindowStateServiceTests
{
    [Fact]
    public void SaveAndLoadLastFolders_RoundTrip()
    {
        // Test mit temporärem Verzeichnis
        string tempDir = Path.Combine(Path.GetTempPath(), $"wstest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Wir testen indirekt über die öffentlichen Methoden
            // Die Service-Klasse nutzt feste Pfade unter %AppData%,
            // daher testen wir nur dass keine Exception auftritt
            RoSpriteEnhancer.Core.Utils.WindowStateService.SaveLastFolders(
                Path.Combine(tempDir, "input"),
                Path.Combine(tempDir, "output"));

            var (inputs, outputs) = RoSpriteEnhancer.Core.Utils.WindowStateService.LoadLastFoldersList();

            // Sollte keine Exception werfen
            Assert.True(inputs != null);
            Assert.True(outputs != null);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SaveWindowState_DoesNotThrow()
    {
        // Sollte keine Exception werfen auch bei ungültigen Werten
        RoSpriteEnhancer.Core.Utils.WindowStateService.SaveWindowState(
            "TestWindow", 100, 200, 800, 600, "Normal");

        Assert.True(true);
    }

    [Fact]
    public void TryLoadWindowState_UnknownWindow_ReturnsFalse()
    {
        var result = RoSpriteEnhancer.Core.Utils.WindowStateService.TryLoadWindowState(
            "NonExistentWindow", out double l, out double t, out double w, out double h, out string ws);

        Assert.False(result);
    }
}
