using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RoSpriteEnhancer.Core.Filtering
{
    public static class PixelEnhancer
    {
        public static Bitmap Apply(Bitmap src, EnhancerOptions opts)
        {
            bool hasColorShift = Math.Abs(opts.HueShift) > 0.01f
                || Math.Abs(opts.SaturationScale - 1f) > 0.001f
                || Math.Abs(opts.LightnessScale - 1f) > 0.001f;

            if (!opts.Outline && !opts.Shadow && !opts.Highlight && !opts.Sharpen && !opts.Volumetric3D && !hasColorShift)
                return src;

            int w = src.Width;
            int h = src.Height;

            BitmapData data = src.LockBits(new Rectangle(0, 0, w, h),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] pixels = new int[w * h];
            try
            {
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            }
            finally
            {
                src.UnlockBits(data);
            }

            int[] result = (int[])pixels.Clone();

            if (opts.Outline)   ApplyOutline(pixels, result, w, h, opts.OutlineStrength, opts.OutlineMode);
            if (opts.Shadow)    ApplyShadow(pixels, result, w, h, opts.ShadowStrength);
            if (opts.Highlight) ApplyHighlight(pixels, result, w, h, opts.HighlightStrength);
            if (opts.Sharpen)   ApplySharpen(pixels, result, w, h, opts.SharpenStrength);
            if (hasColorShift)  ApplyColorShift(result, opts.HueShift, opts.SaturationScale, opts.LightnessScale);

            if (opts.Volumetric3D)
            {
                Bitmap tempBmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                BitmapData td = tempBmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(result, 0, td.Scan0, result.Length);
                tempBmp.UnlockBits(td);
                using Bitmap v3dBmp = Volumetric3DEffect.Apply(tempBmp, opts.Volumetric3DStrength);
                BitmapData td2 = v3dBmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(td2.Scan0, result, 0, result.Length);
                v3dBmp.UnlockBits(td2);
                tempBmp.Dispose();
            }

            if (opts.ExclusionColor.HasValue)
            {
                int excl = opts.ExclusionColor.Value;
                for (int i = 0; i < pixels.Length; i++)
                    if (pixels[i] == excl)
                        result[i] = pixels[i];
            }

            Bitmap dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            BitmapData dstData = dst.LockBits(new Rectangle(0, 0, w, h),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                Marshal.Copy(result, 0, dstData.Scan0, result.Length);
            }
            finally
            {
                dst.UnlockBits(dstData);
            }
            return dst;
        }

        private static void ApplyOutline(int[] src, int[] dst, int w, int h, float strength, OutlineMode mode)
        {
            int outlineAlpha = Clamp((int)(220 * Math.Min(1f, strength)));
            int outlineR = 20, outlineG = 20, outlineB = 20;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    int pixel = src[idx];
                    int alpha = (pixel >> 24) & 0xFF;

                    if (alpha < 10)
                    {
                        if (mode == OutlineMode.InternalOnly) continue;

                        bool nearBody =
                            ((SafeGet(src, w, h, x - 1, y) >> 24) & 0xFF) >= 128 ||
                            ((SafeGet(src, w, h, x + 1, y) >> 24) & 0xFF) >= 128 ||
                            ((SafeGet(src, w, h, x, y - 1) >> 24) & 0xFF) >= 128 ||
                            ((SafeGet(src, w, h, x, y + 1) >> 24) & 0xFF) >= 128;

                        if (nearBody)
                        {
                            dst[idx] = (outlineAlpha << 24) | (outlineR << 16) | (outlineG << 8) | outlineB;
                        }
                        continue;
                    }

                    int rC = (pixel >> 16) & 0xFF;
                    int gC = (pixel >> 8)  & 0xFF;
                    int bC =  pixel        & 0xFF;

                    int maxDiff = 0;
                    maxDiff = Math.Max(maxDiff, ColorDiff(SafeGet(src, w, h, x, y - 1), rC, gC, bC));
                    maxDiff = Math.Max(maxDiff, ColorDiff(SafeGet(src, w, h, x, y + 1), rC, gC, bC));
                    maxDiff = Math.Max(maxDiff, ColorDiff(SafeGet(src, w, h, x - 1, y), rC, gC, bC));
                    maxDiff = Math.Max(maxDiff, ColorDiff(SafeGet(src, w, h, x + 1, y), rC, gC, bC));

                    if (maxDiff > 55)
                    {
                        float darken = 1.0f - (strength * 0.40f);
                        int nr = Clamp((int)(rC * darken));
                        int ng = Clamp((int)(gC * darken));
                        int nb = Clamp((int)(bC * darken));
                        dst[idx] = (alpha << 24) | (nr << 16) | (ng << 8) | nb;
                    }
                }
            }
        }

        private static int SafeGet(int[] src, int w, int h, int x, int y)
        {
            if (x < 0 || x >= w || y < 0 || y >= h) return 0;
            return src[y * w + x];
        }

        private readonly struct OutlineCandidate
        {
            public readonly int X, Y, R, G, B;
            public OutlineCandidate(int x, int y, int r, int g, int b)
            { X = x; Y = y; R = r; G = g; B = b; }
        }

        private static List<OutlineCandidate> CollectOutlineCandidates(int[] src, int w, int h)
        {
            var result = new List<OutlineCandidate>();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int pixel = src[y * w + x];
                    int alpha = (pixel >> 24) & 0xFF;
                    if (alpha < 128) continue;

                    int r = (pixel >> 16) & 0xFF;
                    int g = (pixel >>  8) & 0xFF;
                    int b =  pixel        & 0xFF;
                    float luma = (r * 0.299f + g * 0.587f + b * 0.114f) / 255f;
                    if (luma > 0.35f) continue;

                    bool atEdge =
                        ((SafeGet(src, w, h, x - 1, y) >> 24) & 0xFF) < 10 ||
                        ((SafeGet(src, w, h, x + 1, y) >> 24) & 0xFF) < 10 ||
                        ((SafeGet(src, w, h, x, y - 1) >> 24) & 0xFF) < 10 ||
                        ((SafeGet(src, w, h, x, y + 1) >> 24) & 0xFF) < 10;
                    if (!atEdge) continue;

                    result.Add(new OutlineCandidate(x, y, r, g, b));
                }
            }
            return result;
        }

        private static (bool found, int r, int g, int b) AverageCandidates(List<OutlineCandidate> candidates)
        {
            if (candidates.Count < 8) return (false, 0, 0, 0);
            long sr = 0, sg = 0, sb = 0;
            foreach (var c in candidates) { sr += c.R; sg += c.G; sb += c.B; }
            int n = candidates.Count;
            return (true, (int)(sr / n), (int)(sg / n), (int)(sb / n));
        }

        private static (bool found, int r, int g, int b) LocalOutlineColor(
            List<OutlineCandidate> candidates, int x, int y)
        {
            long sr = 0, sg = 0, sb = 0;
            int cnt = 0;
            foreach (var c in candidates)
            {
                int dx = c.X - x, dy = c.Y - y;
                int distSq = dx * dx + dy * dy;
                if (distSq > 100) continue;
                sr += c.R; sg += c.G; sb += c.B; cnt++;
            }
            if (cnt < 2) return (false, 0, 0, 0);
            return (true, (int)(sr / cnt), (int)(sg / cnt), (int)(sb / cnt));
        }

        private static void AccumulateNeighbor(int pixel, ref int sr, ref int sg, ref int sb, ref int cnt)
        {
            int a = (pixel >> 24) & 0xFF;
            if (a < 10) return;
            sr += (pixel >> 16) & 0xFF;
            sg += (pixel >>  8) & 0xFF;
            sb +=  pixel        & 0xFF;
            cnt++;
        }

        private static int ColorDiff(int neighbor, int rC, int gC, int bC)
        {
            int a = (neighbor >> 24) & 0xFF;
            if (a < 10) return 0;
            return Math.Abs(((neighbor >> 16) & 0xFF) - rC)
                 + Math.Abs(((neighbor >>  8) & 0xFF) - gC)
                 + Math.Abs(( neighbor        & 0xFF) - bC);
        }

        private static void ApplyShadow(int[] src, int[] dst, int w, int h, float strength)
        {
            int offsetX = Math.Max(2, (int)(Math.Max(w, h) * 0.03f));
            int offsetY = Math.Max(2, (int)(Math.Max(w, h) * 0.03f));
            float alphaFactor = 0.55f * strength;
            int[] shadow = new int[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int alpha = (src[y * w + x] >> 24) & 0xFF;
                    if (alpha < 10) continue;

                    int sx = x + offsetX;
                    int sy = y + offsetY;
                    if (sx >= w || sy >= h) continue;

                    int sidx = sy * w + sx;
                    int existing = shadow[sidx];
                    int newAlpha = Math.Min(255, existing + (int)(alpha * alphaFactor));
                    shadow[sidx] = newAlpha;
                }
            }

            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    int sidx = y * w + x;
                    if (shadow[sidx] == 0) continue;

                    int targetAlpha = (dst[sidx] >> 24) & 0xFF;
                    if (targetAlpha >= 20) continue;

                    int blurred = 0, cnt = 0;
                    for (int ky = -1; ky <= 1; ky++)
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            blurred += shadow[(y + ky) * w + (x + kx)];
                            cnt++;
                        }
                    int finalAlpha = Clamp(blurred / cnt);
                    if (finalAlpha > 0)
                        dst[sidx] = (finalAlpha << 24);
                }
            }
        }

        private static void ApplyHighlight(int[] src, int[] dst, int w, int h, float strength)
        {
            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    int idx   = y * w + x;
                    int pixel = src[idx];
                    int alpha = (pixel >> 24) & 0xFF;
                    if (alpha < 10) continue;

                    int r = (pixel >> 16) & 0xFF;
                    int g = (pixel >>  8) & 0xFF;
                    int b =  pixel        & 0xFF;

                    float luma = (r * 0.299f + g * 0.587f + b * 0.114f) / 255f;
                    float lumaBoost = luma > 0.45f ? (luma - 0.45f) * 0.7f * strength : 0f;

                    int aTop     = (src[(y-1)*w + x    ] >> 24) & 0xFF;
                    int aLeft    = (src[y*w     + (x-1)] >> 24) & 0xFF;
                    int aTopLeft = (src[(y-1)*w + (x-1)] >> 24) & 0xFF;
                    bool facesLight = aTop < 40 || aLeft < 40 || aTopLeft < 40;
                    float rimBoost = facesLight ? 0.35f * strength : 0f;

                    float boost = Math.Max(lumaBoost, rimBoost);
                    if (boost > 0f)
                    {
                        int nr = Clamp((int)(r + (255 - r) * boost));
                        int ng = Clamp((int)(g + (255 - g) * boost));
                        int nb = Clamp((int)(b + (255 - b) * boost));

                        int avg = (nr + ng + nb) / 3;
                        float satBoost = 0.15f * strength;
                        nr = Clamp((int)(nr + (nr - avg) * satBoost));
                        ng = Clamp((int)(ng + (ng - avg) * satBoost));
                        nb = Clamp((int)(nb + (nb - avg) * satBoost));

                        dst[idx] = (alpha << 24) | (nr << 16) | (ng << 8) | nb;
                    }
                }
            }
        }

        private static readonly int[] SharpenKernel = { 0, -1, 0, -1, 5, -1, 0, -1, 0 };

        private static void ApplySharpen(int[] src, int[] dst, int w, int h, float strength)
        {
            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    int center = src[y * w + x];
                    int alpha = (center >> 24) & 0xFF;
                    if (alpha < 10) continue;

                    float rSum = 0, gSum = 0, bSum = 0;
                    int ki = 0;
                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            int neighbor = src[(y+ky)*w + (x+kx)];
                            if (((neighbor >> 24) & 0xFF) < 10) neighbor = center;
                            rSum += ((neighbor >> 16) & 0xFF) * SharpenKernel[ki];
                            gSum += ((neighbor >>  8) & 0xFF) * SharpenKernel[ki];
                            bSum +=  (neighbor        & 0xFF) * SharpenKernel[ki];
                            ki++;
                        }
                    }

                    int origR = (center >> 16) & 0xFF;
                    int origG = (center >>  8) & 0xFF;
                    int origB =  center        & 0xFF;

                    dst[y*w+x] = (alpha << 24)
                        | (Clamp((int)(origR + (rSum - origR) * strength)) << 16)
                        | (Clamp((int)(origG + (gSum - origG) * strength)) << 8)
                        |  Clamp((int)(origB + (bSum - origB) * strength));
                }
            }
        }

        private static void ApplyColorShift(int[] pixels, float hueShiftDeg, float satScale, float lightScale)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                int pixel = pixels[i];
                int alpha = (pixel >> 24) & 0xFF;
                if (alpha < 10) continue;

                int r = (pixel >> 16) & 0xFF;
                int g = (pixel >>  8) & 0xFF;
                int b =  pixel        & 0xFF;

                RgbToHsl(r, g, b, out float h, out float s, out float l);

                h = (h + hueShiftDeg) % 360f;
                if (h < 0) h += 360f;
                s = Math.Clamp(s * satScale, 0f, 1f);
                l = Math.Clamp(l * lightScale, 0f, 1f);

                HslToRgb(h, s, l, out int nr, out int ng, out int nb);
                pixels[i] = (alpha << 24) | (nr << 16) | (ng << 8) | nb;
            }
        }

        private static void RgbToHsl(int r, int g, int b, out float h, out float s, out float l)
        {
            float rf = r / 255f, gf = g / 255f, bf = b / 255f;
            float max = Math.Max(rf, Math.Max(gf, bf));
            float min = Math.Min(rf, Math.Min(gf, bf));
            float delta = max - min;

            l = (max + min) / 2f;

            if (delta < 0.0001f) { h = 0f; s = 0f; return; }

            s = l < 0.5f ? delta / (max + min) : delta / (2f - max - min);

            if (max == rf)      h = 60f * (((gf - bf) / delta) % 6f);
            else if (max == gf) h = 60f * (((bf - rf) / delta) + 2f);
            else                h = 60f * (((rf - gf) / delta) + 4f);
            if (h < 0) h += 360f;
        }

        private static void HslToRgb(float h, float s, float l, out int r, out int g, out int b)
        {
            if (s < 0.0001f)
            {
                int v = Clamp((int)(l * 255f));
                r = g = b = v;
                return;
            }

            float c = (1f - Math.Abs(2f * l - 1f)) * s;
            float x = c * (1f - Math.Abs((h / 60f) % 2f - 1f));
            float m = l - c / 2f;

            float rf, gf, bf;
            if      (h < 60f)  { rf = c; gf = x; bf = 0f; }
            else if (h < 120f) { rf = x; gf = c; bf = 0f; }
            else if (h < 180f) { rf = 0f; gf = c; bf = x; }
            else if (h < 240f) { rf = 0f; gf = x; bf = c; }
            else if (h < 300f) { rf = x; gf = 0f; bf = c; }
            else               { rf = c; gf = 0f; bf = x; }

            r = Clamp((int)((rf + m) * 255f));
            g = Clamp((int)((gf + m) * 255f));
            b = Clamp((int)((bf + m) * 255f));
        }

        private static int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;
    }

    public enum OutlineMode
    {
        Full,
        InternalOnly,
    }

    public class EnhancerOptions
    {
        public bool  Outline          { get; set; } = true;
        public float OutlineStrength  { get; set; } = 0.5f;
        public OutlineMode OutlineMode{ get; set; } = OutlineMode.Full;
        public bool  Shadow           { get; set; } = false;
        public float ShadowStrength   { get; set; } = 1.0f;
        public bool  Highlight        { get; set; } = false;
        public float HighlightStrength{ get; set; } = 0.0f;
        public bool  Sharpen          { get; set; } = false;
        public float SharpenStrength  { get; set; } = 0.3f;
        public bool  Volumetric3D     { get; set; } = false;
        public float Volumetric3DStrength { get; set; } = 0.75f;
        public int? ExclusionColor    { get; set; } = null;
        public float HueShift         { get; set; } = 0f;
        public float SaturationScale  { get; set; } = 1.0f;
        public float LightnessScale   { get; set; } = 1.0f;
    }
}
