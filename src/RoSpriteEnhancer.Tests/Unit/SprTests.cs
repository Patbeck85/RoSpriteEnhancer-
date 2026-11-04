using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit
{
    public class SprTests
    {
        [Fact]
        public void SprFile_Load_InvalidMagic_ThrowsInvalidDataException()
        {
            string testPath = Path.Combine(Path.GetTempPath(), "invalid_" + Guid.NewGuid().ToString("N") + ".spr");
            try
            {
                using (var bw = new BinaryWriter(File.Create(testPath)))
                {
                    bw.Write(new byte[] { 0x58, 0x59 });
                    bw.Write(new byte[10]);
                }

                Assert.Throws<InvalidDataException>(() => RoSpriteEnhancer.Core.IO.SprFile.Load(testPath));
            }
            finally
            {
                if (File.Exists(testPath)) File.Delete(testPath);
            }
        }

        [Fact]
        public void SprWriter_WriteAndLoad_32BitRgbaRoundtrip_Success()
        {
            string testPath = Path.Combine(Path.GetTempPath(), "roundtrip_" + Guid.NewGuid().ToString("N") + ".spr");
            try
            {
                var frames = new List<Bitmap>();
                using (var b1 = new Bitmap(16, 16, PixelFormat.Format32bppArgb))
                using (var b2 = new Bitmap(16, 16, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(b1)) g.Clear(Color.Red);
                    using (Graphics g = Graphics.FromImage(b2)) g.Clear(Color.Blue);

                    frames.Add(b1);
                    frames.Add(b2);

                    RoSpriteEnhancer.Core.IO.SprWriter.Write(testPath, frames, indexedCount: 0, rgbaCount: 2);
                }

                using var spr = RoSpriteEnhancer.Core.IO.SprFile.Load(testPath);

                Assert.Equal(2, spr.TotalFrameCount);
                Assert.Equal(0, spr.IndexedFrameCount);
                Assert.Equal(2, spr.RgbaFrameCount);
                Assert.Equal(16, spr.Frames[0].Width);
                Assert.Equal(16, spr.Frames[0].Height);
            }
            finally
            {
                if (File.Exists(testPath)) File.Delete(testPath);
            }
        }
    }
}