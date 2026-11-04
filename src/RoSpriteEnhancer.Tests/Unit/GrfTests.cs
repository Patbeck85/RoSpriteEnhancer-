using System;
using System.IO;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit
{
    public class GrfTests
    {
        [Fact]
        public void GrfArchive_Open_InvalidMagic_ThrowsInvalidDataException()
        {
            string testPath = Path.Combine(Path.GetTempPath(), "invalid_" + Guid.NewGuid().ToString("N") + ".grf");
            try
            {
                using (var bw = new BinaryWriter(File.Create(testPath)))
                {
                    bw.Write(new byte[46]);
                }

                Assert.Throws<InvalidDataException>(() => RoSpriteEnhancer.Core.IO.GrfArchive.Open(testPath));
            }
            finally
            {
                if (File.Exists(testPath)) File.Delete(testPath);
            }
        }

        [Fact]
        public void GrfArchive_PackAndOpenFolder_Success()
        {
            string sourceDir = Path.Combine(Path.GetTempPath(), "source_" + Guid.NewGuid().ToString("N"));
            string grfPath   = Path.Combine(Path.GetTempPath(), "out_" + Guid.NewGuid().ToString("N") + ".grf");
            string extractDir = Path.Combine(Path.GetTempPath(), "ext_" + Guid.NewGuid().ToString("N"));

            try
            {
                Directory.CreateDirectory(sourceDir);
                string testFile = Path.Combine(sourceDir, "test_sprite.bmp");
                File.WriteAllText(testFile, "DUMMY_SPRITE_DATA");

                RoSpriteEnhancer.Core.IO.GrfArchive.PackFolder(sourceDir, grfPath);
                Assert.True(File.Exists(grfPath));

                using (var grf = RoSpriteEnhancer.Core.IO.GrfArchive.Open(grfPath))
                {
                    Assert.True(grf.FileCount > 0);
                    int extracted = grf.ExtractSprites(extractDir);
                    Assert.Equal(1, extracted);
                }

                string extractedFile = Path.Combine(extractDir, "test_sprite.bmp");
                Assert.True(File.Exists(extractedFile));
                Assert.Equal("DUMMY_SPRITE_DATA", File.ReadAllText(extractedFile));
            }
            finally
            {
                if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);
                if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                if (File.Exists(grfPath)) File.Delete(grfPath);
            }
        }
    }
}