using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace RoSpriteEnhancer.Core.Upscaling
{
    /// <summary>
    /// TASK-1506: GFPGANRunner – Offline Gesicht & Detail-Restaurierung mit GFPGAN ncnn Vulkan.
    ///
    /// Nutzt das lokale gfpgan-ncnn-vulkan.exe (kein Internet nötig).
    /// Ideal für: Feine Gesichtsdetails, Ornamente, Schmuck, kleine Texturen in Anime-Sprites.
    ///
    /// Verarbeitung läuft offline auf der lokalen GPU (Vulkan) oder CPU.
    /// </summary>
    public class GFGANOptions
    {
        public string ExecutablePath { get; set; } = "";   // Pfad zu gfpgan-ncnn-vulkan.exe
        public string ModelPath { get; set; } = "GFPGANv1.4"; // Modell-Ordner oder -name
        public float Upscale { get; set; } = 2.0f;         // 1.0 (nur Restauration) oder 2.0
        public int TileSize { get; set; } = 0;             // 0 = kein Tiling
        public int TilePad { get; set; } = 4;
        public int TimeoutSeconds { get; set; } = 120;
        public bool UseGpu { get; set; } = true;
        public float BlendStrength { get; set; } = 0.5f;   // Stärke der Restauration (0.0 = Original, 1.0 = Voll)
    }

    public static class GFPGANRunner
    {
        private static readonly object _lock = new();
        private static int _activeJobs = 0;
        private const int MaxConcurrentJobs = 1;

        public static string LastError { get; private set; } = "";

        public static Bitmap? Restore(Bitmap src, GFGANOptions opts)
        {
            LastError = "";

            if (string.IsNullOrWhiteSpace(opts.ExecutablePath) || !File.Exists(opts.ExecutablePath))
            {
                LastError = "GFPGAN-Executable nicht gefunden. Bitte Pfad zu gfpgan-ncnn-vulkan.exe angeben.";
                return null;
            }

            if (src == null)
            {
                LastError = "Quell-Bitmap ist null.";
                return null;
            }

            lock (_lock)
            {
                if (_activeJobs >= MaxConcurrentJobs)
                {
                    LastError = "GFPGAN läuft bereits. Bitte warten.";
                    return null;
                }
                _activeJobs++;
            }

            try
            {
                return RestoreInternal(src, opts);
            }
            finally
            {
                lock (_lock)
                {
                    _activeJobs--;
                }
            }
        }

        private static Bitmap? RestoreInternal(Bitmap src, GFGANOptions opts)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "RoSpriteEnhancer_GFPGAN");
            Directory.CreateDirectory(tempDir);
            string id = Guid.NewGuid().ToString("N");
            string inPath = Path.Combine(tempDir, $"in_{id}.png");
            string outPath = Path.Combine(tempDir, $"out_{id}.png");

            try
            {
                src.Save(inPath, System.Drawing.Imaging.ImageFormat.Png);

                var args = new System.Text.StringBuilder();
                args.Append($"-i \"{inPath}\" ");
                args.Append($"-o \"{outPath}\" ");
                args.Append($"-m \"{opts.ModelPath}\" ");
                args.Append($"-s {opts.Upscale} ");

                if (opts.TileSize > 0)
                {
                    args.Append($"-t {opts.TileSize} ");
                }
                if (opts.TilePad > 0)
                {
                    args.Append($"-p {opts.TilePad} ");
                }
                if (!opts.UseGpu)
                {
                    args.Append("-g 0 ");
                }

                var psi = new ProcessStartInfo
                {
                    FileName = opts.ExecutablePath,
                    Arguments = args.ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = string.IsNullOrEmpty(Path.GetDirectoryName(opts.ExecutablePath))
                        ? tempDir : Path.GetDirectoryName(opts.ExecutablePath)!,
                };

                using var proc = new Process { StartInfo = psi };
                var errorOutput = new System.Text.StringBuilder();
                proc.ErrorDataReceived += (_, e) => { if (e.Data != null) errorOutput.AppendLine(e.Data); };
                proc.OutputDataReceived += (_, e) => { if (e.Data != null) System.Diagnostics.Debug.WriteLine($"[GFPGAN] {e.Data}"); };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                bool exited = proc.WaitForExit(opts.TimeoutSeconds * 1000);
                if (!exited)
                {
                    try { proc.Kill(entireProcessTree: true); } catch (Exception ex)
                {
                    Debug.WriteLine($"[GFPGANRunner] Fehler: {ex.Message}");
                }
                    LastError = "GFPGAN Timeout.";
                    return null;
                }

                if (proc.ExitCode != 0)
                {
                    LastError = $"GFPGAN Exit-Code {proc.ExitCode}: {errorOutput.ToString().Trim()}";
                    return null;
                }

                if (!File.Exists(outPath))
                {
                    LastError = $"GFPGAN hat keine Ausgabedatei erzeugt: {errorOutput.ToString().Trim()}";
                    return null;
                }

                using (var loadedRaw = new Bitmap(outPath))
                {
                    Bitmap dst32 = new Bitmap(loadedRaw.Width, loadedRaw.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (var g = System.Drawing.Graphics.FromImage(dst32))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        g.DrawImage(loadedRaw, 0, 0, loadedRaw.Width, loadedRaw.Height);
                    }

                    // Blend mit Original für kontrollierte Restauration
                    if (opts.BlendStrength < 1.0f && opts.BlendStrength > 0.0f)
                    {
                        using (var blended = BlendImages(src, dst32, opts.BlendStrength))
                        {
                            // Kopieren, da dst32 hier disposed wird und der Aufrufer ein eigenes Bitmap braucht
                            Bitmap result = new Bitmap(blended.Width, blended.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                            using (var g2 = System.Drawing.Graphics.FromImage(result))
                            {
                                g2.DrawImage(blended, 0, 0, blended.Width, blended.Height);
                            }
                            return result;
                        }
                    }

                    return dst32;
                }
            }
            catch (Exception ex)
            {
                LastError = $"GFPGAN Fehler: {ex.Message}";
                return null;
            }
            finally
            {
                try { if (File.Exists(inPath)) File.Delete(inPath); } catch (Exception ex)
                {
                    Debug.WriteLine($"[GFPGANRunner] Fehler: {ex.Message}");
                }
                try { if (File.Exists(outPath)) File.Delete(outPath); } catch (Exception ex)
                {
                    Debug.WriteLine($"[GFPGANRunner] Fehler: {ex.Message}");
                }
            }
        }

        private static Bitmap BlendImages(Bitmap original, Bitmap restored, float blendStrength)
        {
            int w = Math.Min(original.Width, restored.Width);
            int h = Math.Min(original.Height, restored.Height);
            Bitmap result = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var cOrig = original.GetPixel(x, y);
                    var cRest = restored.GetPixel(x, y);
                    int r = (int)(cOrig.R * (1 - blendStrength) + cRest.R * blendStrength);
                    int g = (int)(cOrig.G * (1 - blendStrength) + cRest.G * blendStrength);
                    int b = (int)(cOrig.B * (1 - blendStrength) + cRest.B * blendStrength);
                    int a = cOrig.A;
                    result.SetPixel(x, y, Color.FromArgb(a, r, g, b));
                }
            }
            return result;
        }

        public static string? AutoDetectExecutable()
        {
            string[] searchPaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gfpgan-ncnn-vulkan.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "gfpgan", "gfpgan-ncnn-vulkan.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "gfpgan-ncnn-vulkan.exe"),
                @"C:\tools\gfpgan-ncnn-vulkan.exe",
                @"C:\Program Files\GFPGAN\gfpgan-ncnn-vulkan.exe",
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                    return path;
            }
            return null;
        }

        public static bool ModelExists(string modelPath)
        {
            if (string.IsNullOrWhiteSpace(modelPath)) return false;
            return Directory.Exists(modelPath) || File.Exists(modelPath);
        }
    }
}
