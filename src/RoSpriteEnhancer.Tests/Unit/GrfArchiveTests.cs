using System.IO;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit
{
    public class GrfArchiveTests
    {
        [Fact]
        public void Open_WithInvalidFile_ThrowsInvalidDataException()
        {
            string testPath = Path.Combine(Path.GetTempPath(), "invalid_" + System.Guid.NewGuid().ToString("N") + ".grf");
            try
            {
                File.WriteAllBytes(testPath, new byte[16]);
                Assert.Throws<System.IO.InvalidDataException>(() => RoSpriteEnhancer.Core.IO.GrfArchive.Open(testPath));
            }
            finally
            {
                if (File.Exists(testPath)) File.Delete(testPath);
            }
        }
    }
}
