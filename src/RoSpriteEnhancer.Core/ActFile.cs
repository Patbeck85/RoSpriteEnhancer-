using System.IO;

namespace RoSpriteEnhancer.Core
{
    public static class ActFile
    {
        private static readonly byte[] Magic = { 0x41, 0x43 }; // "AC"

        private const uint MaxActions = 2000;
        private const uint MaxFramesPerAction = 5000;
        private const uint MaxClipsPerFrame = 200;

        public static bool TryScaleOffsets(string inputPath, string outputPath, float scaleFactor)
        {
            if (scaleFactor <= 0f || Math.Abs(scaleFactor - 1f) < 0.001f)
                return false;

            try
            {
                byte[] raw = File.ReadAllBytes(inputPath);
                using var ms    = new MemoryStream(raw);
                using var br    = new BinaryReader(ms);
                using var outMs = new MemoryStream(raw.Length + 512);
                using var bw    = new BinaryWriter(outMs);

                byte[] magic = br.ReadBytes(2);
                if (magic.Length < 2 || magic[0] != Magic[0] || magic[1] != Magic[1])
                    return false;

                byte minor = br.ReadByte();
                byte major = br.ReadByte();
                float version = major + minor / 10f;

                bw.Write(magic); bw.Write(minor); bw.Write(major);
                if (version >= 2.0f) bw.Write(br.ReadBytes(2));

                uint actionCount = br.ReadUInt32();
                if (actionCount > MaxActions) return false;
                bw.Write(actionCount);

                for (uint a = 0; a < actionCount; a++)
                {
                    uint frameCount = br.ReadUInt32();
                    if (frameCount > MaxFramesPerAction) return false;
                    bw.Write(frameCount);

                    for (uint f = 0; f < frameCount; f++)
                    {
                        for (int i = 0; i < 4; i++) bw.Write(br.ReadInt32());

                        uint clipCount = br.ReadUInt32();
                        if (clipCount > MaxClipsPerFrame) return false;
                        bw.Write(clipCount);

                        for (uint c = 0; c < clipCount; c++)
                        {
                            int offX      = br.ReadInt32();
                            int offY      = br.ReadInt32();
                            int spriteIdx = br.ReadInt32();
                            int mirror    = br.ReadInt32();

                            bw.Write((int)Math.Round(offX * scaleFactor));
                            bw.Write((int)Math.Round(offY * scaleFactor));
                            bw.Write(spriteIdx);
                            bw.Write(mirror);

                            if (version >= 2.0f)
                            {
                                bw.Write(br.ReadUInt32());
                                bw.Write(br.ReadSingle());
                                bw.Write(br.ReadSingle());
                                bw.Write(br.ReadInt32());
                                bw.Write(br.ReadInt32());
                            }

                            if (version >= 2.5f)
                            {
                                int w = br.ReadInt32();
                                int h = br.ReadInt32();
                                bw.Write((int)Math.Round(w * scaleFactor));
                                bw.Write((int)Math.Round(h * scaleFactor));
                            }
                        }

                        if (version >= 2.0f) bw.Write(br.ReadInt32());
                    }

                    if (version >= 2.0f) bw.Write(br.ReadSingle());
                }

                long remaining = ms.Length - ms.Position;
                if (remaining > 0) bw.Write(br.ReadBytes((int)remaining));

                File.WriteAllBytes(outputPath, outMs.ToArray());
                return true;
            }
            catch { return false; }
        }

        public static bool TryInterpolateActFrames(string inputPath, string outputPath, float scaleFactor = 1.0f)
        {
            if (!File.Exists(inputPath)) return false;

            try
            {
                byte[] raw = File.ReadAllBytes(inputPath);
                using var ms    = new MemoryStream(raw);
                using var br    = new BinaryReader(ms);
                using var outMs = new MemoryStream(raw.Length * 2);
                using var bw    = new BinaryWriter(outMs);

                byte[] magic = br.ReadBytes(2);
                if (magic[0] != Magic[0] || magic[1] != Magic[1]) return false;

                byte minor = br.ReadByte();
                byte major = br.ReadByte();
                float version = major + minor / 10f;

                bw.Write(magic); bw.Write(minor); bw.Write(major);
                if (version >= 2.0f) bw.Write(br.ReadBytes(2));

                uint actionCount = br.ReadUInt32();
                bw.Write(actionCount);

                for (uint a = 0; a < actionCount; a++)
                {
                    uint frameCount = br.ReadUInt32();
                    bw.Write(frameCount * 2);

                    for (uint f = 0; f < frameCount; f++)
                    {
                        int[] reserved = new int[4];
                        for (int i = 0; i < 4; i++) reserved[i] = br.ReadInt32();
                        uint clipCount = br.ReadUInt32();

                        for (int i = 0; i < 4; i++) bw.Write(reserved[i]);
                        bw.Write(clipCount);

                        for (uint c = 0; c < clipCount; c++)
                        {
                            int offX = br.ReadInt32();
                            int offY = br.ReadInt32();
                            int spriteIdx = br.ReadInt32();
                            int mirror = br.ReadInt32();

                            bw.Write((int)Math.Round(offX * scaleFactor));
                            bw.Write((int)Math.Round(offY * scaleFactor));
                            bw.Write(spriteIdx * 2);
                            bw.Write(mirror);

                            if (version >= 2.0f)
                            {
                                bw.Write(br.ReadUInt32());
                                bw.Write(br.ReadSingle());
                                bw.Write(br.ReadSingle());
                                bw.Write(br.ReadInt32());
                                bw.Write(br.ReadInt32());
                            }
                            if (version >= 2.5f)
                            {
                                bw.Write((int)Math.Round(br.ReadInt32() * scaleFactor));
                                bw.Write((int)Math.Round(br.ReadInt32() * scaleFactor));
                            }
                        }

                        if (version >= 2.0f) bw.Write(br.ReadInt32());
                    }

                    if (version >= 2.0f)
                    {
                        float interval = br.ReadSingle();
                        bw.Write(interval / 2.0f);
                    }
                }

                long remaining = ms.Length - ms.Position;
                if (remaining > 0) bw.Write(br.ReadBytes((int)remaining));

                File.WriteAllBytes(outputPath, outMs.ToArray());
                return true;
            }
            catch { return false; }
        }
    }
}
