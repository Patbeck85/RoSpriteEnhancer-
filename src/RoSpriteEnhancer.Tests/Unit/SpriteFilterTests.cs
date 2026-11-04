using Xunit;

namespace RoSpriteEnhancer.Tests.Unit
{
    public class SpriteFilterTests
    {
        [Fact]
        public void IsWeaponFile_WithWeaponKeyword_ReturnsTrue()
        {
            Assert.True(RoSpriteEnhancer.Core.Models.SpriteFilter.IsWeaponFile("data/sprite/in간족/남/무사_검.spr"));
            Assert.True(RoSpriteEnhancer.Core.Models.SpriteFilter.IsWeaponFile("data/sprite/weapon_sword.spr"));
            Assert.True(RoSpriteEnhancer.Core.Models.SpriteFilter.IsWeaponFile("방패.spr"));
        }

        [Fact]
        public void IsWeaponFile_WithoutWeaponKeyword_ReturnsFalse()
        {
            Assert.False(RoSpriteEnhancer.Core.Models.SpriteFilter.IsWeaponFile("data/sprite/in간족/남/무사.spr"));
            Assert.False(RoSpriteEnhancer.Core.Models.SpriteFilter.IsWeaponFile("data/sprite/npc/kafra.spr"));
        }

        [Fact]
        public void MatchesCategory_KnownCategory_ReturnsTrue()
        {
            var monsterCat = RoSpriteEnhancer.Core.Models.SpriteFilter.AllCategories
                .First(c => c.Id == "monsters");

            Assert.True(RoSpriteEnhancer.Core.Models.SpriteFilter.MatchesCategory("data\\sprite\\몬스터\\poring.spr", monsterCat));
        }

        [Fact]
        public void MatchesCategory_UnknownCategory_ReturnsFalse()
        {
            var npcCat = RoSpriteEnhancer.Core.Models.SpriteFilter.AllCategories
                .First(c => c.Id == "npc");

            Assert.False(RoSpriteEnhancer.Core.Models.SpriteFilter.MatchesCategory("data\\sprite\\몬스터\\poring.spr", npcCat));
        }

        [Fact]
        public void Matches_MatchingCategoryId_ReturnsTrue()
        {
            // jobs_m Kategorie hat PathFragment "인간족\\남\\" (koreanisch)
            var result = RoSpriteEnhancer.Core.Models.SpriteFilter.Matches(
                "data\\sprite\\인간족\\남\\무사.spr",
                new[] { "jobs_m" });

            Assert.True(result);
        }

        [Fact]
        public void Matches_NoCategories_ReturnsTrue()
        {
            var result = RoSpriteEnhancer.Core.Models.SpriteFilter.Matches(
                "data/sprite/in간족/남/무사.spr",
                new string[0]);

            Assert.True(result);
        }
    }
}
