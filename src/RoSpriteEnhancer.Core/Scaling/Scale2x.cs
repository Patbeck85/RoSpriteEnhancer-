using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RoSpriteEnhancer.Core.Scaling
{
    /// <summary>
    /// Scale2x (EPX) algorithm — optimiert für Pixel-Art.
    /// Erkennt Konturen und interpoliert intelligent, ohne Kanten zu verwischen.
    /// Läuft vollständig auf CPU, kein GPU nötig.
    /// </summary>
    public static class Scale2x
    {
        public static Bitmap Scale(Bitmap src, int passes = 1)
        {
            Bitmap current = src;
            for (int p = 0; p < passes; p++)
            {
                Bitmap next;
                try
                {
                    next = ScaleOnce(current);
                }
                catch
                {
                    // Dispose intermediate result from a previous pass (not the original src)
                    if (p > 0) current.Dispose();
                    throw;
                }
                if (p > 0) current.Dispose();
                current = next;
            }
            return current;
        }

        private static Bitmap ScaleOnce(Bitmap src)
        {
            int sw = src.Width;
            int sh = src.Height;
            int dw = sw * 2;
            int dh = sh * 2;

            // Read source pixels with safe unlock
            BitmapData srcData = src.LockBits(
                new Rectangle(0, 0, sw, sh),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            int[] srcPixels = new int[sw * sh];
            try
            {
                Marshal.Copy(srcData.Scan0, srcPixels, 0, srcPixels.Length);
            }
            finally
            {
                src.UnlockBits(srcData);
            }

            // Scale2x core algorithm
            int[] dstPixels = new int[dw * dh];
            for (int y = 0; y < sh; y++)
            {
                for (int x = 0; x < sw; x++)
                {
                    int E = srcPixels[y * sw + x];
                    int A = y > 0      ? srcPixels[(y-1) * sw + x]  : E;
                    int B = x < sw - 1 ? srcPixels[y * sw + (x+1)]  : E;
                    int C = x > 0      ? srcPixels[y * sw + (x-1)]  : E;
                    int D = y < sh - 1 ? srcPixels[(y+1) * sw + x]  : E;

                    int e0 = (ColorsEqual(C, A) && !ColorsEqual(C, D) && !ColorsEqual(A, B)) ? A : E;
                    int e1 = (ColorsEqual(A, B) && !ColorsEqual(A, C) && !ColorsEqual(B, D)) ? B : E;
                    int e2 = (ColorsEqual(D, C) && !ColorsEqual(D, B) && !ColorsEqual(C, A)) ? C : E;
                    int e3 = (ColorsEqual(B, D) && !ColorsEqual(B, A) && !ColorsEqual(D, C)) ? D : E;

                    int dy = y * 2;
                    int dx = x * 2;
                    dstPixels[dy * dw + dx]             = e0;
                    dstPixels[dy * dw + (dx + 1)]       = e1;
                    dstPixels[(dy + 1) * dw + dx]       = e2;
                    dstPixels[(dy + 1) * dw + (dx + 1)] = e3;
                }
            }

            // Write destination pixels with safe unlock
            Bitmap dst = new Bitmap(dw, dh, PixelFormat.Format32bppArgb);
            BitmapData dstData = dst.LockBits(
                new Rectangle(0, 0, dw, dh),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                Marshal.Copy(dstPixels, 0, dstData.Scan0, dstPixels.Length);
            }
            finally
            {
                dst.UnlockBits(dstData);
            }
            return dst;
        }

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static bool ColorsEqual(int a, int b)
        {
            // Volle ARGB-Gleichheit inkl. Alpha.
            // WICHTIG: Nur RGB zu vergleichen würde einen transparenten Pixel
            // (RGB=0,0,0 Alpha=0, von MagentaToAlpha) mit einem sichtbaren
            // schwarzen Outline-Pixel (RGB=0,0,0 Alpha=255) gleichsetzen —
            // sehr häufig bei RO-Sprites mit schwarzer Kontur. Das würde zu
            // falscher Kantenerkennung an jeder Outline führen.
            return a == b;
        }
    }
}
