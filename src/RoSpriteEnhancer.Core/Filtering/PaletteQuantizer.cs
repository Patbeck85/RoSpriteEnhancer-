using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RoSpriteEnhancer.Core.Filtering
{
    /// <summary>
    /// Farbquantisierer: Reduziert ein 32-bit ARGB Bild auf 8-bit indiziert mit bis zu 256 Farben.
    ///
    /// Algorithmus: Median-Cut (populärster Algorithmus für Palette-Quantisierung)
    ///   1. Alle einzigartigen Farben (alpha >= threshold) werden gesammelt
    ///   2. Farbraum wird rekursiv am Median der größten Achse geteilt
    ///   3. Jedes resultierende "Bucket" wird auf seinen Mittelwert reduziert
    ///   4. Slot 0 = Magenta (#FF00FF) reserviert für RO-Transparenz
    ///
    /// Vorheriger Bug: Octree nutzte nur 1 Ebene → max 8 Farben statt 256.
    /// Jetzt: Median-Cut → echte 255 Farben + 1 Magenta = 256 Paletten-Einträge.
    /// </summary>
    public static class PaletteQuantizer
    {
        private const int MaxColors = 255; // Slot 0 reserviert für Magenta

        public static Bitmap Quantize(Bitmap src, int alphaThreshold = 128)
        {
            int w = src.Width;
            int h = src.Height;

            // 1. Lese alle Pixel
            BitmapData srcData = src.LockBits(new Rectangle(0, 0, w, h),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] pixels = new int[w * h];
            try
            {
                Marshal.Copy(srcData.Scan0, pixels, 0, pixels.Length);
            }
            finally
            {
                src.UnlockBits(srcData);
            }

            // 2. Sammle einzigartige sichtbare Farben mit Häufigkeit
            var colorFreq = new Dictionary<int, int>(pixels.Length / 2);
            foreach (int p in pixels)
            {
                int a = (p >> 24) & 0xFF;
                if (a < alphaThreshold) continue;
                int rgb = p & 0x00FFFFFF;
                colorFreq.TryGetValue(rgb, out int cnt);
                colorFreq[rgb] = cnt + 1;
            }

            // 3. Median-Cut Quantisierung
            List<Color> palette;
            if (colorFreq.Count <= MaxColors)
            {
                // Weniger einzigartige Farben als Max — direkt verwenden
                palette = colorFreq.Keys
                    .Select(rgb => Color.FromArgb(
                        (rgb >> 16) & 0xFF,
                        (rgb >>  8) & 0xFF,
                         rgb        & 0xFF))
                    .ToList();
            }
            else
            {
                // Median-Cut
                var initialBucket = colorFreq.Keys
                    .Select(rgb => new RgbColor(
                        (rgb >> 16) & 0xFF,
                        (rgb >>  8) & 0xFF,
                         rgb        & 0xFF,
                        colorFreq[rgb]))
                    .ToList();

                palette = MedianCut(initialBucket, MaxColors);
            }

            // 4. Erstelle finale 256-Farben-Palette
            // Slot 0 = Magenta (Transparenz-Marker)
            var finalPalette = new Color[256];
            finalPalette[0] = Color.FromArgb(255, 255, 0, 255);
            for (int i = 0; i < palette.Count && i < 255; i++)
                finalPalette[i + 1] = Color.FromArgb(255, palette[i].R, palette[i].G, palette[i].B);
            for (int i = palette.Count + 1; i < 256; i++)
                finalPalette[i] = Color.Black;

            // 5. Floyd-Steinberg-Dithering: verteilt Rundungsfehler auf Nachbarn,
            //    verhindert sichtbare Farbbänder bei nur 255 Farben (siehe SprWriter
            //    für ausführliche Erklärung — identischer Ansatz).
            byte[] indices = new byte[w * h];
            float[] errR = new float[w * h];
            float[] errG = new float[w * h];
            float[] errB = new float[w * h];

            bool hasColors = palette.Count > 0;

            if (hasColors)
            {
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int i = y * w + x;
                        int p = pixels[i];
                        int a = (p >> 24) & 0xFF;
                        if (a < alphaThreshold) { indices[i] = 0; continue; }

                        float r = Math.Clamp(((p >> 16) & 0xFF) + errR[i], 0, 255);
                        float g = Math.Clamp(((p >>  8) & 0xFF) + errG[i], 0, 255);
                        float b = Math.Clamp(( p        & 0xFF) + errB[i], 0, 255);

                        int bestIdx = FindNearestPaletteIndex(finalPalette, r, g, b);
                        indices[i] = (byte)bestIdx;

                        Color chosen = finalPalette[bestIdx];
                        float er = r - chosen.R, eg = g - chosen.G, eb = b - chosen.B;

                        if (x + 1 < w && ((pixels[i + 1] >> 24) & 0xFF) >= alphaThreshold)
                        { errR[i+1] += er*7f/16f; errG[i+1] += eg*7f/16f; errB[i+1] += eb*7f/16f; }
                        if (y + 1 < h)
                        {
                            int below = i + w;
                            if (x > 0 && ((pixels[below - 1] >> 24) & 0xFF) >= alphaThreshold)
                            { errR[below-1] += er*3f/16f; errG[below-1] += eg*3f/16f; errB[below-1] += eb*3f/16f; }
                            if (((pixels[below] >> 24) & 0xFF) >= alphaThreshold)
                            { errR[below] += er*5f/16f; errG[below] += eg*5f/16f; errB[below] += eb*5f/16f; }
                            if (x + 1 < w && ((pixels[below + 1] >> 24) & 0xFF) >= alphaThreshold)
                            { errR[below+1] += er*1f/16f; errG[below+1] += eg*1f/16f; errB[below+1] += eb*1f/16f; }
                        }
                    }
                }
            }

            // 6. Erstelle 8-bit Bitmap
            Bitmap dst = new Bitmap(w, h, PixelFormat.Format8bppIndexed);
            ColorPalette dstPalette = dst.Palette;
            for (int i = 0; i < 256; i++)
                dstPalette.Entries[i] = finalPalette[i];
            dst.Palette = dstPalette;

            BitmapData dstData = dst.LockBits(new Rectangle(0, 0, w, h),
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            try
            {
                unsafe
                {
                    for (int y = 0; y < h; y++)
                    {
                        byte* row = (byte*)dstData.Scan0 + y * dstData.Stride;
                        for (int x = 0; x < w; x++)
                            row[x] = indices[y * w + x];
                    }
                }
            }
            finally
            {
                dst.UnlockBits(dstData);
            }

            return dst;
        }

        /// <summary>
        /// Findet den nächsten Palette-Index (Slot 0 = Magenta/Transparenz, wird übersprungen).
        /// </summary>
        private static int FindNearestPaletteIndex(Color[] palette, float r, float g, float b)
        {
            int bestIdx = 1;
            float bestDist = float.MaxValue;
            for (int i = 1; i < 256; i++)
            {
                Color c = palette[i];
                if (c.A == 0) continue;
                float dr = c.R - r, dg = c.G - g, db = c.B - b;
                float dist = dr*dr*2 + dg*dg*4 + db*db;
                if (dist < bestDist) { bestDist = dist; bestIdx = i; }
            }
            return bestIdx;
        }

        // ─── MEDIAN CUT ──────────────────────────────────────────────────────────

        private static List<Color> MedianCut(List<RgbColor> colors, int targetCount)
        {
            // Starte mit einem Bucket aller Farben
            var buckets = new List<List<RgbColor>> { colors };

            // Teile solange bis targetCount Buckets erreicht
            while (buckets.Count < targetCount)
            {
                // Finde Bucket mit größtem Farbbereich
                int splitIdx = FindLargestBucket(buckets);
                var bucket = buckets[splitIdx];

                if (bucket.Count <= 1) break;

                // Bestimme welche Achse den größten Range hat
                int rMin = 255, rMax = 0;
                int gMin = 255, gMax = 0;
                int bMin = 255, bMax = 0;
                foreach (var c in bucket)
                {
                    if (c.R < rMin) rMin = c.R; if (c.R > rMax) rMax = c.R;
                    if (c.G < gMin) gMin = c.G; if (c.G > gMax) gMax = c.G;
                    if (c.B < bMin) bMin = c.B; if (c.B > bMax) bMax = c.B;
                }

                int rRange = rMax - rMin;
                int gRange = gMax - gMin;
                int bRange = bMax - bMin;

                // Sortiere nach der Achse mit dem größten Range
                if (rRange >= gRange && rRange >= bRange)
                    bucket.Sort((a, b) => a.R.CompareTo(b.R));
                else if (gRange >= rRange && gRange >= bRange)
                    bucket.Sort((a, b) => a.G.CompareTo(b.G));
                else
                    bucket.Sort((a, b) => a.B.CompareTo(b.B));

                // Teile am Median
                int mid = bucket.Count / 2;
                var left  = bucket.GetRange(0, mid);
                var right = bucket.GetRange(mid, bucket.Count - mid);

                buckets.RemoveAt(splitIdx);
                buckets.Add(left);
                buckets.Add(right);
            }

            // Berechne Mittelwert für jeden Bucket (gewichtet nach Häufigkeit)
            var result = new List<Color>(buckets.Count);
            foreach (var bucket in buckets)
            {
                if (bucket.Count == 0) continue;
                long rSum = 0, gSum = 0, bSum = 0, total = 0;
                foreach (var c in bucket)
                {
                    rSum  += (long)c.R * c.Freq;
                    gSum  += (long)c.G * c.Freq;
                    bSum  += (long)c.B * c.Freq;
                    total += c.Freq;
                }
                if (total == 0) continue;
                result.Add(Color.FromArgb(
                    (int)(rSum / total),
                    (int)(gSum / total),
                    (int)(bSum / total)));
            }
            return result;
        }

        private static int FindLargestBucket(List<List<RgbColor>> buckets)
        {
            int best = 0;
            int bestRange = -1;
            for (int i = 0; i < buckets.Count; i++)
            {
                var b = buckets[i];
                if (b.Count <= 1) continue;

                int rMin = 255, rMax = 0, gMin = 255, gMax = 0, bMin = 255, bMax = 0;
                foreach (var c in b)
                {
                    if (c.R < rMin) rMin = c.R; if (c.R > rMax) rMax = c.R;
                    if (c.G < gMin) gMin = c.G; if (c.G > gMax) gMax = c.G;
                    if (c.B < bMin) bMin = c.B; if (c.B > bMax) bMax = c.B;
                }
                int range = Math.Max(rMax - rMin, Math.Max(gMax - gMin, bMax - bMin));
                if (range > bestRange) { bestRange = range; best = i; }
            }
            return best;
        }
    }

    // ─── HELPER STRUCT ───────────────────────────────────────────────────────────

    internal readonly struct RgbColor
    {
        public int R { get; }
        public int G { get; }
        public int B { get; }
        public int Freq { get; }

        public RgbColor(int r, int g, int b, int freq)
        {
            R = r; G = g; B = b; Freq = freq;
        }
    }
}
