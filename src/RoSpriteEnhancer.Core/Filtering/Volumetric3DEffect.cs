using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RoSpriteEnhancer.Core.Filtering
{
    public static class Volumetric3DEffect
    {
        public static Bitmap Apply(Bitmap src, float depthStrength = 0.75f)
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
            float[] distMap = new float[w * h];
            int maxDistRadius = Math.Max(4, (int)(Math.Min(w, h) * 0.18f));

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    if (((srcPixels[idx] >> 24) & 0xFF) < 10) continue;

                    float minDist = maxDistRadius;
                    for (int dy = -maxDistRadius; dy <= maxDistRadius; dy++)
                    {
                        int ny = y + dy;
                        if (ny < 0 || ny >= h) { minDist = Math.Min(minDist, Math.Abs(dy)); continue; }
                        for (int dx = -maxDistRadius; dx <= maxDistRadius; dx++)
                        {
                            int nx = x + dx;
                            if (nx < 0 || nx >= w || ((srcPixels[ny * w + nx] >> 24) & 0xFF) < 10)
                            {
                                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                                if (d < minDist) minDist = d;
                            }
                        }
                    }
                    distMap[idx] = Math.Clamp(minDist / maxDistRadius, 0f, 1f);
                }
            }

            float lightX = -0.577f, lightY = -0.577f, lightZ = 0.577f;

            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    int p = srcPixels[idx];
                    int a = (p >> 24) & 0xFF;
                    if (a < 10) { dstPixels[idx] = p; continue; }

                    int r = (p >> 16) & 0xFF, g = (p >> 8) & 0xFF, b = p & 0xFF;

                    float dL = x > 0 ? distMap[y * w + (x - 1)] : 0f;
                    float dR = x < w - 1 ? distMap[y * w + (x + 1)] : 0f;
                    float dU = y > 0 ? distMap[(y - 1) * w + x] : 0f;
                    float dD = y < h - 1 ? distMap[(y + 1) * w + x] : 0f;

                    float nx = (dL - dR) * 2.5f;
                    float ny = (dU - dD) * 2.5f;
                    float nz = 1.0f;

                    float len = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
                    nx /= len; ny /= len; nz /= len;

                    float dot = nx * lightX + ny * lightY + nz * lightZ;
                    float ambient = 0.65f;
                    float diffuse = Math.Max(0f, dot) * depthStrength;

                    float totalLight = (ambient + diffuse) * (0.65f + distMap[idx] * 0.35f);

                    int finalR = Math.Clamp((int)(r * totalLight), 0, 255);
                    int finalG = Math.Clamp((int)(g * totalLight), 0, 255);
                    int finalB = Math.Clamp((int)(b * totalLight), 0, 255);

                    dstPixels[idx] = (a << 24) | (finalR << 16) | (finalG << 8) | finalB;
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
