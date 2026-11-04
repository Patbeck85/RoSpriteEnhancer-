using System.Text;

namespace RoSpriteEnhancer.Core.Models
{
    public static class SpriteFilter
    {
        static SpriteFilter()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private static readonly string[] WeaponKeywords = new[]
        {
            "_검", "_활", "_창", "_단검", "_지팡이", "_도끼", "_채찍", "_카타르", "_총", "_너클", "_수리검", "_무기", "방패",
            "_°Ë", "_È°", "_Â¢", "_´Ü°Ë", "_ÁöÆÎÀÌ", "_µµö¢", "_Ã¤Âï", "_««Å¸¸", "_ÃÑ", "_³Ê¬", "_¼ö¸®°Ë", "ºñÆa",
            "weapon", "weapons", "shield", "shields"
        };

        public static bool IsWeaponFile(string path)
        {
            foreach (var kw in WeaponKeywords)
                if (path.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        public static readonly SpriteCategory[] AllCategories = new[]
        {
            // ── 1. JOBS (männlich) ──
            new SpriteCategory("jobs_m", "Jobklassen (Männlich)", "👨", new[]
            {
                @"data\sprite\인간족\남\",
                @"data\sprite\ÀÎ°£Á·\³²\",
                @"인간족\남\",
                @"ÀÎ°£Á·\³²\",
                @"인간족\몸\남\",
                @"ÀÎ°£Á·\ô\³²\"
            }),

            // ── 2. JOBS (weiblich) ──
            new SpriteCategory("jobs_f", "Jobklassen (Weiblich)", "👩", new[]
            {
                @"data\sprite\인간족\여\",
                @"data\sprite\ÀÎ°£Á·\¿©\",
                @"인간족\여\",
                @"ÀÎ°£Á·\¿©\",
                @"인간족\몸\여\",
                @"ÀÎ°£Á·\ô\¿©\"
            }),

            // ── 3. HAIRS (männlich) ──
            new SpriteCategory("hair_m", "Haarstile (Männlich)", "🦱", new[]
            {
                @"머리통\남\",
                @"¸Ó¸®Åë\³²\"
            }),

            // ── 4. HAIRS (weiblich) ──
            new SpriteCategory("hair_f", "Haarstile (Weiblich)", "👱‍♀️", new[]
            {
                @"머리통\여\",
                @"¸Ó¸®Åë\¿©\"
            }),

            // ── 5. MONSTERS ──
            new SpriteCategory("monsters", "Monster", "👾", new[]
            {
                @"data\sprite\몬스터\",
                @"data\sprite\¸ó½ºÅÍ\",
                @"몬스터\",
                @"¸ó½ºÅÍ\"
            }),

            // ── 6. PETS ──
            new SpriteCategory("pets", "Pets / Haustiere", "🐾", new[]
            {
                @"data\sprite\펫\",
                @"data\sprite\Æeb\",
                @"\펫\",
                @"\Æeb\"
            }),

            // ── 7. MOUNTS & CARTS ──
            new SpriteCategory("mounts_carts", "Reittiere, Carts & Mado", "🏇", new[]
            {
                @"data\sprite\탈것\",
                @"data\sprite\수레\",
                @"탈것\",
                @"수레\"
            }),

            // ── 8. NPCs ──
            new SpriteCategory("npc", "NPCs", "🗣", new[]
            {
                @"data\sprite\npc\",
                @"sprite\npc\"
            }),

            // ── 9. WEAPONS & SHIELDS ──
            new SpriteCategory("weapons_shields", "Waffen & Schilde", "🛡", new[]
            {
                @"방패",
                @"\xB9\xE6\xC6\xD0",
                @"무기",
                @"\xB9\xAB\xB1\xE2",
                @"_검",
                @"_활",
                @"_창",
                @"_단검",
                @"_지팡이",
                @"_도끼",
                @"_채찍",
                @"_카타르",
                @"_총",
                @"_너클",
                @"_수리검",
                @"_무기"
            }),

            // ── 10. ACCESSORIES ──
            new SpriteCategory("accessories", "Accessoires / Kopfschmuck", "👑", new[]
            {
                @"악세사리\",
                @"\xBD\xC7\xBC\\xBC\xBD\xC3\xB8\\xAE\", 
                @"¾Ç¼¼»ç¸®\"
            }),

            // ── 11. EFFECTS & SKILLS ──
            new SpriteCategory("effects", "Effekte & Skills", "✨", new[]
            {
                @"data\sprite\효과\",
                @"\효과\",
                @"효과\",
                @"data\sprite\È¿°ú\",
                @"\È¿°ú\",
                @"È¿°ú\",
                @"\effect\",
                @"\effects\",
                @"effect\",
                @"effects\"
            }),

            // ── 12. TEXTURES ──
            new SpriteCategory("textures", "Map-Texturen", "🏔", new[]
            {
                @"data\texture\",
                @"texture\"
            }),

            // ── 13. HOMUNCULI & MERCENARIES ──
            new SpriteCategory("homunculi", "Homunculi & Söldner", "🧬", new[]
            {
                @"data\sprite\가상인간족\",
                @"\가상인간족\",
                @"가상인간족\",
                @"data\sprite\°¡»óÀÎ°£Á·\",
                @"\°¡»óÀÎ°£Á·\",
                @"°¡»óÀÎ°£Á·\",
                @"data\sprite\용병\",
                @"\용병\",
                @"용병\",
                @"data\sprite\¿ëº´\",
                @"\¿ëº´\",
                @"¿ëº´\",
                @"homunculus\",
                @"homunculi\",
                @"mercenary\",
                @"mercenaries\"
            }),

            // ── 14. ITEMS ──
            new SpriteCategory("items", "Items (Boden-Drops)", "🎒", new[]
            {
                @"아이템\",
                @"\xBE\xC6\xC0\xCC\\xC5\\xDB\", 
                @"¾ÆÀÌÅÛ\"
            }),

            // ── 15. EMOTES ──
            new SpriteCategory("emotes", "Emotes & Gefühle", "💬", new[]
            {
                @"data\sprite\감정표현\",
                @"data\sprite\°¨Á¤Ç¥Çö\",
                @"emotion\",
                @"감정표현\"
            }),

            // ── 16. COSTUMES ──
            new SpriteCategory("costumes", "Costumes / Robes / Wings", "🎭", new[]
            {
                @"로브\",
                @"·Îºê\",
                @"costume\",
                @"robe\"
            }),
        };

        public static bool Matches(string grfPath, IEnumerable<string> selectedCategoryIds, string customPath = "")
        {
            var ids = selectedCategoryIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(customPath) && grfPath.Contains(customPath.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            if (ids.Count == 0) return true;

            foreach (var cat in AllCategories)
            {
                if (!ids.Contains(cat.Id)) continue;
                if (MatchesCategory(grfPath, cat)) return true;
            }
            return false;
        }

        public static bool MatchesCategory(string path, SpriteCategory category)
        {
            return MatchesCategoryInternal(path, category);
        }

        private static bool MatchesCategoryInternal(string original, SpriteCategory cat)
        {
            foreach (string fragment in cat.PathFragments)
            {
                if (original.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                    return true;

                try
                {
                    var eucFragment = Encoding.GetEncoding("EUC-KR").GetBytes(fragment);
                    var eucPath = Encoding.GetEncoding("EUC-KR").GetBytes(original);
                    if (ContainsBytes(eucPath, eucFragment))
                        return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SpriteFilter] EUC-KR Match fehlgeschlagen für '{fragment}': {ex.Message}");
                }
            }
            return false;
        }

        private static bool ContainsBytes(byte[] haystack, byte[] needle)
        {
            if (needle.Length == 0) return true;
            if (needle.Length > haystack.Length) return false;
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j]) { match = false; break; }
                }
                if (match) return true;
            }
            return false;
        }

        public static Dictionary<string, int> CountByCategory(IEnumerable<string> grfPaths)
        {
            var counts = AllCategories.ToDictionary(c => c.Id, _ => 0);
            counts["other"] = 0;

            foreach (string path in grfPaths)
            {
                bool matched = false;
                foreach (var cat in AllCategories)
                {
                    if (MatchesCategory(path, cat))
                    {
                        counts[cat.Id]++;
                        matched = true;
                        break;
                    }
                }
                if (!matched) counts["other"]++;
            }
            return counts;
        }
    }

    public class SpriteCategory
    {
        public string   Id            { get; }
        public string   DisplayName   { get; }
        public string   Icon          { get; }
        public string[] PathFragments { get; }

        public bool IsSelected { get; set; } = true;
        public string Label => $"{Icon}  {DisplayName}";

        public SpriteCategory(string id, string displayName, string icon, string[] pathFragments)
        {
            Id = id; DisplayName = displayName; Icon = icon; PathFragments = pathFragments;
        }
    }
}
