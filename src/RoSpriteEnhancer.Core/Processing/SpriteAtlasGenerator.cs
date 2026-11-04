using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RoSpriteEnhancer.Core.Processing
{
    /// <summary>
    /// Beschreibt einen einzelnen Sprite-Eintrag im Atlas.
    /// </summary>
    public class AtlasSpriteEntry
    {
        public string FileName { get; set; } = "";
        public string OriginalPath { get; set; } = "";
        public int AtlasIndex { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int OriginalWidth { get; set; }
        public int OriginalHeight { get; set; }
    }

    /// <summary>
    /// Metadaten für einen kompletten Sprite-Atlas.
    /// </summary>
    public class AtlasMetadata
    {
        public string Version { get; set; } = "1.0";
        public int AtlasWidth { get; set; }
        public int AtlasHeight { get; set; }
        public int SpriteCount { get; set; }
        public List<AtlasSpriteEntry> Sprites { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Erstellt Sprite-Atlas / Spritesheets aus mehreren Einzelbildern.
    /// Packt Sprites in ein Grid-Muster und schreibt eine JSON-Metadatendatei
    /// mit den Koordinaten jedes Sprites, damit sie später wieder extrahiert werden können.
    /// </summary>
    public static class SpriteAtlasGenerator
    {
        /// <summary>
        /// Maximale Atlas-Größe pro Seite (in Pixeln). Größere Atlanten werden aufgeteilt.
        /// </summary>
        public const int MaxAtlasSize = 4096;

        /// <summary>
        /// Abstand zwischen einzelnen Sprites im Atlas (vermeidet Farb-Bläuen an den Rändern).
        /// </summary>
        public const int Padding = 2;

        /// <summary>
        /// Erstellt einen oder mehrere Sprite-Atlanten aus den angegebenen Bilddateien.
        /// </summary>
        /// <param name="imagePaths">Pfade zu den Quell-Bildern (BMP, PNG, TGA).</param>
        /// <param name="outputDir">Zielverzeichnis für die Atlas-Bilder und JSON-Metadaten.</param>
        /// <param name="baseName">Basisname für die Ausgabedateien (z. B. "monster_atlas").</param>
        /// <returns>Liste der erstellten Atlas-Metadaten (eine pro Atlas-Datei).</returns>
        public static List<AtlasMetadata> CreateAtlas(
            List<string> imagePaths,
            string outputDir,
            string baseName)
        {
            if (imagePaths == null || imagePaths.Count == 0)
                return new List<AtlasMetadata>();

            Directory.CreateDirectory(outputDir);

            // Bilder laden und nach Größe sortieren (größte zuerst für besseres Packen)
            var loaded = new List<(string Path, Bitmap Bitmap, string FileName)>();
            foreach (var path in imagePaths)
            {
                try
                {
                    var bmp = new Bitmap(path);
                    loaded.Add((path, bmp, Path.GetFileName(path)));
                }
                catch
                {
                    // Datei überspringen, wenn sie nicht geladen werden kann
                }
            }

            if (loaded.Count == 0)
                return new List<AtlasMetadata>();

            var atlases = new List<AtlasMetadata>();

            // Einfache Grid-Packung: Sprites werden zeilenweise in ein Atlas gepackt
            var currentAtlas = new List<(string Path, Bitmap Bitmap, string FileName)>();
            int currentAtlasIndex = 0;
            int currentRowY = 0;
            int currentRowMaxH = 0;
            int currentX = 0;
            int atlasW = 0;
            int atlasH = 0;

            // Erstes Atlas vorbereiten
            var maxW = loaded.Max(x => x.Bitmap.Width);
            var maxH = loaded.Max(x => x.Bitmap.Height);
            atlasW = Math.Min(MaxAtlasSize, Math.Max(maxW + Padding * 2, 512));
            atlasH = MaxAtlasSize;

            foreach (var item in loaded)
            {
                int spriteW = item.Bitmap.Width + Padding * 2;
                int spriteH = item.Bitmap.Height + Padding * 2;

                // Prüfen ob Sprite in aktuelle Zeile passt
                if (currentX + spriteW > atlasW)
                {
                    // Zeile umbrechen
                    currentX = 0;
                    currentRowY += currentRowMaxH + Padding;
                    currentRowMaxH = 0;
                }

                // Prüfen ob Sprite in aktuelle Spalte/Atlas passt
                if (currentRowY + spriteH > atlasH)
                {
                    // Neues Atlas starten
                    if (currentAtlas.Count > 0)
                    {
                        atlases.Add(BuildAtlas(currentAtlasIndex, currentAtlas, atlasW, atlasH, outputDir, baseName));
                        currentAtlasIndex++;
                        currentAtlas = new List<(string, Bitmap, string)>();
                    }

                    currentRowY = 0;
                    currentX = 0;
                    currentRowMaxH = 0;
                }

                currentAtlas.Add(item);
                currentRowMaxH = Math.Max(currentRowMaxH, spriteH);
                currentX += spriteW;
            }

            // Letztes Atlas abschließen
            if (currentAtlas.Count > 0)
            {
                atlases.Add(BuildAtlas(currentAtlasIndex, currentAtlas, atlasW, atlasH, outputDir, baseName));
            }

            // Alle geladenen Bitmaps entsorgen
            foreach (var item in loaded)
            {
                item.Bitmap.Dispose();
            }

            return atlases;
        }

        private static AtlasMetadata BuildAtlas(
            int atlasIndex,
            List<(string Path, Bitmap Bitmap, string FileName)> sprites,
            int atlasW,
            int atlasH,
            string outputDir,
            string baseName)
        {
            var atlasBmp = new Bitmap(atlasW, atlasH, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(atlasBmp);
            g.Clear(Color.Transparent);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            var metadata = new AtlasMetadata
            {
                AtlasWidth = atlasW,
                AtlasHeight = atlasH,
                SpriteCount = sprites.Count
            };

            int currentX = 0;
            int currentRowY = 0;
            int currentRowMaxH = 0;

            foreach (var (path, bmp, fileName) in sprites)
            {
                int spriteW = bmp.Width + Padding * 2;
                int spriteH = bmp.Height + Padding * 2;

                // Zeilenumbruch wenn nötig
                if (currentX + spriteW > atlasW)
                {
                    currentX = 0;
                    currentRowY += currentRowMaxH + Padding;
                    currentRowMaxH = 0;
                }

                int drawX = currentX + Padding;
                int drawY = currentRowY + Padding;

                g.DrawImage(bmp, drawX, drawY, bmp.Width, bmp.Height);

                metadata.Sprites.Add(new AtlasSpriteEntry
                {
                    FileName = fileName,
                    OriginalPath = path,
                    AtlasIndex = atlasIndex,
                    X = drawX,
                    Y = drawY,
                    Width = bmp.Width,
                    Height = bmp.Height,
                    OriginalWidth = bmp.Width,
                    OriginalHeight = bmp.Height
                });

                currentRowMaxH = Math.Max(currentRowMaxH, spriteH);
                currentX += spriteW;
            }

            // Atlas-Bild speichern
            string atlasFileName = $"{baseName}_{atlasIndex:D3}.png";
            string atlasPath = Path.Combine(outputDir, atlasFileName);
            atlasBmp.Save(atlasPath, ImageFormat.Png);

            // Metadaten-JSON speichern
            string jsonFileName = $"{baseName}_{atlasIndex:D3}.atlas.json";
            string jsonPath = Path.Combine(outputDir, jsonFileName);
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(metadata, jsonOptions));

            atlasBmp.Dispose();

            return metadata;
        }

        /// <summary>
        /// Extrahiert einen einzelnen Sprite aus einem Atlas basierend auf der Metadatendatei.
        /// </summary>
        public static Bitmap? ExtractSprite(string atlasImagePath, string atlasJsonPath, string fileName)
        {
            try
            {
                if (!File.Exists(atlasImagePath) || !File.Exists(atlasJsonPath))
                    return null;

                var json = File.ReadAllText(atlasJsonPath);
                var metadata = JsonSerializer.Deserialize<AtlasMetadata>(json);
                if (metadata == null) return null;

                var entry = metadata.Sprites.FirstOrDefault(s => s.FileName == fileName);
                if (entry == null || entry.Width <= 0 || entry.Height <= 0)
                    return null;

                using var atlas = new Bitmap(atlasImagePath);
                var sprite = new Bitmap(entry.Width, entry.Height, PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(sprite);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.DrawImage(atlas, 0, 0, new Rectangle(entry.X, entry.Y, entry.Width, entry.Height), GraphicsUnit.Pixel);

                return sprite;
            }
            catch
            {
                return null;
            }
        }
    }
}