using System.IO;
using System.IO.Compression;
using System.Text;

namespace RoSpriteEnhancer.Core.IO
{
    public class GrfArchive : IDisposable
    {
        static GrfArchive()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private static readonly byte[] Magic =
            Encoding.ASCII.GetBytes("Master of Magic\0");

        public string FilePath { get; }
        public IReadOnlyList<GrfEntry> Entries => _entries;
        public int FileCount => _entries.Count;

        private readonly List<GrfEntry> _entries = new();
        private bool _disposed;

        private GrfArchive(string path) { FilePath = path; }

        public static GrfArchive Open(string path)
        {
            var grf = new GrfArchive(path);
            grf.ReadHeader();
            return grf;
        }

        private void ReadHeader()
        {
            using var br = new BinaryReader(File.OpenRead(FilePath), Encoding.ASCII, false);

            byte[] magic = br.ReadBytes(16);
            for (int i = 0; i < 16; i++)
                if (magic[i] != Magic[i])
                    throw new InvalidDataException("Keine gültige GRF-Datei (Magic 'Master of Magic' fehlt).");

            br.ReadBytes(14);

            uint fileTableOffset = br.ReadUInt32();
            uint seed            = br.ReadUInt32();
            uint fileCountRaw    = br.ReadUInt32();
            uint version         = br.ReadUInt32();

            if (version != 0x0200)
                throw new NotSupportedException($"GRF Version 0x{version:X4} wird nicht unterstützt (nur 0x0200).");

            uint fileCount = seed <= fileCountRaw ? fileCountRaw - seed : fileCountRaw;

            long tablePos = 46 + fileTableOffset;
            br.BaseStream.Seek(tablePos, SeekOrigin.Begin);

            uint compressedTableSize   = br.ReadUInt32();
            uint uncompressedTableSize = br.ReadUInt32();

            byte[] compressedTable = br.ReadBytes((int)compressedTableSize);
            byte[] tableData       = ZlibDecompress(compressedTable, (int)uncompressedTableSize);

            using var ms = new MemoryStream(tableData);
            using var tr = new BinaryReader(ms);

            for (int i = 0; i < fileCount; i++)
            {
                if (ms.Position >= ms.Length) break;

                string filename = ReadNullTerminatedString(tr);
                uint compAligned = tr.ReadUInt32();
                uint comp        = tr.ReadUInt32();
                uint orig        = tr.ReadUInt32();
                byte type        = tr.ReadByte();
                uint offset      = tr.ReadUInt32();

                if (type == 0x01 && !string.IsNullOrEmpty(filename))
                {
                    _entries.Add(new GrfEntry(filename, compAligned, comp, orig, type, offset));
                }
            }
        }

        public Dictionary<string, int> GetSpriteCounts()
        {
            var paths = _entries
                .Where(e => IsSpriteFile(e.FileName))
                .Select(e => e.FileName);
            return SpriteFilter.CountByCategory(paths);
        }

        public int ExtractSprites(string outputFolder,
            IEnumerable<string>? categoryFilter = null,
            IProgress<(int current, int total, string file)>? progress = null,
            CancellationToken ct = default)
        {
            var selectedIds = categoryFilter?.ToList() ?? new List<string>();

            var spriteEntries = _entries
                .Where(e => IsSpriteFile(e.FileName))
                .Where(e => selectedIds.Count == 0 || SpriteFilter.Matches(e.FileName, selectedIds))
                .ToList();

            int count = 0;
            using var fs = File.OpenRead(FilePath);

            for (int i = 0; i < spriteEntries.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var entry = spriteEntries[i];
                progress?.Report((i + 1, spriteEntries.Count, entry.FileName));

                try
                {
                    byte[] data = ExtractEntryData(fs, entry);

                    string relPath = entry.FileName
                        .Replace('\\', Path.DirectorySeparatorChar)
                        .TrimStart(Path.DirectorySeparatorChar);

                    string outPath = Path.Combine(outputFolder, relPath);
                    string? outDir = Path.GetDirectoryName(outPath);
                    if (!string.IsNullOrEmpty(outDir))
                        Directory.CreateDirectory(outDir);

                    File.WriteAllBytes(outPath, data);
                    count++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GrfArchive] Extraktionsfehler für '{entry.FileName}': {ex.Message}");
                }
            }

            return count;
        }

        public byte[] ExtractEntry(GrfEntry entry)
        {
            using var fs = File.OpenRead(FilePath);
            return ExtractEntryData(fs, entry);
        }

        private static byte[] ExtractEntryData(FileStream fs, GrfEntry entry)
        {
            fs.Seek(46 + entry.Offset, SeekOrigin.Begin);
            byte[] compressed = new byte[entry.CompressedSizeAligned];
            int read = fs.Read(compressed, 0, compressed.Length);
            if (read < entry.CompressedSize)
                throw new EndOfStreamException($"Unerwartetes Dateiende beim Lesen von '{entry.FileName}'.");

            return ZlibDecompress(compressed[..(int)entry.CompressedSize], (int)entry.OriginalSize);
        }

        public static void PackFolder(string sourceFolder, string outputGrfPath,
            IProgress<(int current, int total, string file)>? progress = null,
            CancellationToken ct = default)
        {
            var files = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories)
                .OrderBy(f => f)
                .ToList();

            try
            {
                using var fs = new FileStream(outputGrfPath, FileMode.Create, FileAccess.Write);
                using var bw = new BinaryWriter(fs);

                bw.Write(Magic);
                bw.Write(new byte[14]);
                bw.Write((uint)0); bw.Write((uint)0); bw.Write((uint)0);
                bw.Write((uint)0x0200);

                using var tableStream = new MemoryStream();
                using var tableWriter = new BinaryWriter(tableStream, System.Text.Encoding.UTF8, leaveOpen: true);

                int count = 0;
                for (int i = 0; i < files.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    string file    = files[i];
                    string relPath = Path.GetRelativePath(sourceFolder, file)
                        .Replace(Path.DirectorySeparatorChar, '\\');
                    progress?.Report((i + 1, files.Count, relPath));

                    byte[] original   = File.ReadAllBytes(file);
                    byte[] compressed = ZlibCompress(original);

                    int aligned = (compressed.Length + 7) & ~7;
                    byte[] paddedCompressed = new byte[aligned];
                    Array.Copy(compressed, paddedCompressed, compressed.Length);

                    uint offset = (uint)(fs.Position - 46);
                    bw.Write(paddedCompressed);

                    byte[] filenameBytes = Encoding.GetEncoding("EUC-KR").GetBytes(relPath);
                    tableWriter.Write(filenameBytes);
                    tableWriter.Write((byte)0);
                    tableWriter.Write((uint)aligned);
                    tableWriter.Write((uint)compressed.Length);
                    tableWriter.Write((uint)original.Length);
                    tableWriter.Write((byte)0x01);
                    tableWriter.Write(offset);
                    count++;
                }

                uint fileTableOffset = (uint)(fs.Position - 46);
                byte[] tableData       = tableStream.ToArray();
                byte[] compressedTable = ZlibCompress(tableData);

                bw.Write((uint)compressedTable.Length);
                bw.Write((uint)tableData.Length);
                bw.Write(compressedTable);

                fs.Seek(30, SeekOrigin.Begin);
                bw.Write(fileTableOffset);
                bw.Write((uint)0);
                bw.Write((uint)count);
                bw.Write((uint)0x0200);
            }
            catch
            {
                try { if (File.Exists(outputGrfPath)) File.Delete(outputGrfPath); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[GrfArchive] Cleanup: {ex.Message}"); }
                throw;
            }
        }

        public static void MergeWithFolder(string baseGrfPath, string patchFolder,
            string outputGrfPath,
            IProgress<(int current, int total, string file)>? progress = null,
            CancellationToken ct = default)
        {
            using var baseGrf = Open(baseGrfPath);

            var patchFiles = Directory.Exists(patchFolder)
                ? Directory.GetFiles(patchFolder, "*.*", SearchOption.AllDirectories)
                    .ToDictionary(
                        f => Path.GetRelativePath(patchFolder, f).Replace(Path.DirectorySeparatorChar, '\\'),
                        f => f,
                        StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>();

            try
            {
                using var outFs = new FileStream(outputGrfPath, FileMode.Create, FileAccess.Write);
                using var bw    = new BinaryWriter(outFs);

                bw.Write(Magic);
                bw.Write(new byte[14]);
                bw.Write((uint)0); bw.Write((uint)0); bw.Write((uint)0);
                bw.Write((uint)0x0200);

                using var tableStream = new MemoryStream();
                using var tableWriter = new BinaryWriter(tableStream, System.Text.Encoding.UTF8, leaveOpen: true);

                using var baseFs = File.OpenRead(baseGrfPath);
                int count = 0;
                int total = baseGrf.Entries.Count + patchFiles.Count;

                foreach (var entry in baseGrf.Entries)
                {
                    ct.ThrowIfCancellationRequested();
                    progress?.Report((count + 1, total, entry.FileName));

                    byte[] data;
                    if (patchFiles.TryGetValue(entry.FileName, out string? patchPath))
                    {
                        data = File.ReadAllBytes(patchPath);
                        patchFiles.Remove(entry.FileName);
                    }
                    else
                    {
                        data = ExtractEntryData(baseFs, entry);
                    }

                    WriteEntry(bw, tableWriter, entry.FileName, data, outFs);
                    count++;
                }

                foreach (var (relPath, patchPath) in patchFiles)
                {
                    ct.ThrowIfCancellationRequested();
                    progress?.Report((count + 1, total, relPath));
                    byte[] data = File.ReadAllBytes(patchPath);
                    WriteEntry(bw, tableWriter, relPath, data, outFs);
                    count++;
                }

                uint fileTableOffset = (uint)(outFs.Position - 46);
                byte[] tableData       = tableStream.ToArray();
                byte[] compressedTable = ZlibCompress(tableData);
                bw.Write((uint)compressedTable.Length);
                bw.Write((uint)tableData.Length);
                bw.Write(compressedTable);

                outFs.Seek(30, SeekOrigin.Begin);
                bw.Write(fileTableOffset);
                bw.Write((uint)0);
                bw.Write((uint)count);
                bw.Write((uint)0x0200);
            }
            catch
            {
                try { if (File.Exists(outputGrfPath)) File.Delete(outputGrfPath); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[GrfArchive] Cleanup: {ex.Message}"); }
                throw;
            }
        }

        private static void WriteEntry(BinaryWriter bw, BinaryWriter tableWriter,
            string relPath, byte[] data, FileStream outFs)
        {
            byte[] compressed = ZlibCompress(data);
            int aligned = (compressed.Length + 7) & ~7;
            byte[] padded = new byte[aligned];
            Array.Copy(compressed, padded, compressed.Length);

            uint offset = (uint)(outFs.Position - 46);
            bw.Write(padded);

            byte[] filenameBytes = Encoding.GetEncoding("EUC-KR").GetBytes(relPath);
            tableWriter.Write(filenameBytes);
            tableWriter.Write((byte)0);
            tableWriter.Write((uint)aligned);
            tableWriter.Write((uint)compressed.Length);
            tableWriter.Write((uint)data.Length);
            tableWriter.Write((byte)0x01);
            tableWriter.Write(offset);
        }

        private static bool IsSpriteFile(string name)
        {
            string ext = Path.GetExtension(name).ToLowerInvariant();
            return ext is ".bmp" or ".spr" or ".tga" or ".png";
        }

        private static string ReadNullTerminatedString(BinaryReader br)
        {
            var bytes = new List<byte>(64);
            byte b;
            while (br.BaseStream.Position < br.BaseStream.Length && (b = br.ReadByte()) != 0)
                bytes.Add(b);
            try { return Encoding.GetEncoding("EUC-KR").GetString(bytes.ToArray()); }
            catch { return Encoding.ASCII.GetString(bytes.ToArray()); }
        }

        private static byte[] ZlibDecompress(byte[] data, int expectedSize)
        {
            int offset = (data.Length >= 2 && data[0] == 0x78) ? 2 : 0;
            using var input  = new MemoryStream(data, offset, data.Length - offset);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream(expectedSize);
            deflate.CopyTo(output);
            return output.ToArray();
        }

        private static byte[] ZlibCompress(byte[] data)
        {
            using var output = new MemoryStream();
            output.WriteByte(0x78);
            output.WriteByte(0xDA);
            using (var deflate = new DeflateStream(output, System.IO.Compression.CompressionLevel.SmallestSize, leaveOpen: true))
                deflate.Write(data, 0, data.Length);
            uint adler = Adler32(data);
            output.WriteByte((byte)(adler >> 24));
            output.WriteByte((byte)(adler >> 16));
            output.WriteByte((byte)(adler >>  8));
            output.WriteByte((byte) adler);
            return output.ToArray();
        }

        private static uint Adler32(byte[] data)
        {
            const uint MOD = 65521;
            uint s1 = 1, s2 = 0;
            foreach (byte b in data)
            {
                s1 = (s1 + b)  % MOD;
                s2 = (s2 + s1) % MOD;
            }
            return (s2 << 16) | s1;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _entries.Clear();
        }
    }

    public class GrfEntry
    {
        public string FileName            { get; }
        public uint   CompressedSizeAligned { get; }
        public uint   CompressedSize      { get; }
        public uint   OriginalSize        { get; }
        public byte   Type                { get; }
        public uint   Offset              { get; }

        public GrfEntry(string fileName, uint compAligned, uint comp,
                        uint orig, byte type, uint offset)
        {
            FileName             = fileName;
            CompressedSizeAligned = compAligned;
            CompressedSize       = comp;
            OriginalSize         = orig;
            Type                 = type;
            Offset               = offset;
        }
    }
}

