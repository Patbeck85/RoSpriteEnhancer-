using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RoSpriteEnhancer.Core.Utils
{
    public static class ColorMatchLock
    {
        public static Bitmap Apply(Bitmap original, Bitmap enhanced)
        {
            int w = enhanced.Width, h = enhanced.Height;
            int origW = original.Width, origH = original.Height;

            // 1. Durschnittsfarben des Originals (Safe Unsafe Pointer)
            BitmapData dataOrig = original.LockBits(new Rectangle(0, 0, origW, origH), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] origPixels = new int[origW * origH];
            try
            {
                unsafe
                {
                    byte* ptr0 = (byte*)dataOrig.Scan0;
                    for (int y = 0; y < origH; y++)
                    {
                        int* row = (int*)(ptr0 + y * dataOrig.Stride);
                        for (int x = 0; x < origW; x++)
                            origPixels[y * origW + x] = row[x];
                    }
                }
            }
            finally { original.UnlockBits(dataOrig); }

            long sumR0 = 0, sumG0 = 0, sumB0 = 0; int cnt0 = 0;
            foreach (int p in origPixels)
            {
                if (((p >> 24) & 0xFF) < 10) continue;
                sumR0 += (p >> 16) & 0xFF;
                sumG0 += (p >> 8) & 0xFF;
                sumB0 += p & 0xFF;
                cnt0++;
            }
            if (cnt0 == 0) return new Bitmap(enhanced);

            float meanR0 = (float)sumR0 / cnt0;
            float meanG0 = (float)sumG0 / cnt0;
            float meanB0 = (float)sumB0 / cnt0;

            // 2. Durchschnittsfarben des KI-Ergebnisses (Safe Unsafe Pointer)
            BitmapData dataEnh = enhanced.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] enhPixels = new int[w * h];
            try
            {
                unsafe
                {
                    byte* ptr1 = (byte*)dataEnh.Scan0;
                    for (int y = 0; y < h; y++)
                    {
                        int* row = (int*)(ptr1 + y * dataEnh.Stride);
                        for (int x = 0; x < w; x++)
                            enhPixels[y * w + x] = row[x];
                    }
                }
            }
            finally { enhanced.UnlockBits(dataEnh); }

            long sumR1 = 0, sumG1 = 0, sumB1 = 0; int cnt1 = 0;
            foreach (int p in enhPixels)
            {
                if (((p >> 24) & 0xFF) < 10) continue;
                sumR1 += (p >> 16) & 0xFF;
                sumG1 += (p >> 8) & 0xFF;
                sumB1 += p & 0xFF;
                cnt1++;
            }
            if (cnt1 == 0) return new Bitmap(enhanced);

            float meanR1 = (float)sumR1 / cnt1;
            float meanG1 = (float)sumG1 / cnt1;
            float meanB1 = (float)sumB1 / cnt1;

            // 3. Delta-Korrektur
            float diffR = meanR0 - meanR1;
            float diffG = meanG0 - meanG1;
            float diffB = meanB0 - meanB1;

            if (Math.Abs(diffR) < 1.5f && Math.Abs(diffG) < 1.5f && Math.Abs(diffB) < 1.5f)
                return new Bitmap(enhanced);

            int[] result = new int[w * h];
            for (int i = 0; i < enhPixels.Length; i++)
            {
                int p = enhPixels[i];
                int a = (p >> 24) & 0xFF;
                if (a < 10) { result[i] = p; continue; }

                int r = (p >> 16) & 0xFF;
                int g = (p >> 8) & 0xFF;
                int b = p & 0xFF;

                int nr = Math.Clamp((int)(r + diffR), 0, 255);
                int ng = Math.Clamp((int)(g + diffG), 0, 255);
                int nb = Math.Clamp((int)(b + diffB), 0, 255);

                result[i] = (a << 24) | (nr << 16) | (ng << 8) | nb;
            }

            Bitmap dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            BitmapData dstData = dst.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    byte* ptrDst = (byte*)dstData.Scan0;
                    for (int y = 0; y < h; y++)
                    {
                        int* row = (int*)(ptrDst + y * dstData.Stride);
                        for (int x = 0; x < w; x++)
                            row[x] = result[y * w + x];
                    }
                }
            }
            finally { dst.UnlockBits(dstData); }

            return dst;
        }
    }
}
