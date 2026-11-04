using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RoSpriteEnhancer.Core.Filtering
{
    /// <summary>
    /// Super-Sampling Anti-Aliasing (SSAA) für RO-Sprites.
    ///
    /// Technik (wird von vielen HD-Sprite-Projekten verwendet):
    ///
    ///   Schritt 1: Original auf 4x hochskalieren (Nearest-Neighbor — kein Blur)
    ///   Schritt 2: Pixel-Shading bei hoher Auflösung anwenden (Outline, Shadow, etc.)
    ///   Schritt 3: Mit hochwertigem Filter auf Zielgröße runterskalieren
    ///
    ///   Ergebnis:
    ///     - Kanten werden beim Runterskalieren natürlich geglättet (Anti-Aliasing)
    ///     - Keine Pixel-Art-Treppchen mehr
    ///     - Subtile Details bleiben erhalten
    ///     - Wirkt wie "höhere Auflösung gerendert"
    ///
    /// Zielgröße-Optionen:
    ///   Original  = selbe Größe wie Eingabe  (z.B. 128 → 4x → 512 → runter auf 128)
    ///   2x        = doppelte Größe            (z.B. 128 → 4x → 512 → runter auf 256)
    ///
    /// Downscale-Filter:
    ///   Lanczos3  = schärfster, bester für Sprites (selbst implementiert)
    ///   Bicubic   = weich, kaum Ringing
    ///   Bilinear  = schnell, etwas weicher
    /// </summary>
    public static class SuperSampler
    {
        public enum DownscaleFilter { Lanczos3, Bicubic, Bilinear }
        public enum OutputSize     { Original, TwoX }

        /// <summary>
        /// Führt den kompletten SSAA-Prozess durch:
        /// Nearest-4x → Shading → hochwertiger Downscale
        /// </summary>
        public static Bitmap Process(Bitmap src, EnhancerOptions enhancerOpts,
            DownscaleFilter filter = DownscaleFilter.Lanczos3,
            OutputSize targetSize  = OutputSize.TwoX)
        {
            int srcW = src.Width;
            int srcH = src.Height;

            // Zielgröße berechnen
            int dstW = targetSize == OutputSize.TwoX ? srcW * 2 : srcW;
            int dstH = targetSize == OutputSize.TwoX ? srcH * 2 : srcH;

            // Schritt 1: 4x hochskalieren (Nearest-Neighbor — kein Weichzeichnen)
            using Bitmap hi = NearestScale(src, srcW * 4, srcH * 4);

            // Schritt 2: Pixel-Shading bei hoher Auflösung
            Bitmap shaded = PixelEnhancer.Apply(hi, enhancerOpts);
            Bitmap toDownscale = ReferenceEquals(shaded, hi) ? hi : shaded;

            // Schritt 3: Hochwertiger Downscale auf Zielgröße
            // WICHTIG: shaded erst NACH dem Downscale disposen
            try
            {
                return filter switch
                {
                    DownscaleFilter.Lanczos3 => Lanczos3Scale(toDownscale, dstW, dstH),
                    DownscaleFilter.Bicubic  => GdiScale(toDownscale, dstW, dstH, InterpolationMode.HighQualityBicubic),
                    DownscaleFilter.Bilinear => GdiScale(toDownscale, dstW, dstH, InterpolationMode.HighQualityBilinear),
                    _                        => Lanczos3Scale(toDownscale, dstW, dstH)
                };
            }
            finally
            {
                // Dispose shaded nur wenn es ein neues Bitmap ist (nicht Referenz auf hi)
                if (!ReferenceEquals(shaded, hi)) shaded.Dispose();
            }
        }

        // ─── NEAREST NEIGHBOR (4x) ───────────────────────────────────────────────

        private static Bitmap NearestScale(Bitmap src, int dstW, int dstH)
        {
            // Pixel-für-Pixel kopieren — kein Weichzeichnen, keine Interpolation
            int srcW = src.Width;
            int srcH = src.Height;

            BitmapData srcData = src.LockBits(new Rectangle(0, 0, srcW, srcH),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] srcPixels = new int[srcW * srcH];
            try { Marshal.Copy(srcData.Scan0, srcPixels, 0, srcPixels.Length); }
            finally { src.UnlockBits(srcData); }

            int scaleX = dstW / srcW;
            int scaleY = dstH / srcH;

            Bitmap dst = new Bitmap(dstW, dstH, PixelFormat.Format32bppArgb);
            BitmapData dstData = dst.LockBits(new Rectangle(0, 0, dstW, dstH),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int[] dstPixels = new int[dstW * dstH];

            for (int sy = 0; sy < srcH; sy++)
                for (int sx = 0; sx < srcW; sx++)
                {
                    int p = srcPixels[sy * srcW + sx];
                    for (int ky = 0; ky < scaleY; ky++)
                        for (int kx = 0; kx < scaleX; kx++)
                            dstPixels[(sy * scaleY + ky) * dstW + (sx * scaleX + kx)] = p;
                }

            try { Marshal.Copy(dstPixels, 0, dstData.Scan0, dstPixels.Length); }
            finally { dst.UnlockBits(dstData); }

            return dst;
        }

        // ─── GDI+ DOWNSCALE (Bicubic / Bilinear) ────────────────────────────────

        private static Bitmap GdiScale(Bitmap src, int dstW, int dstH, InterpolationMode mode)
        {
            var dst = new Bitmap(dstW, dstH, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(dst);
            g.InterpolationMode  = mode;
            g.SmoothingMode      = SmoothingMode.HighQuality;
            g.PixelOffsetMode    = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.DrawImage(src, 0, 0, dstW, dstH);
            return dst;
        }

        // ─── LANCZOS3 DOWNSCALE ──────────────────────────────────────────────────
        // Schärfster Downscale-Filter — deutlich besser als GDI+ Bicubic für Sprites.
        // Lanczos-Kernel: sinc(x) * sinc(x/a), a=3
        // Gibt die schärfsten Kanten bei minimalem Ringing.

        private static Bitmap Lanczos3Scale(Bitmap src, int dstW, int dstH)
        {
            int srcW = src.Width;
            int srcH = src.Height;

            BitmapData srcData = src.LockBits(new Rectangle(0, 0, srcW, srcH),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] srcPixels = new int[srcW * srcH];
            try { Marshal.Copy(srcData.Scan0, srcPixels, 0, srcPixels.Length); }
            finally { src.UnlockBits(srcData); }

            double scaleX = (double)srcW / dstW;
            double scaleY = (double)srcH / dstH;
            const int a = 3;
            int[] dstPixels = new int[dstW * dstH];

            // Strip-basierte Verarbeitung: begrenzt RAM-Verbrauch auf dstW×StripH×4 floats
            // Statt einem riesigen dstW×srcH Array
            const int StripH = 32;
            var intermediate = new float[dstW * StripH * 4];

            for (int stripStart = 0; stripStart < srcH; stripStart += StripH)
            {
                int stripEnd  = Math.Min(stripStart + StripH, srcH);

                // Horizontaler Pass für diesen Strip
                for (int sy = stripStart; sy < stripEnd; sy++)
                {
                    int localY = sy - stripStart;
                    for (int dx = 0; dx < dstW; dx++)
                    {
                        double srcX = (dx + 0.5) * scaleX - 0.5;
                        int x0 = (int)Math.Floor(srcX) - a + 1;
                        int x1 = (int)Math.Floor(srcX) + a;
                        double rS=0,gS=0,bS=0,aS=0,wS=0;
                        for (int sx = x0; sx <= x1; sx++)
                        {
                            int sxc = Math.Clamp(sx, 0, srcW - 1);
                            double w = LanczosKernel(srcX - sx, a);
                            int p = srcPixels[sy * srcW + sxc];
                            aS += ((p >> 24) & 0xFF) * w; rS += ((p >> 16) & 0xFF) * w;
                            gS += ((p >>  8) & 0xFF) * w; bS +=  (p & 0xFF)        * w;
                            wS += w;
                        }
                        if (wS > 0)
                        {
                            int idx = (localY * dstW + dx) * 4;
                            intermediate[idx]=  (float)(rS/wS); intermediate[idx+1]=(float)(gS/wS);
                            intermediate[idx+2]=(float)(bS/wS); intermediate[idx+3]=(float)(aS/wS);
                        }
                    }
                }

                // Vertikaler Pass: nur dstY-Zeilen die Quell-Pixel aus diesem Strip nutzen
                for (int dy = 0; dy < dstH; dy++)
                {
                    double srcY = (dy + 0.5) * scaleY - 0.5;
                    int y0 = (int)Math.Floor(srcY) - a + 1;
                    int y1 = (int)Math.Floor(srcY) + a;

                    bool anyInStrip = false;
                    for (int sy2 = y0; sy2 <= y1; sy2++)
                        if (Math.Clamp(sy2, 0, srcH-1) >= stripStart && Math.Clamp(sy2, 0, srcH-1) < stripEnd)
                            { anyInStrip = true; break; }
                    if (!anyInStrip) continue;

                    for (int dx = 0; dx < dstW; dx++)
                    {
                        double rS=0,gS=0,bS=0,aS=0,wS=0;
                        for (int sy2 = y0; sy2 <= y1; sy2++)
                        {
                            int syc = Math.Clamp(sy2, 0, srcH-1);
                            if (syc < stripStart || syc >= stripEnd) continue;
                            double w = LanczosKernel(srcY - sy2, a);
                            int idx = ((syc - stripStart) * dstW + dx) * 4;
                            rS+=intermediate[idx]*w; gS+=intermediate[idx+1]*w;
                            bS+=intermediate[idx+2]*w; aS+=intermediate[idx+3]*w;
                            wS+=w;
                        }
                        if (wS > 0)
                            dstPixels[dy * dstW + dx] =
                                (Clamp((int)(aS/wS)) << 24) | (Clamp((int)(rS/wS)) << 16) |
                                (Clamp((int)(gS/wS)) <<  8) |  Clamp((int)(bS/wS));
                    }
                }
            }

            Bitmap dst = new Bitmap(dstW, dstH, PixelFormat.Format32bppArgb);
            BitmapData dstData = dst.LockBits(new Rectangle(0, 0, dstW, dstH),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try { Marshal.Copy(dstPixels, 0, dstData.Scan0, dstPixels.Length); }
            finally { dst.UnlockBits(dstData); }
            return dst;
        }

        // sinc(x) * sinc(x/a), a = Lanczos window
        private static double LanczosKernel(double x, int a)
        {
            if (x == 0.0) return 1.0;
            if (Math.Abs(x) >= a) return 0.0;
            double px = Math.PI * x;
            return (Math.Sin(px) / px) * (Math.Sin(px / a) / (px / a));
        }

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;
    }

    /// <summary>
    /// Einstellungen für den Super-Sampler.
    /// </summary>
    public class SuperSamplerOptions
    {
        public bool                        Enabled     { get; set; } = false;
        public SuperSampler.DownscaleFilter Filter      { get; set; } = SuperSampler.DownscaleFilter.Lanczos3;
        public SuperSampler.OutputSize     TargetSize  { get; set; } = SuperSampler.OutputSize.TwoX;
    }

    // ═════════════════════════════════════════════════════════════════
    // Bitmap-Pool für wiederkehrende Größen (TASK-0203)
    // ═════════════════════════════════════════════════════════════════
    public static class BitmapPool
    {
        private static readonly Dictionary<(int w, int h), Stack<Bitmap>> _pools = new();
        private static readonly object _lock = new();

        public static Bitmap Rent(int width, int height)
        {
            lock (_lock)
            {
                var key = (width, height);
                if (_pools.TryGetValue(key, out var stack) && stack.Count > 0)
                {
                    return stack.Pop();
                }
            }
            return new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        }

        public static void Return(Bitmap bmp)
        {
            if (bmp == null) return;
            var key = (bmp.Width, bmp.Height);
            lock (_lock)
            {
                if (!_pools.TryGetValue(key, out var stack))
                {
                    stack = new Stack<Bitmap>();
                    _pools[key] = stack;
                }
                // Begrenze Pool-Größe pro Größe auf 16 Instanzen
                if (stack.Count < 16)
                {
                    stack.Push(bmp);
                }
                else
                {
                    bmp.Dispose();
                }
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                foreach (var stack in _pools.Values)
                {
                    foreach (var bmp in stack)
                        bmp.Dispose();
                    stack.Clear();
                }
                _pools.Clear();
            }
        }
    }
}
