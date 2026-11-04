using System.IO;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class ActFileTests
{
    [Fact]
    public void TryScaleOffsets_NonExistentInput_ReturnsFalse()
    {
        string input = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.act");
        string output = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid():N}.act");

        bool result = RoSpriteEnhancer.Core.ActFile.TryScaleOffsets(input, output, 2.0f);
        Assert.False(result);
    }
}