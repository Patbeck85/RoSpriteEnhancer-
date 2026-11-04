using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;

namespace RoSpriteEnhancer.Core.Utils
{
    /// <summary>
    /// Verwaltet benutzerdefinierte Farbpaletten für RO-Sprites.
    /// Paletten können aus .pal (Adobe Photoshop), .act (RIFF ACT) oder .json Dateien geladen
    /// und auf indizierte Sprite-Frames angewendet werden.
    /// </summary>
    public static class PaletteManager
    {
        private static readonly string PaletteDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RoSpriteEnhancer", "Palettes");

        static PaletteManager()
        {
            try { Directory.CreateDirectory(PaletteDir); } catch { }
        }

        /// <summary>
        /// Gibt alle verfügbaren Paletten-Namen zurück.
        /// </summary>
        public static List<string> GetAvailablePaletteNames()
        {
            var names = new List<string>();
            try
            {
                if (Directory.Exists(PaletteDir))
                {
                    foreach (var file in Directory.GetFiles(PaletteDir, "*.palette.json"))
                    {
                        names.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }
            }
            catch { }
            return names;
        }

        /// <summary>
        /// Lädt eine Palette aus einer JSON-Datei.
        /// </summary>
        public static Color[]? LoadPalette(string paletteName)
        {
            if (string.IsNullOrWhiteSpace(paletteName)) return null;

            string filePath = Path.Combine(PaletteDir, $"{SanitizeFileName(paletteName)}.palette.json");
            if (!File.Exists(filePath)) return null;

            try
            {
                string json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<PaletteData>(json);
                if (data?.Colors != null)
                {
                    var colors = new Color[data.Colors.Count];
                    for (int i = 0; i < data.Colors.Count; i++)
                    {
                        colors[i] = Color.FromArgb(data.Colors[i].A, data.Colors[i].R, data.Colors[i].G, data.Colors[i].B);
                    }
                    return colors;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Speichert eine Palette als JSON-Datei.
        /// </summary>
        public static bool SavePalette(string paletteName, Color[] colors)
        {
            if (string.IsNullOrWhiteSpace(paletteName) || colors == null) return false;

            try
            {
                Directory.CreateDirectory(PaletteDir);
                string filePath = Path.Combine(PaletteDir, $"{SanitizeFileName(paletteName)}.palette.json");

                var data = new PaletteData
                {
                    Name = paletteName,
                    Colors = new List<PaletteColor>()
                };
                foreach (var c in colors)
                {
                    data.Colors.Add(new PaletteColor { R = c.R, G = c.G, B = c.B, A = c.A });
                }

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Löscht eine gespeicherte Palette.
        /// </summary>
        public static bool DeletePalette(string paletteName)
        {
            if (string.IsNullOrWhiteSpace(paletteName)) return false;
            try
            {
                string filePath = Path.Combine(PaletteDir, $"{SanitizeFileName(paletteName)}.palette.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Wendet eine Palette auf ein indiziertes Bitmap an.
        /// Ersetzt die Farben des Bitmaps mit den Palette-Farben basierend auf dem Palette-Index.
        /// </summary>
        public static Bitmap? ApplyPaletteToIndexed(Bitmap src, Color[] palette)
        {
            if (src == null || palette == null) return null;

            int w = src.Width, h = src.Height;
            var result = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            BitmapData srcData = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] srcPixels = new int[w * h];
            try { MarshalCopy(srcData, srcPixels); }
            finally { src.UnlockBits(srcData); }

            int[] dstPixels = new int[w * h];
            for (int i = 0; i < srcPixels.Length; i++)
            {
                int pixel = srcPixels[i];
                int alpha = (pixel >> 24) & 0xFF;

                if (alpha < 10)
                {
                    dstPixels[i] = pixel; // Transparent bleibt transparent
                    continue;
                }

                // Der niedrigere Byte des Pixel-Werts wird als Palette-Index interpretiert
                int paletteIndex = pixel & 0xFF;
                if (paletteIndex >= 0 && paletteIndex < palette.Length)
                {
                    Color c = palette[paletteIndex];
                    dstPixels[i] = (alpha << 24) | (c.R << 16) | (c.G << 8) | c.B;
                }
                else
                {
                    dstPixels[i] = pixel; // Unbekannter Index, Original behalten
                }
            }

            var dstData = result.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try { MarshalCopy(dstPixels, dstData); }
            finally { result.UnlockBits(dstData); }

            return result;
        }

        /// <summary>
        /// Erstellt eine Standard-RO-Palette basierend auf typischen RO-Farben.
        /// </summary>
        public static Color[] CreateDefaultRoPalette()
        {
            var colors = new Color[256];
            // Slot 0 = Transparent
            colors[0] = Color.Transparent;

            // Einige typische RO-Farben für die ersten Slots
            colors[1] = Color.FromArgb(255, 0, 0);     // Rot
            colors[2] = Color.FromArgb(0, 255, 0);     // Grün
            colors[3] = Color.FromArgb(0, 0, 255);     // Blau
            colors[4] = Color.FromArgb(255, 255, 0);   // Gelb
            colors[5] = Color.FromArgb(255, 0, 255);   // Magenta
            colors[6] = Color.FromArgb(0, 255, 255);   // Cyan
            colors[7] = Color.FromArgb(255, 128, 0);   // Orange
            colors[8] = Color.FromArgb(128, 0, 128);   // Lila
            colors[9] = Color.FromArgb(128, 128, 128); // Grau

            // Rest mit Farbverlauf füllen
            for (int i = 10; i < 256; i++)
            {
                int val = (i * 255) / 246;
                colors[i] = Color.FromArgb(val, val, val); // Graustufen-Verlauf
            }

            return colors;
        }

        #region Hilfsmethoden

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            fileName = fileName.Replace("..", "_").Replace("/", "_").Replace("\\", "_");
            return fileName;
        }

        private static void MarshalCopy(BitmapData data, int[] pixels)
        {
            int length = pixels.Length;
            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                for (int y = 0; y < data.Height; y++)
                {
                    byte* rowPtr = ptr + y * data.Stride;
                    for (int x = 0; x < data.Width; x++)
                    {
                        byte* pixelPtr = rowPtr + x * 4;
                        int b = pixelPtr[0];
                        int g = pixelPtr[1];
                        int r = pixelPtr[2];
                        int a = pixelPtr[3];
                        pixels[y * data.Width + x] = (a << 24) | (r << 16) | (g << 8) | b;
                    }
                }
            }
        }

        private static void MarshalCopy(int[] pixels, BitmapData data)
        {
            int length = pixels.Length;
            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                for (int y = 0; y < data.Height; y++)
                {
                    byte* rowPtr = ptr + y * data.Stride;
                    for (int x = 0; x < data.Width; x++)
                    {
                        int pixel = pixels[y * data.Width + x];
                        int a = (pixel >> 24) & 0xFF;
                        int r = (pixel >> 16) & 0xFF;
                        int g = (pixel >> 8) & 0xFF;
                        int b = pixel & 0xFF;
                        byte* pixelPtr = rowPtr + x * 4;
                        pixelPtr[0] = (byte)b;
                        pixelPtr[1] = (byte)g;
                        pixelPtr[2] = (byte)r;
                        pixelPtr[3] = (byte)a;
                    }
                }
            }
        }

        #endregion

        #region Datentypen für Serialisierung

        private class PaletteData
        {
            public string Name { get; set; } = "";
            public List<PaletteColor> Colors { get; set; } = new();
        }

        private class PaletteColor
        {
            public byte R { get; set; }
            public byte G { get; set; }
            public byte B { get; set; }
            public byte A { get; set; } = 255;
        }

        #endregion
    }
}