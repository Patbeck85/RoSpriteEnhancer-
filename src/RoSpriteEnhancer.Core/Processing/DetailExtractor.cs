using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RoSpriteEnhancer.Core.Processing
{
    /// <summary>
    /// TASK-1401 / TASK-1402: Detail-Extraktion & Injektion für Anime-HD-Upscale.
    ///
    /// Vor dem Upscaling werden feine Linien und Texturen (hochfrequente Komponenten)
    /// über einen Laplacian/High-Pass-Filter extrahiert und als "Detail-Karte" gespeichert.
    /// Nach dem Downscale werden diese Details über das Ergebnis gelegt, sodass
    /// Kanten, Haare, Ornamente und kleine Strukturen nicht verschluckt werden.
    /// </summary>
    public static class DetailExtractor
    {
        // Laplacian 3×3 Kernel für Detail-Extraktion (betont Kanten)
        private static readonly int[] LaplacianKernel = { 0, -1, 0, -1, 4, -1, 0, -1, 0 };

        /// <summary>
        /// Extrahiert eine Detail-Karte aus dem Quell-Bitmap.
        ///
        /// Die Karte enthält nur die hochfrequenten Unterschiede (Kanten, Texturen),
        /// skaliert auf einen Bereich von 0..255 pro Kanal (128 = neutral).
        /// </summary>
        public static Bitmap ExtractDetailMap(Bitmap src)
        {
            int w = src.Width;
            int h = src.Height;

            BitmapData data = src.LockBits(new Rectangle(0, 0, w, h),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] pixels = new int[w * h];
            try { Marshal.Copy(data.Scan0, pixels, 0, pixels.Length); }
            finally { src.UnlockBits(data); }

            int[] detail = new int[w * h];

            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    int idx = y * w + x;
                    int center = pixels[idx];

                    int ca = (center >> 24) & 0xFF;
                    int cr = (center >> 16) & 0xFF;
                    int cg = (center >> 8) & 0xFF;
                    int cb = center & 0xFF;

                    int da = 0, dr = 0, dg = 0, db = 0;

                    // Laplacian: 4*center - sum(neighbors)
                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            int n = pixels[(y + ky) * w + (x + kx)];
                            int wgt = (kx == 0 && ky == 0) ? 4 : -1;

                            int na = (n >> 24) & 0xFF;
                            int nr = (n >> 16) & 0xFF;
                            int ng = (n >> 8) & 0xFF;
                            int nb = n & 0xFF;

                            da += (ca - na) * wgt;
                            dr += (cr - nr) * wgt;
                            dg += (cg - ng) * wgt;
                            db += (cb - nb) * wgt;
                        }
                    }

                    // Normalisiere auf 0..255 (Detail-Stärke, 128 = neutral)
                    int a = Clamp(128 + da / 2);
                    int r = Clamp(128 + dr / 2);
                    int g = Clamp(128 + dg / 2);
                    int b = Clamp(128 + db / 2);

                    detail[idx] = (a << 24) | (r << 16) | (g << 8) | b;
                }
            }

            Bitmap result = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            BitmapData outData = result.LockBits(new Rectangle(0, 0, w, h),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try { Marshal.Copy(detail, 0, outData.Scan0, detail.Length); }
            finally { result.UnlockBits(outData); }

            return result;
        }

        /// Mischt die Detail-Karte über das Ziel-Bitmap.
        /// factor: 0.0 = kein Detail, 1.0 = volle Detail-Stärke.
        public static Bitmap InjectDetail(Bitmap target, Bitmap detailMap, float factor)
        {
            if (detailMap == null || factor <= 0f) return target;

            int w = target.Width;
            int h = target.Height;

            BitmapData td = target.LockBits(new Rectangle(0, 0, w, h),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int[] tpix = new int[w * h];
            try { Marshal.Copy(td.Scan0, tpix, 0, tpix.Length); }
            finally { target.UnlockBits(td); }

            BitmapData dd = detailMap.LockBits(new Rectangle(0, 0, detailMap.Width, detailMap.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] dpix = new int[detailMap.Width * detailMap.Height];
            try { Marshal.Copy(dd.Scan0, dpix, 0, dpix.Length); }
            finally { detailMap.UnlockBits(dd); }

            int minW = Math.Min(w, detailMap.Width);
            int minH = Math.Min(h, detailMap.Height);

            for (int y = 0; y < minH; y++)
            {
                for (int x = 0; x < minW; x++)
                {
                    int idx = y * w + x;
                    int t = tpix[idx];
                    int d = dpix[y * detailMap.Width + x];

                    int ta = (t >> 24) & 0xFF;
                    int tr = (t >> 16) & 0xFF;
                    int tg = (t >> 8) & 0xFF;
                    int tb = t & 0xFF;

                    int da = (d >> 24) & 0xFF;
                    int dr = (d >> 16) & 0xFF;
                    int dg = (d >> 8) & 0xFF;
                    int db = d & 0xFF;

                    // Detail-Karte ist um 128 offsetiert (128 = neutral)
                    float f = factor;
                    int na = Clamp((int)(ta + (da - 128) * f * 0.6f));
                    int nr = Clamp((int)(tr + (dr - 128) * f * 0.8f));
                    int ng = Clamp((int)(tg + (dg - 128) * f * 0.8f));
                    int nb = Clamp((int)(tb + (db - 128) * f * 0.8f));

                    tpix[idx] = (na << 24) | (nr << 16) | (ng << 8) | nb;
                }
            }

            Bitmap result = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            BitmapData rd = result.LockBits(new Rectangle(0, 0, w, h),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try { Marshal.Copy(tpix, 0, rd.Scan0, tpix.Length); }
            finally { result.UnlockBits(rd); }

            return result;
        }

        /// <summary>
        /// Bestehende Apply-Methode: High-Frequency Local Contrast Boost direkt auf dem Bild.
        /// detailStrength: 0.0 = kein Effekt, 0.35 = Standard, 1.0 = stark.
        /// </summary>
        public static Bitmap Apply(Bitmap src, float detailStrength = 0.35f)
        {
            int w = src.Width, h = src.Height;
            BitmapData data = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] pixels = new int[w * h];
            try
            {
                unsafe
                {
                    byte* ptr = (byte*)data.Scan0;
                    for (int y = 0; y < h; y++)
                        Marshal.Copy((IntPtr)(ptr + y * data.Stride), pixels, y * w, w);
                }
            }
            finally { src.UnlockBits(data); }

            int[] result = (int[])pixels.Clone();

            // High-Frequency Local Contrast Boost (Falten, Fugen & Schmuck herausarbeiten)
            Parallel.For(1, h - 1, y =>
            {
                for (int x = 1; x < w - 1; x++)
                {
                    int idx = y * w + x;
                    int centerP = pixels[idx];
                    int alpha = (centerP >> 24) & 0xFF;
                    if (alpha < 10) continue;

                    int cr = (centerP >> 16) & 0xFF, cg = (centerP >> 8) & 0xFF, cb = centerP & 0xFF;

                    // 3x3 Laplace High-Pass Filter
                    int sumR = 0, sumG = 0, sumB = 0;
                    int[] weights = { 0, -1, 0, -1, 4, -1, 0, -1, 0 };
                    int ki = 0;

                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            int np = pixels[(y + ky) * w + (x + kx)];
                            int na = (np >> 24) & 0xFF;
                            if (na < 10) np = centerP;

                            sumR += ((np >> 16) & 0xFF) * weights[ki];
                            sumG += ((np >> 8) & 0xFF) * weights[ki];
                            sumB += (np & 0xFF) * weights[ki];
                            ki++;
                        }
                    }

                    int hpDiff = (Math.Abs(sumR) + Math.Abs(sumG) + Math.Abs(sumB)) / 3;
                    if (hpDiff > 12 && hpDiff < 180)
                    {
                        int nr = Math.Clamp((int)(cr + sumR * detailStrength), 0, 255);
                        int ng = Math.Clamp((int)(cg + sumG * detailStrength), 0, 255);
                        int nb = Math.Clamp((int)(cb + sumB * detailStrength), 0, 255);
                        result[idx] = (alpha << 24) | (nr << 16) | (ng << 8) | nb;
                    }
                }
            });

            Bitmap dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            BitmapData dstData = dst.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    byte* ptrDst = (byte*)dstData.Scan0;
                    for (int y = 0; y < h; y++)
                        Marshal.Copy(result, y * w, (IntPtr)(ptrDst + y * dstData.Stride), w);
                }
            }
            finally { dst.UnlockBits(dstData); }

            return dst;
        }

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;
    }
}
