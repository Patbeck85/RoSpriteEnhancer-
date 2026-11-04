using System.IO;
using System.Linq;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit
{
    public class ManifestAndFilterTests
    {
        [Fact]
        public void IncrementalManifest_ComputeContentHash_GeneratesMd5()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "RO_SPRITE_TEST_CONTENT");
                string hash = RoSpriteEnhancer.Core.Processing.IncrementalManifest.ComputeContentHash(tempFile);

                Assert.NotNull(hash);
                Assert.Equal(32, hash.Length);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void SpriteFilter_MatchesCategory_DetectsCategoriesCorrectly()
        {
            string monsterPath = @"data\sprite\몬스터\poring.spr";
            string npcPath     = @"data\sprite\npc\kafra.spr";

            var monsterCat = RoSpriteEnhancer.Core.Models.SpriteFilter.AllCategories.First(c => c.Id == "monsters");
            var npcCat     = RoSpriteEnhancer.Core.Models.SpriteFilter.AllCategories.First(c => c.Id == "npc");

            Assert.True(RoSpriteEnhancer.Core.Models.SpriteFilter.MatchesCategory(monsterPath, monsterCat));
            Assert.True(RoSpriteEnhancer.Core.Models.SpriteFilter.MatchesCategory(npcPath, npcCat));
        }
    }
}