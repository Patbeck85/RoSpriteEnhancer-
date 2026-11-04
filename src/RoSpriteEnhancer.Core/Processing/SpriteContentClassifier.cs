using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RoSpriteEnhancer.Core.Processing
{
    /// <summary>
    /// Ergebnis der automatischen Inhalt-basierten Kategorie-Erkennung.
    /// </summary>
    public record ContentCategoryResult
    {
        public string PredictedCategoryId { get; init; } = "other";
        public string PredictedCategoryName { get; init; } = "Sonstige";
        public float Confidence { get; init; } // 0.0 = unsicher, 1.0 = sehr sicher
        public List<string> DetectedFeatures { get; init; } = new();
    }

    /// <summary>
    /// Analysiert Bildinhalte (Farben, Formen, Muster) um die Sprite-Kategorie automatisch zu bestimmen.
    /// Wird verwendet wenn der Dateipfad nicht eindeutig einer Kategorie zugeordnet werden kann.
    /// </summary>
    public static class SpriteContentClassifier
    {
        // Hauttöne: typische RGB-Bereiche für menschliche Haut (RO-Charaktere)
        private static bool IsSkinTone(int r, int g, int b)
        {
            // Einfache Hautton-Erkennung (YCbCr-basiert heuristisch)
            if (r < 95 || g < 40 || b < 20 || r > 250 || g > 220 || b > 200) return false;
            if (Math.Abs(r - g) > 15 && r > g && r > b) return true;
            // YCbCr-Check
            double y = 0.299 * r + 0.587 * g + 0.114 * b;
            double cb = -0.1687 * r - 0.3313 * g + 0.5 * b + 128;
            double cr = 0.5 * r - 0.4187 * g - 0.0813 * b + 128;
            return y > 80 && cb > 77 && cb < 127 && cr > 133 && cr < 173;
        }

        private static bool IsRed(int r, int g, int b) => r > 150 && g < 100 && b < 100;
        private static bool IsBlue(int r, int g, int b) => b > 150 && r < 100 && g < 100;
        private static bool IsGreen(int r, int g, int b) => g > 150 && r < 100 && b < 100;
        private static bool IsDark(int r, int g, int b) => (r + g + b) / 3.0 < 60;
        private static bool IsBright(int r, int g, int b) => (r + g + b) / 3.0 > 200;

        /// <summary>
        /// Analysiert ein Bitmap und schlägt eine Kategorie vor.
        /// </summary>
        public static ContentCategoryResult Classify(Bitmap? bmp)
        {
            if (bmp == null)
                return new ContentCategoryResult { PredictedCategoryId = "other", PredictedCategoryName = "Sonstige", Confidence = 0f };

            int w = bmp.Width, h = bmp.Height;
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] pixels = new int[w * h];
            try { Marshal.Copy(data.Scan0, pixels, 0, pixels.Length); }
            finally { bmp.UnlockBits(data); }

            int skinCount = 0, redCount = 0, blueCount = 0, greenCount = 0;
            int darkCount = 0, brightCount = 0;
            int alphaZeroCount = 0;
            long totalR = 0, totalG = 0, totalB = 0;
            int validPixelCount = 0;

            foreach (int p in pixels)
            {
                int a = (p >> 24) & 0xFF;
                if (a < 10) { alphaZeroCount++; continue; }

                int r = (p >> 16) & 0xFF;
                int g = (p >> 8) & 0xFF;
                int b = p & 0xFF;

                totalR += r; totalG += g; totalB += b;
                validPixelCount++;

                if (IsSkinTone(r, g, b)) skinCount++;
                if (IsRed(r, g, b)) redCount++;
                if (IsBlue(r, g, b)) blueCount++;
                if (IsGreen(r, g, b)) greenCount++;
                if (IsDark(r, g, b)) darkCount++;
                if (IsBright(r, g, b)) brightCount++;
            }

            if (validPixelCount == 0)
                return new ContentCategoryResult { PredictedCategoryId = "other", PredictedCategoryName = "Sonstige", Confidence = 0f };

            float alphaLoss = (float)alphaZeroCount / pixels.Length;

            double avgR = totalR / (double)validPixelCount;
            double avgG = totalG / (double)validPixelCount;
            double avgB = totalB / (double)validPixelCount;

            var features = new List<string>();
            string predictedId = "other";
            string predictedName = "Sonstige";
            float confidence = 0f;

            // --- Entscheidung basierend auf erkannten Features ---

            // 1. Hauttöne stark vertreten → Charakter / Job / NPC
            float skinRatio = (float)skinCount / validPixelCount;
            if (skinRatio > 0.15f)
            {
                features.Add($"Hauttöne ({skinRatio:P0})");
                // Dunkelere Hauttöne + mehr Kontrast → Monster, hellere → Charakter/Job
                if (darkCount > validPixelCount * 0.3f && skinRatio > 0.25f)
                {
                    predictedId = "monsters";
                    predictedName = "Monster";
                    confidence = 0.65f + Math.Min(0.25f, skinRatio);
                }
                else
                {
                    predictedId = "jobs_m";
                    predictedName = "Jobklassen (Männlich)";
                    confidence = 0.6f + Math.Min(0.3f, skinRatio);
                }
            }
            // 2. Viel Rot → Waffe oder Effekt
            else if ((float)redCount / validPixelCount > 0.10f && redCount > blueCount && redCount > greenCount)
            {
                features.Add($"Rotanteil ({(float)redCount / validPixelCount:P0})");
                predictedId = "weapons_shields";
                predictedName = "Waffen & Schilde";
                confidence = 0.55f;
            }
            // 3. Viel Blau → NPC oder Effekt
            else if ((float)blueCount / validPixelCount > 0.10f && blueCount > redCount && blueCount > greenCount)
            {
                features.Add($"Blauanteil ({(float)blueCount / validPixelCount:P0})");
                predictedId = "npc";
                predictedName = "NPCs";
                confidence = 0.55f;
            }
            // 4. Viel Grün → Item, Pflanze, Natur
            else if ((float)greenCount / validPixelCount > 0.10f && greenCount > redCount && greenCount > blueCount)
            {
                features.Add($"Grünanteil ({(float)greenCount / validPixelCount:P0})");
                predictedId = "items";
                predictedName = "Items (Boden-Drops)";
                confidence = 0.55f;
            }
            // 5. Sehr dunkel → Monster oder Schatten-Effekt
            else if ((float)darkCount / validPixelCount > 0.4f)
            {
                features.Add($"Dunkelanteil ({(float)darkCount / validPixelCount:P0})");
                predictedId = "monsters";
                predictedName = "Monster";
                confidence = 0.5f;
            }
            // 6. Sehr hell → Item oder Effekt
            else if ((float)brightCount / validPixelCount > 0.5f)
            {
                features.Add($"Hellanteil ({(float)brightCount / validPixelCount:P0})");
                predictedId = "items";
                predictedName = "Items (Boden-Drops)";
                confidence = 0.5f;
            }

            // Transparenz-Hinweis: viele transparente Pixel deuten auf Objekt/Item hin
            if (alphaLoss > 0.6f)
            {
                features.Add($"Transparenz ({alphaLoss:P0})");
                if (predictedId == "other")
                {
                    predictedId = "items";
                    predictedName = "Items (Boden-Drops)";
                    confidence = 0.45f;
                }
            }

            if (features.Count == 0)
                features.Add("Keine klaren Merkmale erkannt");

            return new ContentCategoryResult
            {
                PredictedCategoryId = predictedId,
                PredictedCategoryName = predictedName,
                Confidence = confidence,
                DetectedFeatures = features
            };
        }
    }
}