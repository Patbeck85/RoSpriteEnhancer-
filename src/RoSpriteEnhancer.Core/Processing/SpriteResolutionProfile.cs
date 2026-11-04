using System.Collections.Generic;
using System.Drawing;

namespace RoSpriteEnhancer.Core.Processing
{
    /// <summary>
    /// TASK-1508: SpriteResolutionProfile – Auflösungsempfehlungen pro Sprite-Kategorie.
    ///
    /// Definiert für jede Kategorie die empfohlene Zielauflösung (Breite × Höhe),
    /// den empfohlenen Scale-Algorithmus und die Detail-Stärke.
    /// Diese Werte werden im Batch-Workflow automatisch angewendet, wenn für eine Kategorie
    /// kein expliziter Überschreibungswert gesetzt ist.
    /// </summary>
    public class SpriteResolutionProfile
    {
        public string CategoryId   { get; set; } = "";
        public string DisplayName  { get; set; } = "";
        public int    TargetWidth  { get; set; } = 256;
        public int    TargetHeight { get; set; } = 256;
        public ScaleAlgorithm RecommendedAlgorithm { get; set; } = ScaleAlgorithm.RealESRGAN4x;
        public float  DetailStrength { get; set; } = 0.35f;
        public string Description  { get; set; } = "";

        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Berechnet den benötigten Scale-Faktor basierend auf der Ursprungsgröße.
        /// </summary>
        public int CalculateScaleFactor(int sourceWidth, int sourceHeight)
        {
            if (sourceWidth <= 0 || sourceHeight <= 0) return 1;
            int scaleW = TargetWidth / sourceWidth;
            int scaleH = TargetHeight / sourceHeight;
            int scale = System.Math.Min(scaleW, scaleH);
            return System.Math.Max(1, scale);
        }

        /// <summary>
        /// Berechnet die Zielmaße basierend auf der Ursprungsgröße und dem Scale-Faktor.
        /// </summary>
        public Size CalculateTargetSize(int sourceWidth, int sourceHeight, int scaleFactor)
        {
            return new Size(sourceWidth * scaleFactor, sourceHeight * scaleFactor);
        }
    }

    /// <summary>
    /// TASK-1508: Auflösung-Empfehlungen für alle RO Sprite-Kategorien.
    /// Basierend auf der Analyse der Sprite-Gruppen und deren typischen Ursprungsgrößen.
    /// </summary>
    public static class SpriteResolutionProfiles
    {
        public static readonly Dictionary<string, SpriteResolutionProfile> All = new(StringComparer.OrdinalIgnoreCase)
        {
            // ── Items (Boden-Drops) ──
            ["items"] = new SpriteResolutionProfile
            {
                CategoryId = "items", DisplayName = "Items (Boden-Drops)",
                TargetWidth = 128, TargetHeight = 128,
                RecommendedAlgorithm = ScaleAlgorithm.RealESRGAN4x,
                DetailStrength = 0.30f,
                Description = "16×16 Ursprung → 4× = 64×64, empfohlen 128×128 für klare Konturen"
            },

            // ── Haarstile ──
            ["hair_m"] = new SpriteResolutionProfile
            {
                CategoryId = "hair_m", DisplayName = "Haarstile (Männlich)",
                TargetWidth = 256, TargetHeight = 256,
                RecommendedAlgorithm = ScaleAlgorithm.RealESRGAN4x,
                DetailStrength = 0.40f,
                Description = "32×32 Ursprung → 4× = 128×128, empfohlen 256×256 für feine Strähnen"
            },
            ["hair_f"] = new SpriteResolutionProfile
            {
                CategoryId = "hair_f", DisplayName = "Haarstile (Weiblich)",
                TargetWidth = 256, TargetHeight = 256,
                RecommendedAlgorithm = ScaleAlgorithm.RealESRGAN4x,
                DetailStrength = 0.40f,
                Description = "32×32 Ursprung → 4× = 128×128, empfohlen 256×256 für feine Strähnen"
            },

            // ── Jobklassen ──
            ["jobs_m"] = new SpriteResolutionProfile
            {
                CategoryId = "jobs_m", DisplayName = "Jobklassen (Männlich)",
                TargetWidth = 256, TargetHeight = 256,
                RecommendedAlgorithm = ScaleAlgorithm.RealESRGAN4x,
                DetailStrength = 0.35f,
                Description = "32×32 Ursprung → 4× = 128×128, empfohlen 256×256 für Kleidungsfalten"
            },
            ["jobs_f"] = new SpriteResolutionProfile
            {
                CategoryId = "jobs_f", DisplayName = "Jobklassen (Weiblich)",
                TargetWidth = 256, TargetHeight = 256,
                RecommendedAlgorithm = ScaleAlgorithm.RealESRGAN4x,
                DetailStrength = 0.35f,
                Description = "32×32 Ursprung → 4× = 128×128, empfohlen 256×256 für Kleidungsfalten"
            },

            // ── Monster ──
            ["monsters"] = new SpriteResolutionProfile
            {
                CategoryId = "monsters", DisplayName = "Monster",
                TargetWidth = 256, TargetHeight = 256,
                RecommendedAlgorithm = ScaleAlgorithm.RealESRGAN4x,
                DetailStrength = 0.35f,
                Description = "32–64× Ursprung → 4× = 128–256×, empfohlen 256×256 (Boss bis 512×512)"
            },

            // ── NPCs ──
            ["npc"] = new SpriteResolutionProfile
            {
                CategoryId = "npc", DisplayName = "NPCs",
                TargetWidth = 256, TargetHeight = 256,
                RecommendedAlgorithm = ScaleAlgorithm.RealESRGAN4x,
                DetailStrength = 0.35f,
                Description = "32×32 Ursprung → 4× = 128×128, empfohlen 256×256 für Gesichtsdetails"
            },

            // ── Pets / Homunculi ──
            ["pets"] = new SpriteResolutionProfile
            {
                CategoryId = "pets", DisplayName = "Pets / Haustiere",
                TargetWidth = 256, TargetHeight = 256,
                RecommendedAlgorithm = ScaleAlgorithm.RealESRGAN4x,
                DetailStrength = 0.35f,
                Description = "32×32 Ursprung → 4× = 128×128, empfohlen 256×256"
            },
            ["homunculi"] = new SpriteResolutionProfile
            {
                CategoryId = "homunculi", DisplayName = "Homunculi & Söldner",
                TargetWidth = 256, TargetHeight = 256,
                RecommendedAlgorithm = ScaleAlgorithm.RealESRGAN4x,
                DetailStrength = 0.35f,
                Description = "32×32 Ursprung → 4× = 128×128, empfohlen 256×256"
            },

            // ── Mounts / Carts ──
            ["mounts_carts"] = new SpriteResolutionProfile
            {
                CategoryId = "mounts_carts", DisplayName = "Reittiere & Carts",
                TargetWidth = 384, TargetHeight = 384,
                RecommendedAlgorithm = ScaleAlgorithm.RealESRGAN4x,
                DetailStrength = 0.30f,
                Description = "48–64× Ursprung → 4× = 192–256×, empfohlen 384×384"
            },

            // ── Accessoires ──
            ["accessories"] = new SpriteResolutionProfile
            {
                CategoryId = "accessories", DisplayName = "Accessoires / Kopfschmuck",
                TargetWidth = 128, TargetHeight = 128,
                RecommendedAlgorithm = ScaleAlgorithm.RealESRGAN4x,
                DetailStrength = 0.40f,
                Description = "16–32× Ursprung → 4× = 64–128×, empfohlen 128×128 für hohe Pixeldichte"
            },

            // ── Effekte / Skills ──
            ["effects"] = new SpriteResolutionProfile
            {
                CategoryId = "effects", DisplayName = "Effekte & Skills",
                TargetWidth = 256, TargetHeight = 256,
                RecommendedAlgorithm = ScaleAlgorithm.RealESRGAN4x,
                DetailStrength = 0.30f,
                Description = "32–64× Ursprung → 4× = 128–256×, empfohlen 256×256"
            },

            // ── Texturen ──
            ["textures"] = new SpriteResolutionProfile
            {
                CategoryId = "textures", DisplayName = "Map-Texturen",
                TargetWidth = 512, TargetHeight = 512,
                RecommendedAlgorithm = ScaleAlgorithm.RealESRGAN4x,
                DetailStrength = 0.25f,
                Description = "Variiert, empfohlen 512×512 für höchste Texturqualität"
            },

            // ── Emotes ──
            ["emotes"] = new SpriteResolutionProfile
            {
                CategoryId = "emotes", DisplayName = "Emotes & Gefühle",
                TargetWidth = 128, TargetHeight = 128,
                RecommendedAlgorithm = ScaleAlgorithm.RealESRGAN4x,
                DetailStrength = 0.40f,
                Description = "16–32× Ursprung → 4× = 64–128×, empfohlen 128×128"
            },

            // ── Costumes ──
            ["costumes"] = new SpriteResolutionProfile
            {
                CategoryId = "costumes", DisplayName = "Costumes / Robes / Wings",
                TargetWidth = 256, TargetHeight = 256,
                RecommendedAlgorithm = ScaleAlgorithm.RealESRGAN4x,
                DetailStrength = 0.35f,
                Description = "32×32 Ursprung → 4× = 128×128, empfohlen 256×256 für Stofffalten"
            },

            // ── Waffen & Schilde ──
            ["weapons_shields"] = new SpriteResolutionProfile
            {
                CategoryId = "weapons_shields", DisplayName = "Waffen & Schilde",
                TargetWidth = 256, TargetHeight = 256,
                RecommendedAlgorithm = ScaleAlgorithm.RealESRGAN4x,
                DetailStrength = 0.35f,
                Description = "Variiert, empfohlen 256×256 für klare Kanten"
            },
        };

        /// <summary>
        /// Gibt das Profil für eine Kategorie zurück, oder null wenn nicht gefunden.
        /// </summary>
        public static SpriteResolutionProfile? Get(string categoryId)
        {
            return All.TryGetValue(categoryId, out var profile) ? profile : null;
        }

        /// <summary>
        /// Gibt alle Profile als Liste zurück (für UI-Bindung).
        /// </summary>
        public static List<SpriteResolutionProfile> GetAll() => new(All.Values);
    }
}
