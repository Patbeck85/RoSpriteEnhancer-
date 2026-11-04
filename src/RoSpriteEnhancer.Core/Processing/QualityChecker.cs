using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RoSpriteEnhancer.Core.Processing
{
    /// <summary>
    /// Ergebnis des automatischen Qualitäts-Checks nach der Bildverarbeitung.
    /// </summary>
    public record QualityReport
    {
        public string FileName { get; init; } = "";
        public bool IsBlurry { get; init; }
        public bool TransparencyLost { get; init; }
        public bool ColorShift { get; init; }
        public double OverallScore { get; init; } // 0.0 = schlecht, 1.0 = perfekt
        public List<string> Issues { get; init; } = new();
    }

    /// <summary>
    /// Automatischer Qualitäts-Check: Vergleicht Original und verarbeitetes Bild
    /// auf Schärfeverlust, Transparenzverlust und Farbverschiebung.
    /// </summary>
    public static class QualityChecker
    {
        // Schwellenwerte
        private const double BlurryThreshold = 0.45;   // Laplacian-Varianz-Ratio unter diesem Wert = verschwommen
        private const double TransparencyLossThreshold = 0.10; // >10% Alpha-Verlust = Problem
        private const double ColorShiftThreshold = 18.0;       // Durchschnittliche Farbdifferenz über diesem Wert = Problem

        /// <summary>
        /// Prüft die Qualität des verarbeiteten Bildes im Vergleich zum Original.
        /// </summary>
        public static QualityReport CheckQuality(Bitmap original, Bitmap processed, string fileName)
        {
            var issues = new List<string>();
            double score = 1.0;

            // 1. Schärfe-Check (Laplacian-Varianz)
            double origSharpness = ComputeLaplacianVariance(original);
            double procSharpness = ComputeLaplacianVariance(processed);
            double sharpnessRatio = origSharpness > 0 ? procSharpness / origSharpness : 1.0;

            bool isBlurry = sharpnessRatio < BlurryThreshold;
            if (isBlurry)
            {
                issues.Add($"Schärfeverlust erkannt (Ratio: {sharpnessRatio:P0}) – Bild könnte zu weich sein.");
                score -= 0.3;
            }

            // 2. Transparenz-Check
            double origTrans = ComputeTransparencyRatio(original);
            double procTrans = ComputeTransparencyRatio(processed);
            double transLoss = origTrans > 0 ? (origTrans - procTrans) / origTrans : 0.0;

            bool transparencyLost = transLoss > TransparencyLossThreshold;
            if (transparencyLost)
            {
                issues.Add($"Transparenzverlust erkannt ({transLoss:P0} weniger Alpha-Pixel) – Prüfen Sie Colorkey/Alpha-Einstellungen.");
                score -= 0.3;
            }

            // 3. Farbverschiebung-Check
            double colorDiff = ComputeAverageColorDistance(original, processed);
            bool colorShifted = colorDiff > ColorShiftThreshold;
            if (colorShifted)
            {
                issues.Add($"Farbverschiebung erkannt (ø ΔE: {colorDiff:F1}) – Prüfen Sie HSL/Shading-Einstellungen.");
                score -= 0.2;
            }

            score = Math.Clamp(score, 0.0, 1.0);

            return new QualityReport
            {
                FileName = fileName,
                IsBlurry = isBlurry,
                TransparencyLost = transparencyLost,
                ColorShift = colorShifted,
                OverallScore = score,
                Issues = issues
            };
        }

        /// <summary>
        /// Berechnet die Laplacian-Varianz als Maß für die Schärfe (höher = schärfer).
        /// </summary>
        private static double ComputeLaplacianVariance(Bitmap bmp)
        {
            int w = bmp.Width, h = bmp.Height;
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] pixels = new int[w * h];
            try { Marshal.Copy(data.Scan0, pixels, 0, pixels.Length); }
            finally { bmp.UnlockBits(data); }

            // Graustufen-Laplacian
            double[] laplacian = new double[w * h];
            int count = 0;
            double sum = 0;

            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    int idx = y * w + x;
                    int center = pixels[idx];
                    int ca = (center >> 24) & 0xFF;
                    if (ca < 10) continue; // Transparente Pixel ignorieren

                    int cr = (center >> 16) & 0xFF;
                    int cg = (center >> 8) & 0xFF;
                    int cb = center & 0xFF;
                    int gray = (cr + cg + cb) / 3;

                    int lap = 0;
                    // 3x3 Laplacian Kernel: 0,-1,0 / -1,4,-1 / 0,-1,0
                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            int n = pixels[(y + ky) * w + (x + kx)];
                            int na = (n >> 24) & 0xFF;
                            if (na < 10) n = center;
                            int ng = ((n >> 16) & 0xFF + (n >> 8) & 0xFF + (n & 0xFF)) / 3;
                            int wgt = (kx == 0 && ky == 0) ? 4 : -1;
                            lap += ng * wgt;
                        }
                    }

                    laplacian[count++] = lap;
                    sum += lap;
                }
            }

            if (count == 0) return 0;

            double mean = sum / count;
            double variance = 0;
            for (int i = 0; i < count; i++)
            {
                double d = laplacian[i] - mean;
                variance += d * d;
            }
            return variance / count;
        }

        /// <summary>
        /// Berechnet den Anteil an Pixeln mit Alpha < 128 (transparente Pixel).
        /// </summary>
        private static double ComputeTransparencyRatio(Bitmap bmp)
        {
            int w = bmp.Width, h = bmp.Height;
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] pixels = new int[w * h];
            try { Marshal.Copy(data.Scan0, pixels, 0, pixels.Length); }
            finally { bmp.UnlockBits(data); }

            int transparent = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (((pixels[i] >> 24) & 0xFF) < 128) transparent++;
            }
            return (double)transparent / pixels.Length;
        }

        /// <summary>
        /// Berechnet die durchschnittliche absolute Farbdifferenz pro Pixel (RGB).
        /// </summary>
        public static double ComputeAverageColorDistance(Bitmap original, Bitmap processed)
        {
            int w = Math.Min(original.Width, processed.Width);
            int h = Math.Min(original.Height, processed.Height);

            BitmapData od = original.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] opix = new int[w * h];
            try { Marshal.Copy(od.Scan0, opix, 0, opix.Length); }
            finally { original.UnlockBits(od); }

            BitmapData pd = processed.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] ppix = new int[w * h];
            try { Marshal.Copy(pd.Scan0, ppix, 0, ppix.Length); }
            finally { processed.UnlockBits(pd); }

            long totalDiff = 0;
            int count = 0;

            for (int i = 0; i < opix.Length; i++)
            {
                int oa = (opix[i] >> 24) & 0xFF;
                int pa = (ppix[i] >> 24) & 0xFF;
                if (oa < 10 && pa < 10) continue; // Beide transparent – überspringen

                int or_ = (opix[i] >> 16) & 0xFF;
                int og = (opix[i] >> 8) & 0xFF;
                int ob = opix[i] & 0xFF;

                int pr_ = (ppix[i] >> 16) & 0xFF;
                int pg = (ppix[i] >> 8) & 0xFF;
                int pb = ppix[i] & 0xFF;

                totalDiff += Math.Abs(or_ - pr_) + Math.Abs(og - pg) + Math.Abs(ob - pb);
                count++;
            }

            return count > 0 ? (double)totalDiff / (count * 3) : 0.0;
        }
    }
}