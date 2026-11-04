using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace RoSpriteEnhancer.Core.Upscaling
{
    /// <summary>
    /// TASK-1502: RealESRGANRunner – Offline KI-Upscale mit realesrgan-ncnn-vulkan.
    ///
    /// Nutzt das lokale realesrgan-ncnn-vulkan.exe (kein Internet nötig).
    /// Empfohlene Modelle für Anime/Ro-Sprites:
    ///   - realesrgan-x4plus-anime (bester für 2D-Sprites, klare Konturen)
    ///   - realesrgan-x4plus (allgemein, etwas weicher)
    ///
    /// Alle Verarbeitung läuft offline auf der lokalen GPU (Vulkan) oder CPU.
    /// </summary>
    public class RealESRGANOptions
    {
        public string ExecutablePath { get; set; } = "";      // Pfad zu realesrgan-ncnn-vulkan.exe
        public string ModelPath { get; set; } = "realesrgan-x4plus-anime"; // Modell-Ordner oder -name
        public int ScaleFactor { get; set; } = 4;             // 2 oder 4
        public int TileSize { get; set; } = 0;                // 0 = kein Tiling (ganzes Bild auf einmal)
        public int TilePad { get; set; } = 4;                 // Padding zwischen Tiles
        public bool PrePad { get; set; } = false;             // Vorab-Padding
        public bool UpscaleOnly { get; set; } = false;        // Nur Upscale, kein Denoise
        public int TimeoutSeconds { get; set; } = 120;        // Längere Timeout für KI
        public bool UseGpu { get; set; } = true;              // Vulkan GPU nutzen
    }

    public static class RealESRGANRunner
    {
        private static readonly object _lock = new();
        private static int _activeJobs = 0;
        private const int MaxConcurrentJobs = 1; // RealESRGAN ist GPU-lastig, nur 1 Job gleichzeitig

        public static string LastError { get; private set; } = "";

        public static Bitmap? Upscale(Bitmap src, RealESRGANOptions opts)
        {
            LastError = "";

            if (string.IsNullOrWhiteSpace(opts.ExecutablePath) || !File.Exists(opts.ExecutablePath))
            {
                LastError = "RealESRGAN-Executable nicht gefunden. Bitte Pfad zu realesrgan-ncnn-vulkan.exe angeben.";
                return null;
            }

            if (src == null)
            {
                LastError = "Quell-Bitmap ist null.";
                return null;
            }

            // GPU-Lock: RealESRGAN beansprucht die volle GPU, daher nur 1 Job gleichzeitig
            lock (_lock)
            {
                if (_activeJobs >= MaxConcurrentJobs)
                {
                    LastError = "RealESRGAN läuft bereits. Bitte warten.";
                    return null;
                }
                _activeJobs++;
            }

            try
            {
                return UpscaleInternal(src, opts);
            }
            finally
            {
                lock (_lock)
                {
                    _activeJobs--;
                }
            }
        }

        private static Bitmap? UpscaleInternal(Bitmap src, RealESRGANOptions opts)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "RoSpriteEnhancer_RealESRGAN");
            Directory.CreateDirectory(tempDir);
            string id = Guid.NewGuid().ToString("N");
            string inPath = Path.Combine(tempDir, $"in_{id}.png");
            string outPath = Path.Combine(tempDir, $"out_{id}.png");

            try
            {
                // 1. Quellbild als PNG speichern (verlustfrei)
                src.Save(inPath, System.Drawing.Imaging.ImageFormat.Png);

                // 2. Argumente für realesrgan-ncnn-vulkan aufbauen
                // Syntax: realesrgan-ncnn-vulkan.exe -i input -o output -m models -s scale [options]
                var args = new System.Text.StringBuilder();
                args.Append($"-i \"{inPath}\" ");
                args.Append($"-o \"{outPath}\" ");
                args.Append($"-m \"{opts.ModelPath}\" ");
                args.Append($"-s {opts.ScaleFactor} ");

                if (opts.TileSize > 0)
                {
                    args.Append($"-t {opts.TileSize} ");
                }
                if (opts.TilePad > 0)
                {
                    args.Append($"-p {opts.TilePad} ");
                }
                if (opts.PrePad)
                {
                    args.Append("-x ");
                }
                if (opts.UpscaleOnly)
                {
                    args.Append("-n ");
                }
                if (!opts.UseGpu)
                {
                    args.Append("-g 0 "); // GPU deaktivieren
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
                proc.OutputDataReceived += (_, e) => { if (e.Data != null) System.Diagnostics.Debug.WriteLine($"[RealESRGAN] {e.Data}"); };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                bool exited = proc.WaitForExit(opts.TimeoutSeconds * 1000);
                if (!exited)
                {
                    try { proc.Kill(entireProcessTree: true); } catch (Exception ex)
                {
                    Debug.WriteLine($"[RealESRGANRunner] Fehler: {ex.Message}");
                }
                    LastError = "RealESRGAN Timeout (Prozess reagierte nicht).";
                    return null;
                }

                if (proc.ExitCode != 0)
                {
                    LastError = $"RealESRGAN Exit-Code {proc.ExitCode}: {errorOutput.ToString().Trim()}";
                    return null;
                }

                if (!File.Exists(outPath))
                {
                    LastError = $"RealESRGAN hat keine Ausgabedatei erzeugt: {errorOutput.ToString().Trim()}";
                    return null;
                }

                // 3. Ergebnis laden und als 32bpp ARGB zurückgeben
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
                    return dst32;
                }
            }
            catch (Exception ex)
            {
                LastError = $"RealESRGAN Fehler: {ex.Message}";
                return null;
            }
            finally
            {
                try { if (File.Exists(inPath)) File.Delete(inPath); } catch (Exception ex)
                {
                    Debug.WriteLine($"[RealESRGANRunner] Fehler: {ex.Message}");
                }
                try { if (File.Exists(outPath)) File.Delete(outPath); } catch (Exception ex)
                {
                    Debug.WriteLine($"[RealESRGANRunner] Fehler: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Prüft ob ein realesrgan-ncnn-vulkan.exe an einem der gängigen Pfade existiert.
        /// </summary>
        public static string? AutoDetectExecutable()
        {
            string[] searchPaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "realesrgan-ncnn-vulkan.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "realesrgan", "realesrgan-ncnn-vulkan.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "realesrgan-ncnn-vulkan.exe"),
                @"C:\tools\realesrgan-ncnn-vulkan.exe",
                @"C:\Program Files\RealESRGAN\realesrgan-ncnn-vulkan.exe",
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                    return path;
            }
            return null;
        }

        /// <summary>
        /// Prüft ob Modell-Ordner existiert.
        /// </summary>
        public static bool ModelExists(string modelPath)
        {
            if (string.IsNullOrWhiteSpace(modelPath)) return false;
            // Entweder ein Verzeichnis oder eine Datei
            return Directory.Exists(modelPath) || File.Exists(modelPath);
        }
    }
}
