using System;
using System.IO;
using Xunit;

namespace RoSpriteEnhancer.Tests.Unit
{
    public class ActTests
    {
        [Fact]
        public void TryScaleOffsets_InvalidMagic_ReturnsFalse()
        {
            string inPath = Path.Combine(Path.GetTempPath(), "invalid_" + Guid.NewGuid().ToString("N") + ".act");
            string outPath = Path.Combine(Path.GetTempPath(), "out_" + Guid.NewGuid().ToString("N") + ".act");
            try
            {
                using (var bw = new BinaryWriter(File.Create(inPath)))
                {
                    bw.Write(new byte[] { 0x58, 0x59 });
                    bw.Write(new byte[10]);
                }

                bool success = RoSpriteEnhancer.Core.ActFile.TryScaleOffsets(inPath, outPath, 2.0f);

                Assert.False(success);
                Assert.False(File.Exists(outPath));
            }
            finally
            {
                if (File.Exists(inPath)) File.Delete(inPath);
                if (File.Exists(outPath)) File.Delete(outPath);
            }
        }

        [Fact]
        public void TryScaleOffsets_ValidMinimalAct_ScalesOffsetsCorrectly()
        {
            string inPath = Path.Combine(Path.GetTempPath(), "valid_" + Guid.NewGuid().ToString("N") + ".act");
            string outPath = Path.Combine(Path.GetTempPath(), "out_" + Guid.NewGuid().ToString("N") + ".act");
            try
            {
                using (var bw = new BinaryWriter(File.Create(inPath)))
                {
                    bw.Write(new byte[] { 0x41, 0x43 });
                    bw.Write((byte)0);
                    bw.Write((byte)2);
                    bw.Write(new byte[2]);
                    bw.Write((uint)1);
                    bw.Write((uint)1);

                    for (int i = 0; i < 4; i++) bw.Write(0);
                    bw.Write((uint)1);

                    bw.Write((int)10);
                    bw.Write((int)-20);
                    bw.Write((int)0);
                    bw.Write((int)0);
                    bw.Write((uint)0xFFFFFFFF);
                    bw.Write((float)1.0f);
                    bw.Write((float)1.0f);
                    bw.Write((int)0);
                    bw.Write((int)0);

                    bw.Write((int)0);
                    bw.Write((float)4.0f);
                }

                bool success = RoSpriteEnhancer.Core.ActFile.TryScaleOffsets(inPath, outPath, 2.0f);

                Assert.True(success);
                Assert.True(File.Exists(outPath));

                using (var br = new BinaryReader(File.OpenRead(outPath)))
                {
                    br.BaseStream.Seek(4, SeekOrigin.Begin);
                    br.BaseStream.Seek(2, SeekOrigin.Current);
                    br.ReadUInt32();
                    br.ReadUInt32();
                    br.BaseStream.Seek(16, SeekOrigin.Current);
                    br.ReadUInt32();

                    int scaledX = br.ReadInt32();
                    int scaledY = br.ReadInt32();

                    Assert.Equal(20, scaledX);
                    Assert.Equal(-40, scaledY);
                }
            }
            finally
            {
                if (File.Exists(inPath)) File.Delete(inPath);
                if (File.Exists(outPath)) File.Delete(outPath);
            }
        }
    }
}