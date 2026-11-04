using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RoSpriteEnhancer.Core.Utils
{
    public static class MagentaHandler
    {
        private const int MagentaR = 255;
        private const int MagentaG = 0;
        private const int MagentaB = 255;
        private const int MagentaTolerance = 8;

        public static Bitmap MagentaToAlpha(Bitmap src)
        {
            int w = src.Width, h = src.Height;
            Bitmap dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            BitmapData srcData = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData dstData = dst.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    byte* ptrSrc = (byte*)srcData.Scan0;
                    byte* ptrDst = (byte*)dstData.Scan0;

                    for (int y = 0; y < h; y++)
                    {
                        int* rowSrc = (int*)(ptrSrc + y * srcData.Stride);
                        int* rowDst = (int*)(ptrDst + y * dstData.Stride);

                        for (int x = 0; x < w; x++)
                        {
                            int p = rowSrc[x];
                            int r = (p >> 16) & 0xFF;
                            int g = (p >> 8) & 0xFF;
                            int b = p & 0xFF;

                            if (IsMagenta(r, g, b))
                                rowDst[x] = 0x00000000;
                            else
                                rowDst[x] = p;
                        }
                    }
                }
            }
            finally
            {
                src.UnlockBits(srcData);
                dst.UnlockBits(dstData);
            }

            return dst;
        }

        public static Bitmap AlphaToMagenta(Bitmap src, int alphaThreshold = 128)
        {
            int w = src.Width, h = src.Height;
            int magentaPacked = unchecked((int)0xFF_FF_00_FF);
            Bitmap dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            BitmapData srcData = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData dstData = dst.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    byte* ptrSrc = (byte*)srcData.Scan0;
                    byte* ptrDst = (byte*)dstData.Scan0;

                    for (int y = 0; y < h; y++)
                    {
                        int* rowSrc = (int*)(ptrSrc + y * srcData.Stride);
                        int* rowDst = (int*)(ptrDst + y * dstData.Stride);

                        for (int x = 0; x < w; x++)
                        {
                            int p = rowSrc[x];
                            int a = (p >> 24) & 0xFF;
                            rowDst[x] = a < alphaThreshold ? magentaPacked : p | unchecked((int)0xFF000000);
                        }
                    }
                }
            }
            finally
            {
                src.UnlockBits(srcData);
                dst.UnlockBits(dstData);
            }

            return dst;
        }

        public static bool ContainsMagenta(Bitmap bmp)
        {
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    byte* ptr = (byte*)data.Scan0;
                    int w = bmp.Width, h = bmp.Height;

                    for (int y = 0; y < h; y++)
                    {
                        int* row = (int*)(ptr + y * data.Stride);
                        for (int x = 0; x < w; x++)
                        {
                            int p = row[x];
                            int r = (p >> 16) & 0xFF;
                            int g = (p >> 8) & 0xFF;
                            int b = p & 0xFF;
                            if (IsMagenta(r, g, b)) return true;
                        }
                    }
                }
            }
            finally { bmp.UnlockBits(data); }

            return false;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static bool IsMagenta(int r, int g, int b)
        {
            return Math.Abs(r - MagentaR) <= MagentaTolerance
                && Math.Abs(g - MagentaG) <= MagentaTolerance
                && Math.Abs(b - MagentaB) <= MagentaTolerance;
        }
    }
}
