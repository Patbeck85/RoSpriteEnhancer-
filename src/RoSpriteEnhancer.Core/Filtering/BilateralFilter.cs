using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RoSpriteEnhancer.Core.Filtering
{
    public static class BilateralFilter
    {
        private static readonly float[] RangeLut = new float[256 * 256 * 3];

        static BilateralFilter()
        {
            float rangeSigma = 20.0f;
            float rangeCoeff = -1.0f / (2 * rangeSigma * rangeSigma);
            for (int i = 0; i < RangeLut.Length; i++)
            {
                RangeLut[i] = (float)Math.Exp(i * rangeCoeff);
            }
        }

        public static Bitmap Apply(Bitmap src, float spatialSigma = 2.0f, float rangeSigma = 20.0f, bool preserveDetails = true)
        {
            int w = src.Width, h = src.Height;
            BitmapData data = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] srcPixels = new int[w * h];
            try
            {
                unsafe
                {
                    byte* ptr = (byte*)data.Scan0;
                    for (int y = 0; y < h; y++)
                    {
                        Marshal.Copy((IntPtr)(ptr + y * data.Stride), srcPixels, y * w, w);
                    }
                }
            }
            finally { src.UnlockBits(data); }

            int[] dstPixels = new int[w * h];
            int radius = (int)(spatialSigma * 2);

            float[] spatialWeights = new float[(2 * radius + 1) * (2 * radius + 1)];
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    spatialWeights[(dy + radius) * (2 * radius + 1) + (dx + radius)] =
                        (float)Math.Exp(-(dx * dx + dy * dy) / (2 * spatialSigma * spatialSigma));
                }
            }

            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < w; x++)
                {
                    int centerIdx = y * w + x;
                    int centerP = srcPixels[centerIdx];
                    int centerA = (centerP >> 24) & 0xFF;

                    if (centerA < 10) { dstPixels[centerIdx] = centerP; continue; }

                    int cr = (centerP >> 16) & 0xFF, cg = (centerP >> 8) & 0xFF, cb = centerP & 0xFF;

                    float blendFactor = 1.0f;

                    if (preserveDetails)
                    {
                        int pLeft  = srcPixels[y * w + Math.Max(x - 1, 0)];
                        int pRight = srcPixels[y * w + Math.Min(x + 1, w - 1)];
                        int pUp    = srcPixels[Math.Max(y - 1, 0) * w + x];
                        int pDown  = srcPixels[Math.Min(y + 1, h - 1) * w + x];

                        float lumaLeft  = ((pLeft >> 16) & 0xFF) * 0.299f + ((pLeft >> 8) & 0xFF) * 0.587f + (pLeft & 0xFF) * 0.114f;
                        float lumaRight = ((pRight >> 16) & 0xFF) * 0.299f + ((pRight >> 8) & 0xFF) * 0.587f + (pRight & 0xFF) * 0.114f;
                        float lumaUp    = ((pUp >> 16) & 0xFF) * 0.299f + ((pUp >> 8) & 0xFF) * 0.587f + (pUp & 0xFF) * 0.114f;
                        float lumaDown  = ((pDown >> 16) & 0xFF) * 0.299f + ((pDown >> 8) & 0xFF) * 0.587f + (pDown & 0xFF) * 0.114f;

                        float gX = lumaRight - lumaLeft;
                        float gY = lumaDown - lumaUp;
                        float edgeGrad = (float)Math.Sqrt(gX * gX + gY * gY);

                        blendFactor = Math.Clamp(1.0f - (edgeGrad - 25.0f) / 50.0f, 0.0f, 1.0f);
                    }

                    if (blendFactor <= 0.001f)
                    {
                        dstPixels[centerIdx] = centerP;
                        continue;
                    }

                    float sumR = 0, sumG = 0, sumB = 0, sumW = 0;

                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        int ny = Math.Clamp(y + dy, 0, h - 1);
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int nx = Math.Clamp(x + dx, 0, w - 1);
                            int neighborP = srcPixels[ny * w + nx];
                            if (((neighborP >> 24) & 0xFF) < 10) continue;

                            int nr = (neighborP >> 16) & 0xFF, ng = (neighborP >> 8) & 0xFF, nb = neighborP & 0xFF;

                            int dr = nr - cr, dg = ng - cg, db = nb - cb;
                            int colorDistSq = (int)(dr * dr * 0.299f + dg * dg * 0.587f + db * db * 0.114f);

                            float wRange = colorDistSq < RangeLut.Length ? RangeLut[colorDistSq] : 0f;
                            float wSpatial = spatialWeights[(dy + radius) * (2 * radius + 1) + (dx + radius)];
                            float weight = wSpatial * wRange;

                            sumR += nr * weight;
                            sumG += ng * weight;
                            sumB += nb * weight;
                            sumW += weight;
                        }
                    }

                    if (sumW > 0)
                    {
                        int smoothR = Math.Clamp((int)(sumR / sumW), 0, 255);
                        int smoothG = Math.Clamp((int)(sumG / sumW), 0, 255);
                        int smoothB = Math.Clamp((int)(sumB / sumW), 0, 255);

                        int finalR = (int)(smoothR * blendFactor + cr * (1.0f - blendFactor));
                        int finalG = (int)(smoothG * blendFactor + cg * (1.0f - blendFactor));
                        int finalB = (int)(smoothB * blendFactor + cb * (1.0f - blendFactor));

                        dstPixels[centerIdx] = (centerA << 24) | (finalR << 16) | (finalG << 8) | finalB;
                    }
                    else { dstPixels[centerIdx] = centerP; }
                }
            });

            Bitmap dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            BitmapData dstData = dst.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    byte* ptr = (byte*)dstData.Scan0;
                    for (int y = 0; y < h; y++)
                    {
                        Marshal.Copy(dstPixels, y * w, (IntPtr)(ptr + y * dstData.Stride), w);
                    }
                }
            }
            finally { dst.UnlockBits(dstData); }

            return dst;
        }
    }
}
