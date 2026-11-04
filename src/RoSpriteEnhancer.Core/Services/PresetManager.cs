using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using RoSpriteEnhancer.Core.Processing;
using RoSpriteEnhancer.Core.Upscaling;
using RoSpriteEnhancer.Core.Filtering;
using RoSpriteEnhancer.Core.IO;
using RoSpriteEnhancer.Core.Utils;

namespace RoSpriteEnhancer.Core.Services
{
    /// <summary>
    /// Kategorien für Presets — erleichtert das Auffinden passender Einstellungen
    /// für verschiedene RO-Editionen und Sprite-Typen.
    /// </summary>
    public enum PresetCategory
    {
        Classic,
        Renewal,
        Item,
        Monster,
        NPC,
        Custom,
    }

    /// <summary>
    /// Ein gespeichertes Einstellungs-Profil (Preset) — erfasst alle
    /// Wie soll verbessert werden-Einstellungen, aber bewusst NICHT die
    /// Eingabe-/Ausgabe-Ordner (die sind pro Batch-Lauf unterschiedlich,
    /// ein Preset soll wiederverwendbar über verschiedene Ordner hinweg sein).
    /// </summary>
    public class EnhancementPreset
    {
        public string Name { get; set; } = "";
        public PresetCategory Category { get; set; } = PresetCategory.Custom;

        // Skalierung
        public ScaleAlgorithm ScaleAlgorithm { get; set; } = ScaleAlgorithm.Scale2x;
        public int ScalePasses { get; set; } = 1;

        // Pixel-Shading
        public bool  Outline           { get; set; } = true;
        public float OutlineStrength   { get; set; } = 0.5f;
        public bool  Shadow            { get; set; } = true;
        public float ShadowStrength    { get; set; } = 1.0f;
        public bool  Highlight         { get; set; } = true;
        public float HighlightStrength { get; set; } = 1.0f;
        public bool  Sharpen           { get; set; } = false;
        public float SharpenStrength   { get; set; } = 0.3f;
        public bool  Volumetric3D      { get; set; } = false;
        public float Volumetric3DStrength { get; set; } = 0.75f;

        // AI-First Pipeline Options
        public AiFirstOptions AiFirst { get; set; } = new();

        // Transparenz / Ausgabe
        public bool HandleMagentaKey { get; set; } = true;
        public int  AlphaThreshold   { get; set; } = 128;
        public OutputFormat OutputFormat { get; set; } = OutputFormat.Png;

        // Dateitypen-Filter
        public bool IncludeBmp   { get; set; } = true;
        public bool IncludePng   { get; set; } = true;
        public bool IncludeTga   { get; set; } = true;
        public bool IncludeSpr   { get; set; } = true;
        public bool Subfolders   { get; set; } = true;
        public bool ScaleActAnchors { get; set; } = false;

        // SuperSampler / SSAA
        public bool SuperSamplerEnabled { get; set; } = false;
        public SuperSampler.DownscaleFilter SuperSamplerFilter { get; set; } = SuperSampler.DownscaleFilter.Lanczos3;
        public SuperSampler.OutputSize SuperSamplerTargetSize { get; set; } = SuperSampler.OutputSize.TwoX;
    }

    /// <summary>
    /// Speichert/lädt Presets als JSON-Dateien unter
    /// %AppData%\RoSpriteEnhancer\Presets\{name}.json
    /// </summary>
    public static class PresetManager
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private static string PresetsDir =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RoSpriteEnhancer", "Presets");

        private static readonly HashSet<string> ReservedWindowsNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
            "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
        };

        /// <summary>Erlaubte Zeichen für Preset-Namen — verhindert Pfad-Traversal
        /// und ungültige Dateinamen (z.B. "../../evil" oder reservierte
        /// Windows-Gerätenamen wie "CON", die selbst mit Dateiendung Probleme
        /// verursachen können).</summary>
        private static string SanitizeName(string name)
        {
            name = name.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            // Zusätzlich Pfad-Trenner explizit blocken (falls Plattform sie nicht listet)
            name = name.Replace("..", "_").Replace("/", "_").Replace("\\", "_");
            if (string.IsNullOrWhiteSpace(name)) name = "Preset";
            if (ReservedWindowsNames.Contains(name)) name = "_" + name;
            return name;
        }

        public static void Save(EnhancementPreset preset)
        {
            Directory.CreateDirectory(PresetsDir);
            string safeName = SanitizeName(preset.Name);
            string path = Path.Combine(PresetsDir, safeName + ".json");
            string json = JsonSerializer.Serialize(preset, JsonOpts);
            File.WriteAllText(path, json);
        }

        public static EnhancementPreset? Load(string name)
        {
            string safeName = SanitizeName(name);
            string path = Path.Combine(PresetsDir, safeName + ".json");
            if (!File.Exists(path)) return null;
            try
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<EnhancementPreset>(json, JsonOpts);
            }
            catch
            {
                return null; // Korrupte/unlesbare Preset-Datei — einfach ignorieren
            }
        }

        public static List<string> ListNames()
        {
            if (!Directory.Exists(PresetsDir)) return new List<string>();
            return Directory.GetFiles(PresetsDir, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(n => !string.IsNullOrEmpty(n))
                .Select(n => n!)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static bool Delete(string name)
        {
            string safeName = SanitizeName(name);
            string path = Path.Combine(PresetsDir, safeName + ".json");
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        /// <summary>
        /// Exportiert ein Preset als JSON-Datei an einen beliebigen Ort (zum Teilen).
        /// </summary>
        public static bool Export(string presetName, string exportFilePath)
        {
            try
            {
                string safeName = SanitizeName(presetName);
                string sourcePath = Path.Combine(PresetsDir, safeName + ".json");
                if (!File.Exists(sourcePath)) return false;
                File.Copy(sourcePath, exportFilePath, true);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Importiert eine Preset-Datei von einem beliebigen Ort in den Preset-Ordner.
        /// Gibt den neuen Preset-Namen zurück oder null bei Fehler.
        /// </summary>
        public static string? Import(string importFilePath, string? newName = null)
        {
            try
            {
                if (!File.Exists(importFilePath)) return null;
                string json = File.ReadAllText(importFilePath);
                var preset = JsonSerializer.Deserialize<EnhancementPreset>(json, JsonOpts);
                if (preset == null) return null;

                string targetName = string.IsNullOrWhiteSpace(newName) ? preset.Name : newName;
                preset.Name = SanitizeName(targetName);
                Save(preset);
                return preset.Name;
            }
            catch { return null; }
        }
    }
}
