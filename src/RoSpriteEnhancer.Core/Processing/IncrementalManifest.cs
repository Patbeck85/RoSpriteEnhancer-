using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace RoSpriteEnhancer.Core.Processing
{
    /// <summary>
    /// Merkt sich pro verarbeiteter Datei einen Hash aus (Dateiinhalt + genutzte
    /// Einstellungen). Bei einem erneuten Batch-Lauf mit identischer Datei UND
    /// identischen Einstellungen wird die Verarbeitung übersprungen — nützlich
    /// wenn sich bei einem Client-Update nur ein Bruchteil der Sprites ändert.
    ///
    /// Das Manifest liegt als JSON-Datei im Ausgabe-Ordner (versteckt per
    /// führendem Punkt) und wird NUR gelesen bevor der Batch startet und
    /// EINMAL am Ende komplett neu geschrieben — kein paralleler Datei-Zugriff
    /// während der eigentlichen Verarbeitung.
    /// </summary>
    public class IncrementalManifest
    {
        private const string FileName = ".rose_manifest.json";

        // relativer Pfad → (Inhalts-Hash, Einstellungs-Hash)
        public Dictionary<string, ManifestEntry> Entries { get; set; } = new();

        public class ManifestEntry
        {
            public string ContentHash  { get; set; } = "";
            public string SettingsHash { get; set; } = "";
        }

        public static IncrementalManifest Load(string outputFolder)
        {
            string path = Path.Combine(outputFolder, FileName);
            if (!File.Exists(path)) return new IncrementalManifest();
            try
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<IncrementalManifest>(json) ?? new IncrementalManifest();
            }
            catch
            {
                return new IncrementalManifest(); // Beschädigtes Manifest — einfach neu anfangen
            }
        }

        public void Save(string outputFolder)
        {
            try
            {
                Directory.CreateDirectory(outputFolder);
                string path = Path.Combine(outputFolder, FileName);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch
            {
                // Manifest konnte nicht gespeichert werden — nicht kritisch,
                // der nächste Lauf verarbeitet dann halt alles neu statt zu crashen.
            }
        }

        /// <summary>MD5-Hash der Dateibytes — schnell genug für typische Sprite-
        /// Dateigrößen (KB-Bereich), robust gegen bloße Zeitstempel-Änderungen
        /// (z.B. erneute GRF-Extraktion mit identischem Inhalt).</summary>
        public static string ComputeContentHash(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hash = md5.ComputeHash(stream);
            return Convert.ToHexString(hash);
        }

        /// <summary>Hash über alle Einstellungen die das Verarbeitungsergebnis
        /// beeinflussen. Ändert sich EIN Regler, ändert sich der Hash — die
        /// Datei wird dann trotz unverändertem Inhalt neu verarbeitet.</summary>
        public static string ComputeSettingsHash(ProcessingOptions opts, EnhancerOptions enhancer)
        {
            // Bewusst ein einfacher, deterministischer String aus allen
            // relevanten Feldern — kein Reflection-basiertes Serialisieren,
            // damit exakt nachvollziehbar bleibt was den Hash beeinflusst.
            string fingerprint = string.Join("|",
                opts.ScaleAlgorithm, opts.ScalePasses,
                opts.HandleMagentaKey, opts.AlphaThreshold,
                opts.OutputFormat, opts.ScaleActAnchors,
                opts.SuperSampler.Enabled, opts.SuperSampler.Filter, opts.SuperSampler.TargetSize,
                opts.Esrgan.ExecutablePath, opts.Esrgan.ModelName, opts.Esrgan.ScaleFactor,
                enhancer.Outline, enhancer.OutlineStrength, enhancer.OutlineMode,
                enhancer.Shadow, enhancer.ShadowStrength,
                enhancer.Highlight, enhancer.HighlightStrength,
                enhancer.Sharpen, enhancer.SharpenStrength,
                enhancer.ExclusionColor, enhancer.HueShift, enhancer.SaturationScale, enhancer.LightnessScale);

            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(fingerprint));
            return Convert.ToHexString(hash);
        }
    }
}
