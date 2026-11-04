using Xunit;

namespace RoSpriteEnhancer.Tests.Unit
{
    public class SpriteResolutionProfileTests
    {
        [Fact]
        public void Get_ReturnsProfileForKnownCategory()
        {
            var profile = RoSpriteEnhancer.Core.Processing.SpriteResolutionProfiles.Get("jobs_m");

            Assert.NotNull(profile);
            Assert.Equal("jobs_m", profile.CategoryId);
            Assert.Equal(256, profile.TargetWidth);
            Assert.Equal(256, profile.TargetHeight);
            Assert.True(profile.IsEnabled);
        }

        [Fact]
        public void Get_ReturnsNullForUnknownCategory()
        {
            var profile = RoSpriteEnhancer.Core.Processing.SpriteResolutionProfiles.Get("nonexistent");

            Assert.Null(profile);
        }

        [Fact]
        public void GetAll_ReturnsAllCategories()
        {
            var all = RoSpriteEnhancer.Core.Processing.SpriteResolutionProfiles.GetAll();

            Assert.True(all.Count > 0);
            Assert.Contains(all, c => c.CategoryId == "items");
            Assert.Contains(all, c => c.CategoryId == "monsters");
            Assert.Contains(all, c => c.CategoryId == "npc");
            Assert.Contains(all, c => c.CategoryId == "hair_m");
            Assert.Contains(all, c => c.CategoryId == "hair_f");
        }

        [Fact]
        public void CalculateScaleFactor_16x16To256x256_Returns16()
        {
            var profile = new RoSpriteEnhancer.Core.Processing.SpriteResolutionProfile
            {
                CategoryId = "test",
                TargetWidth = 256,
                TargetHeight = 256
            };

            int scale = profile.CalculateScaleFactor(16, 16);

            Assert.Equal(16, scale);
        }

        [Fact]
        public void CalculateTargetSize_32x32_Scale4_Returns128x128()
        {
            var profile = new RoSpriteEnhancer.Core.Processing.SpriteResolutionProfile
            {
                CategoryId = "test",
                TargetWidth = 256,
                TargetHeight = 256
            };

            var size = profile.CalculateTargetSize(32, 32, 4);

            Assert.Equal(128, size.Width);
            Assert.Equal(128, size.Height);
        }
    }
}
