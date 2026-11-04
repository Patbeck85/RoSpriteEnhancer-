using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace RoSpriteEnhancer.Core.IO
{
    public class SprFile : IDisposable
    {
        public string FilePath { get; }
        public List<SprFrame> Frames { get; } = new();
        public int IndexedFrameCount { get; private set; }
        public int RgbaFrameCount    { get; private set; }
        public int TotalFrameCount   => Frames.Count;

        private bool _disposed;
        private SprFile(string path) { FilePath = path; }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var frame in Frames)
                frame.Dispose();
            Frames.Clear();
        }

        public static SprFile Load(string path)
        {
            var spr = new SprFile(path);
            using var br = new BinaryReader(File.OpenRead(path));

            byte m1 = br.ReadByte();
            byte m2 = br.ReadByte();
            if (m1 != 0x53 || m2 != 0x50)
                throw new InvalidDataException("Keine gültige SPR-Datei (Magic 'SP' fehlt).");

            ushort minor = br.ReadByte();
            ushort major = br.ReadByte();
            float version = major + minor / 10f;

            ushort indexedCount = br.ReadUInt16();
            ushort rgbaCount    = version >= 2.0f ? br.ReadUInt16() : (ushort)0;

            spr.IndexedFrameCount = indexedCount;
            spr.RgbaFrameCount    = rgbaCount;

            Color[] palette = new Color[256];

            if (version < 2.0f)
            {
                palette = ReadPalette(br);
                ReadIndexedFrames(br, spr, indexedCount, palette, rleEncoded: false);
            }
            else
            {
                long frameDataStart = br.BaseStream.Position;
                SkipFrames(br, indexedCount, rgbaCount, version);
                palette = ReadPalette(br);

                br.BaseStream.Seek(frameDataStart, SeekOrigin.Begin);
                ReadIndexedFrames(br, spr, indexedCount, palette, rleEncoded: true);
                ReadRgbaFrames(br, spr, rgbaCount);
            }

            return spr;
        }

        private static Color[] ReadPalette(BinaryReader br)
        {
            var palette = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                byte r = br.ReadByte();
                byte g = br.ReadByte();
                byte b = br.ReadByte();
                byte a = br.ReadByte();
                palette[i] = Color.FromArgb(255, r, g, b);
            }
            palette[0] = Color.FromArgb(0, 0, 0, 0);
            return palette;
        }

        private static void ReadIndexedFrames(BinaryReader br, SprFile spr, int count, Color[] palette, bool rleEncoded)
        {
            for (int i = 0; i < count; i++)
            {
                ushort w = br.ReadUInt16();
                ushort h = br.ReadUInt16();

                if (w == 0 || h == 0)
                {
                    spr.Frames.Add(new SprFrame(i, w, h, CreateEmpty(1, 1)));
                    continue;
                }

                byte[] indices = rleEncoded ? DecodeRle(br, w * h) : br.ReadBytes(w * h);
                var bmp = IndexedToBitmap(indices, w, h, palette);
                spr.Frames.Add(new SprFrame(i, w, h, bmp));
            }
        }

        private static byte[] DecodeRle(BinaryReader br, int pixelCount)
        {
            var result = new byte[pixelCount];
            int pos = 0;
            ushort encodedLen = br.ReadUInt16();
            long end = br.BaseStream.Position + encodedLen;

            while (pos < pixelCount && br.BaseStream.Position < end)
            {
                byte b = br.ReadByte();
                if (b != 0)
                {
                    if (pos < pixelCount) result[pos++] = b;
                }
                else
                {
                    byte count = br.ReadByte();
                    if (count == 0)
                    {
                        if (pos < pixelCount) result[pos++] = 0;
                    }
                    else
                    {
                        for (int k = 0; k < count && pos < pixelCount; k++)
                            result[pos++] = 0;
                    }
                }
            }
            br.BaseStream.Seek(end, SeekOrigin.Begin);
            return result;
        }

        private static Bitmap IndexedToBitmap(byte[] indices, int w, int h, Color[] palette)
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int[] pixels = new int[w * h];
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = palette[indices[i]];
                pixels[i] = (c.A << 24) | (c.R << 16) | (c.G << 8) | c.B;
            }
            try { Marshal.Copy(pixels, 0, data.Scan0, pixels.Length); }
            finally { bmp.UnlockBits(data); }
            return bmp;
        }

        private static void ReadRgbaFrames(BinaryReader br, SprFile spr, int count)
        {
            for (int i = 0; i < count; i++)
            {
                ushort w = br.ReadUInt16();
                ushort h = br.ReadUInt16();

                if (w == 0 || h == 0)
                {
                    int idx = spr.IndexedFrameCount + i;
                    spr.Frames.Add(new SprFrame(idx, w, h, CreateEmpty(1, 1)));
                    continue;
                }

                byte[] raw = br.ReadBytes(w * h * 4);
                var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                int[] pixels = new int[w * h];
                for (int p = 0; p < pixels.Length; p++)
                {
                    int s = p * 4;
                    byte b2 = raw[s]; byte g2 = raw[s+1];
                    byte r2 = raw[s+2]; byte a2 = raw[s+3];
                    pixels[p] = (a2 << 24) | (r2 << 16) | (g2 << 8) | b2;
                }
                try { Marshal.Copy(pixels, 0, data.Scan0, pixels.Length); }
                finally { bmp.UnlockBits(data); }

                int frameIdx = spr.IndexedFrameCount + i;
                spr.Frames.Add(new SprFrame(frameIdx, w, h, bmp));
            }
        }

        private static void SkipFrames(BinaryReader br, int indexedCount, int rgbaCount, float version)
        {
            for (int i = 0; i < indexedCount; i++)
            {
                ushort w = br.ReadUInt16();
                ushort h = br.ReadUInt16();
                if (w == 0 || h == 0) continue;

                if (version >= 1.2f)
                {
                    ushort encodedLen = br.ReadUInt16();
                    br.BaseStream.Seek(encodedLen, SeekOrigin.Current);
                }
                else
                {
                    br.BaseStream.Seek(w * h, SeekOrigin.Current);
                }
            }
            for (int i = 0; i < rgbaCount; i++)
            {
                ushort w = br.ReadUInt16();
                ushort h = br.ReadUInt16();
                if (w > 0 && h > 0)
                    br.BaseStream.Seek(w * h * 4, SeekOrigin.Current);
            }
        }

        private static Bitmap CreateEmpty(int w, int h) => new Bitmap(w, h, PixelFormat.Format32bppArgb);
    }

    public class SprFrame : IDisposable
    {
        public int    Index  { get; }
        public int    Width  { get; }
        public int    Height { get; }
        public Bitmap Image  { get; }

        private bool _disposed;

        public SprFrame(int index, int w, int h, Bitmap img)
        {
            Index = index; Width = w; Height = h; Image = img;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Image?.Dispose();
        }
    }
}
