using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RoSpriteEnhancer.Core.Utils
{
    public static class AlphaBleedCleaner
    {
        public static Bitmap Apply(Bitmap src)
        {
            int w = src.Width, h = src.Height;
            BitmapData data = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] pixels = new int[w * h];
            try { Marshal.Copy(data.Scan0, pixels, 0, pixels.Length); }
            finally { src.UnlockBits(data); }

            int[] result = (int[])pixels.Clone();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    if (((pixels[idx] >> 24) & 0xFF) > 10) continue;

                    long sr = 0, sg = 0, sb = 0; int cnt = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ny = Math.Clamp(y + dy, 0, h - 1);
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = Math.Clamp(x + dx, 0, w - 1);
                            int np = pixels[ny * w + nx];
                            if (((np >> 24) & 0xFF) > 10) { sr += (np >> 16) & 0xFF; sg += (np >> 8) & 0xFF; sb += np & 0xFF; cnt++; }
                        }
                    }

                    if (cnt > 0)
                    {
                        int r = (int)(sr / cnt), g = (int)(sg / cnt), b = (int)(sb / cnt);
                        result[idx] = (0 << 24) | (r << 16) | (g << 8) | b;
                    }
                }
            }

            Bitmap dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            BitmapData dstData = dst.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try { Marshal.Copy(result, 0, dstData.Scan0, result.Length); }
            finally { dst.UnlockBits(dstData); }
            return dst;
        }
    }
}
