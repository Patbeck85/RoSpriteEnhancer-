using System.IO;
using System.Text;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class PathHelperTests
{
    [Fact]
    public void IsValidPath_Null_ReturnsFalse()
    {
        Assert.False(RoSpriteEnhancer.Core.Utils.PathHelper.IsValidPath(null));
    }

    [Fact]
    public void IsValidPath_Empty_ReturnsFalse()
    {
        Assert.False(RoSpriteEnhancer.Core.Utils.PathHelper.IsValidPath(""));
    }

    [Fact]
    public void IsValidPath_Whitespace_ReturnsFalse()
    {
        Assert.False(RoSpriteEnhancer.Core.Utils.PathHelper.IsValidPath("   "));
    }

    [Fact]
    public void IsValidPath_ValidPath_ReturnsTrue()
    {
        Assert.True(RoSpriteEnhancer.Core.Utils.PathHelper.IsValidPath(@"C:\Temp\test.txt"));
    }


    [Fact]
    public void FileExists_Null_ReturnsFalse()
    {
        Assert.False(RoSpriteEnhancer.Core.Utils.PathHelper.FileExists(null));
    }

    [Fact]
    public void FileExists_NonExistentFile_ReturnsFalse()
    {
        Assert.False(RoSpriteEnhancer.Core.Utils.PathHelper.FileExists(@"C:\nonexistent_file_xyz123.txt"));
    }

    [Fact]
    public void FileExists_TempFile_ReturnsTrue()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"rotest_{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllText(tempFile, "test");
            Assert.True(RoSpriteEnhancer.Core.Utils.PathHelper.FileExists(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void DirectoryExists_Null_ReturnsFalse()
    {
        Assert.False(RoSpriteEnhancer.Core.Utils.PathHelper.DirectoryExists(null));
    }

    [Fact]
    public void DirectoryExists_NonExistentDirectory_ReturnsFalse()
    {
        Assert.False(RoSpriteEnhancer.Core.Utils.PathHelper.DirectoryExists(@"C:\nonexistent_dir_xyz123"));
    }

    [Fact]
    public void DirectoryExists_TempDirectory_ReturnsTrue()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"rotest_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            Assert.True(RoSpriteEnhancer.Core.Utils.PathHelper.DirectoryExists(tempDir));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void EnsureDirectory_InvalidPath_ReturnsFalse()
    {
        Assert.False(RoSpriteEnhancer.Core.Utils.PathHelper.EnsureDirectory(null));
        Assert.False(RoSpriteEnhancer.Core.Utils.PathHelper.EnsureDirectory(""));
    }

    [Fact]
    public void EnsureDirectory_NewDirectory_ReturnsTrueAndCreates()
    {
        string newDir = Path.Combine(Path.GetTempPath(), $"rotest_ensure_{Guid.NewGuid():N}");
        try
        {
            Assert.False(Directory.Exists(newDir));
            Assert.True(RoSpriteEnhancer.Core.Utils.PathHelper.EnsureDirectory(newDir));
            Assert.True(Directory.Exists(newDir));
        }
        finally
        {
            if (Directory.Exists(newDir)) Directory.Delete(newDir, true);
        }
    }

    [Fact]
    public void Combine_SinglePath_ReturnsSame()
    {
        string result = RoSpriteEnhancer.Core.Utils.PathHelper.Combine("folder");
        Assert.Equal("folder", result);
    }

    [Fact]
    public void Combine_MultiplePaths_ReturnsCombined()
    {
        string result = RoSpriteEnhancer.Core.Utils.PathHelper.Combine("folder", "subfolder", "file.txt");
        Assert.EndsWith(Path.Combine("folder", "subfolder", "file.txt"), result);
    }

    [Fact]
    public void Combine_NullAndEmptyPaths_AreIgnored()
    {
        string result = RoSpriteEnhancer.Core.Utils.PathHelper.Combine("folder", null, "", "file.txt");
        Assert.EndsWith(Path.Combine("folder", "file.txt"), result);
    }

    [Fact]
    public void SanitizeFileName_Null_ReturnsEmpty()
    {
        Assert.Equal("", RoSpriteEnhancer.Core.Utils.PathHelper.SanitizeFileName(null));
    }

    [Fact]
    public void SanitizeFileName_Empty_ReturnsEmpty()
    {
        Assert.Equal("", RoSpriteEnhancer.Core.Utils.PathHelper.SanitizeFileName(""));
    }

    [Fact]
    public void SanitizeFileName_InvalidChars_AreReplaced()
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            string input = $"test{c}file";
            string result = RoSpriteEnhancer.Core.Utils.PathHelper.SanitizeFileName(input);
            Assert.DoesNotContain(c, result);
        }
    }

    [Fact]
    public void SanitizeFileName_DotDot_AreReplaced()
    {
        string result = RoSpriteEnhancer.Core.Utils.PathHelper.SanitizeFileName("..\\..\\etc\\passwd");
        Assert.DoesNotContain("..", result);
        Assert.DoesNotContain("/", result);
        Assert.DoesNotContain("\\", result);
    }

    [Fact]
    public void CreateTempDirectory_CreatesDirectory()
    {
        string tempDir = RoSpriteEnhancer.Core.Utils.PathHelper.CreateTempDirectory("testprefix");
        try
        {
            Assert.True(Directory.Exists(tempDir));
            Assert.StartsWith("testprefix_", Path.GetFileName(tempDir));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SafeDeleteDirectory_Null_DoesNotThrow()
    {
        // Sollte keine Ausnahme werfen
        RoSpriteEnhancer.Core.Utils.PathHelper.SafeDeleteDirectory(null);
    }

    [Fact]
    public void SafeDeleteDirectory_NonExistent_DoesNotThrow()
    {
        RoSpriteEnhancer.Core.Utils.PathHelper.SafeDeleteDirectory(@"C:\nonexistent_dir_xyz123");
    }

    [Fact]
    public void SafeDeleteFile_Null_DoesNotThrow()
    {
        RoSpriteEnhancer.Core.Utils.PathHelper.SafeDeleteFile(null);
    }

    [Fact]
    public void SafeDeleteFile_NonExistent_DoesNotThrow()
    {
        RoSpriteEnhancer.Core.Utils.PathHelper.SafeDeleteFile(@"C:\nonexistent_file_xyz123.txt");
    }

    [Fact]
    public void SafeDeleteFile_ExistingFile_Deletes()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"rotest_safedelete_{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllText(tempFile, "test");
            Assert.True(File.Exists(tempFile));
            RoSpriteEnhancer.Core.Utils.PathHelper.SafeDeleteFile(tempFile);
            Assert.False(File.Exists(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
