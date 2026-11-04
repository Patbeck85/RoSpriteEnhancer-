using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace RoSpriteEnhancer.Core.IO
{
    /// <summary>
    /// Ragnarok Online SPR Datei-Schreiber.
    ///
    /// Schreibt SPR v2.1 Format:
    ///   [0]  byte[2]  Magic "SP"
    ///   [2]  byte     Minor-Version (1)
    ///   [3]  byte     Major-Version (2)
    ///   [4]  uint16   Anzahl indizierte Frames
    ///   [6]  uint16   Anzahl RGBA Frames
    ///
    ///   Indizierte Frames: Width(2) + Height(2) + RLE-Länge(2) + RLE-Daten
    ///   RGBA Frames:       Width(2) + Height(2) + W×H×4 raw BGRA
    ///   Palette:           256 × RGBA (1024 Bytes) — am Ende
    ///
    /// Wichtig: Reihenfolge muss exakt dem Original entsprechen.
    ///   frames[0 .. indexedCount-1]          → Indexed (Palette+RLE)
    ///   frames[indexedCount .. total-1]       → RGBA    (raw BGRA)
    /// </summary>
    public static class SprWriter
    {
        private static readonly byte[] Magic = { 0x53, 0x50 }; // "SP"

        /// <summary>
        /// Schreibt eine SPR-Datei. Die Frames müssen bereits in der korrekten
        /// Reihenfolge vorliegen: zuerst indexedCount indizierte Frames,
        /// dann rgbaCount RGBA-Frames — identisch mit dem Original-SPR.
        /// </summary>
        public static void Write(string outputPath, IReadOnlyList<Bitmap> frames,
            int indexedCount = -1, int rgbaCount = 0)
        {
            if (frames == null || frames.Count == 0)
                throw new ArgumentException("Mindestens ein Frame erforderlich.");

            // Wenn nicht explizit angegeben: alles als indiziert
            if (indexedCount < 0)
            {
                indexedCount = frames.Count;
                rgbaCount    = 0;
            }

            // Validierung: indexedCount + rgbaCount muss frames.Count ergeben
            if (indexedCount + rgbaCount != frames.Count)
            {
                indexedCount = frames.Count;
                rgbaCount    = 0;
            }

            // Palette nur für indizierte Frames berechnen
            var indexedFrames = frames.Take(indexedCount).ToList();
            Color[] palette   = indexedCount > 0
                ? BuildGlobalPalette(indexedFrames)
                : new Color[256];

            // Indizierte Frames quantisieren
            var quantized  = new byte[indexedCount][];
            var idxDims    = new (int w, int h)[indexedCount];
            for (int i = 0; i < indexedCount; i++)
            {
                idxDims[i]   = (frames[i].Width, frames[i].Height);
                quantized[i] = QuantizeFrame(frames[i], palette);
            }

            using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);

            // Header
            bw.Write(Magic);
            bw.Write((byte)1);               // minor
            bw.Write((byte)2);               // major → v2.1
            bw.Write((ushort)indexedCount);
            bw.Write((ushort)rgbaCount);

            // ── INDEXED FRAMES ── Format: Width + Height + RLE-Länge + RLE-Daten
            for (int i = 0; i < indexedCount; i++)
            {
                bw.Write((ushort)idxDims[i].w);
                bw.Write((ushort)idxDims[i].h);
                byte[] rle = EncodeRle(quantized[i]);
                bw.Write((ushort)rle.Length);
                bw.Write(rle);
            }

            // ── RGBA FRAMES ── Format: Width + Height + W×H×4 raw BGRA
            // Diese Frames behalten ihr Originalformat — kein Palette-Verlust
            for (int i = indexedCount; i < frames.Count; i++)
            {
                var bmp = frames[i];
                bw.Write((ushort)bmp.Width);
                bw.Write((ushort)bmp.Height);

                if (bmp.Width > 0 && bmp.Height > 0)
                    WriteRgbaFrame(bw, bmp);
            }

            // Palette am Ende (nur für indizierte Frames relevant)
            WritePalette(bw, palette);
        }

        // ─── GLOBALE PALETTE ─────────────────────────────────────────────────────

        private static Color[] BuildGlobalPalette(IReadOnlyList<Bitmap> frames)
        {
            var colorFreq = new Dictionary<int, int>(4096);

            foreach (var bmp in frames)
            {
                BitmapData data = bmp.LockBits(
                    new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int[] pixels = new int[bmp.Width * bmp.Height];
                try   { Marshal.Copy(data.Scan0, pixels, 0, pixels.Length); }
                finally { bmp.UnlockBits(data); }

                foreach (int p in pixels)
                {
                    if (((p >> 24) & 0xFF) < 128) continue;
                    int rgb = p & 0x00FFFFFF;
                    colorFreq.TryGetValue(rgb, out int cnt);
                    colorFreq[rgb] = cnt + 1;
                }
            }

            List<Color> palette;
            if (colorFreq.Count == 0)
                palette = new List<Color>();
            else if (colorFreq.Count <= 255)
                palette = colorFreq.Keys
                    .Select(rgb => Color.FromArgb(255,
                        (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF))
                    .ToList();
            else
                palette = MedianCut(colorFreq, 255);

            var final = new Color[256];
            final[0] = Color.FromArgb(0, 0, 0, 0); // Slot 0 = transparent
            for (int i = 0; i < palette.Count && i < 255; i++)
                final[i + 1] = palette[i];
            for (int i = palette.Count + 1; i < 256; i++)
                final[i] = Color.Black;

            return final;
        }

        // ─── FRAME QUANTISIEREN (mit Floyd-Steinberg-Dithering) ─────────────────
        //
        // Ohne Dithering springt jeder Pixel zur nächsten Palettenfarbe — bei nur
        // 255 Farben führt das bei Verläufen (Haut, Rüstungs-Schattierungen) zu
        // sichtbaren Bändern/Stufen. Floyd-Steinberg verteilt den Rundungsfehler
        // jedes Pixels auf die Nachbarn, wodurch der Verlauf im Mittel erhalten
        // bleibt, auch wenn die Palette begrenzt ist.
        //
        // Fehler wird NICHT über transparente Pixel hinweg verteilt (kein
        // Farb-Bleeding in den Hintergrund).

        private static byte[] QuantizeFrame(Bitmap bmp, Color[] palette)
        {
            int w = bmp.Width, h = bmp.Height;

            BitmapData data = bmp.LockBits(
                new Rectangle(0, 0, w, h),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] pixels = new int[w * h];
            try   { Marshal.Copy(data.Scan0, pixels, 0, pixels.Length); }
            finally { bmp.UnlockBits(data); }

            byte[] result = new byte[w * h];

            // Fehler-Puffer pro Kanal (float für Präzision über viele Diffusionsschritte)
            float[] errR = new float[w * h];
            float[] errG = new float[w * h];
            float[] errB = new float[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    int p = pixels[i];

                    if (((p >> 24) & 0xFF) < 128)
                    {
                        result[i] = 0;
                        continue; // transparent — kein Fehler-Diffusion
                    }

                    float r = Math.Clamp(((p >> 16) & 0xFF) + errR[i], 0, 255);
                    float g = Math.Clamp(((p >>  8) & 0xFF) + errG[i], 0, 255);
                    float b = Math.Clamp(( p        & 0xFF) + errB[i], 0, 255);

                    int bestIdx = FindNearestPaletteIndex(palette, r, g, b);
                    result[i] = (byte)bestIdx;

                    Color chosen = palette[bestIdx];
                    float er = r - chosen.R;
                    float eg = g - chosen.G;
                    float eb = b - chosen.B;

                    // Floyd-Steinberg-Gewichte: rechts 7/16, unten-links 3/16,
                    // unten 5/16, unten-rechts 1/16 — nur auf sichtbare Zielpixel
                    if (x + 1 < w && ((pixels[i + 1] >> 24) & 0xFF) >= 128)
                    { errR[i+1] += er * 7f/16f; errG[i+1] += eg * 7f/16f; errB[i+1] += eb * 7f/16f; }

                    if (y + 1 < h)
                    {
                        int rowBelow = i + w;
                        if (x > 0 && ((pixels[rowBelow - 1] >> 24) & 0xFF) >= 128)
                        { errR[rowBelow-1] += er * 3f/16f; errG[rowBelow-1] += eg * 3f/16f; errB[rowBelow-1] += eb * 3f/16f; }

                        if (((pixels[rowBelow] >> 24) & 0xFF) >= 128)
                        { errR[rowBelow] += er * 5f/16f; errG[rowBelow] += eg * 5f/16f; errB[rowBelow] += eb * 5f/16f; }

                        if (x + 1 < w && ((pixels[rowBelow + 1] >> 24) & 0xFF) >= 128)
                        { errR[rowBelow+1] += er * 1f/16f; errG[rowBelow+1] += eg * 1f/16f; errB[rowBelow+1] += eb * 1f/16f; }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Findet den nächsten Palette-Index (Slot 0 = transparent, wird übersprungen).
        /// Direkte Suche statt Lookup-Tabelle — nötig weil Dithering pro Pixel
        /// leicht unterschiedliche (fehler-korrigierte) RGB-Werte hat.
        /// </summary>
        private static int FindNearestPaletteIndex(Color[] palette, float r, float g, float b)
        {
            int bestIdx = 1;
            float bestDist = float.MaxValue;
            for (int i = 1; i < palette.Length; i++)
            {
                Color c = palette[i];
                if (c.A == 0) continue;
                float dr = c.R - r, dg = c.G - g, db = c.B - b;
                // Gewichtete Distanz: Grün > Rot > Blau (menschliche Wahrnehmung)
                float dist = dr*dr*2 + dg*dg*4 + db*db;
                if (dist < bestDist) { bestDist = dist; bestIdx = i; }
            }
            return bestIdx;
        }

        // ─── RLE ENCODING ────────────────────────────────────────────────────────

        private static byte[] EncodeRle(byte[] indices)
        {
            using var ms = new MemoryStream(indices.Length);
            int i = 0;
            while (i < indices.Length)
            {
                if (indices[i] != 0)
                {
                    ms.WriteByte(indices[i++]);
                }
                else
                {
                    // Transparenz-Lauf zählen
                    int count = 0;
                    while (i < indices.Length && indices[i] == 0) { count++; i++; }

                    // In Pakete von max. 255 aufteilen
                    while (count > 0)
                    {
                        if (count == 1)
                        {
                            ms.WriteByte(0); ms.WriteByte(0); // Einzelner transparenter Pixel
                            count--;
                        }
                        else
                        {
                            int chunk = Math.Min(count, 255);
                            ms.WriteByte(0); ms.WriteByte((byte)chunk);
                            count -= chunk;
                        }
                    }
                }
            }
            return ms.ToArray();
        }

        // ─── RGBA FRAME SCHREIBEN ────────────────────────────────────────────────
        // RGBA Frames werden als raw BGRA-Daten geschrieben — kein Palette, kein RLE.
        // Das entspricht exakt dem Format das der RO-Client erwartet.

        private static void WriteRgbaFrame(BinaryWriter bw, Bitmap bmp)
        {
            int w = bmp.Width, h = bmp.Height;
            BitmapData data = bmp.LockBits(
                new Rectangle(0, 0, w, h),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] pixels = new int[w * h];
            try   { Marshal.Copy(data.Scan0, pixels, 0, pixels.Length); }
            finally { bmp.UnlockBits(data); }

            // ARGB → BGRA (RO-Format)
            byte[] bgra = new byte[pixels.Length * 4];
            for (int i = 0; i < pixels.Length; i++)
            {
                int p = pixels[i];
                bgra[i*4]     = (byte)( p        & 0xFF); // B
                bgra[i*4 + 1] = (byte)((p >>  8) & 0xFF); // G
                bgra[i*4 + 2] = (byte)((p >> 16) & 0xFF); // R
                bgra[i*4 + 3] = (byte)((p >> 24) & 0xFF); // A
            }
            bw.Write(bgra);
        }

        // ─── PALETTE SCHREIBEN ───────────────────────────────────────────────────

        private static void WritePalette(BinaryWriter bw, Color[] palette)
        {
            for (int i = 0; i < 256; i++)
            {
                Color c = i < palette.Length ? palette[i] : Color.Black;
                bw.Write(c.R);
                bw.Write(c.G);
                bw.Write(c.B);
                // Slot 0 = transparent (Alpha 0), alle anderen Alpha 255
                bw.Write(i == 0 ? (byte)0 : (byte)255);
            }
        }

        // ─── MEDIAN-CUT ──────────────────────────────────────────────────────────

        private static List<Color> MedianCut(Dictionary<int, int> colorFreq, int maxColors)
        {
            var colors = colorFreq.Keys
                .Select(rgb => new SprRgbColor(
                    (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF,
                    colorFreq[rgb]))
                .ToList();

            var buckets = new List<List<SprRgbColor>> { colors };

            while (buckets.Count < maxColors)
            {
                int splitIdx = FindLargestBucket(buckets);
                var bucket   = buckets[splitIdx];
                if (bucket.Count <= 1) break;

                int rMin=255,rMax=0,gMin=255,gMax=0,bMin=255,bMax=0;
                foreach (var c in bucket)
                {
                    if(c.R<rMin)rMin=c.R; if(c.R>rMax)rMax=c.R;
                    if(c.G<gMin)gMin=c.G; if(c.G>gMax)gMax=c.G;
                    if(c.B<bMin)bMin=c.B; if(c.B>bMax)bMax=c.B;
                }

                if (rMax-rMin >= gMax-gMin && rMax-rMin >= bMax-bMin)
                    bucket.Sort((a,b) => a.R.CompareTo(b.R));
                else if (gMax-gMin >= bMax-bMin)
                    bucket.Sort((a,b) => a.G.CompareTo(b.G));
                else
                    bucket.Sort((a,b) => a.B.CompareTo(b.B));

                int mid   = bucket.Count / 2;
                var left  = bucket.GetRange(0, mid);
                var right = bucket.GetRange(mid, bucket.Count - mid);
                buckets.RemoveAt(splitIdx);
                buckets.Add(left);
                buckets.Add(right);
            }

            var result = new List<Color>(buckets.Count);
            foreach (var bucket in buckets)
            {
                if (bucket.Count == 0) continue;
                long rS=0,gS=0,bS=0,tot=0;
                foreach (var c in bucket)
                {
                    rS += (long)c.R * c.Freq;
                    gS += (long)c.G * c.Freq;
                    bS += (long)c.B * c.Freq;
                    tot += c.Freq;
                }
                if (tot > 0)
                    result.Add(Color.FromArgb(255,
                        (int)(rS/tot), (int)(gS/tot), (int)(bS/tot)));
            }
            return result;
        }

        private static int FindLargestBucket(List<List<SprRgbColor>> buckets)
        {
            int best=0, bestRange=-1;
            for (int i=0; i<buckets.Count; i++)
            {
                var b = buckets[i];
                if (b.Count <= 1) continue;
                int rMin=255,rMax=0,gMin=255,gMax=0,bMin=255,bMax=0;
                foreach(var c in b)
                {
                    if(c.R<rMin)rMin=c.R; if(c.R>rMax)rMax=c.R;
                    if(c.G<gMin)gMin=c.G; if(c.G>gMax)gMax=c.G;
                    if(c.B<bMin)bMin=c.B; if(c.B>bMax)bMax=c.B;
                }
                int range = Math.Max(rMax-rMin, Math.Max(gMax-gMin, bMax-bMin));
                if (range > bestRange) { bestRange=range; best=i; }
            }
            return best;
        }

        private readonly struct SprRgbColor
        {
            public int R { get; }
            public int G { get; }
            public int B { get; }
            public int Freq { get; }
            public SprRgbColor(int r, int g, int b, int freq)
            { R=r; G=g; B=b; Freq=freq; }
        }
    }
}
