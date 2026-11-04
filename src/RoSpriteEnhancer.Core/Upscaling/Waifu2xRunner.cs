using System.Drawing.Imaging;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace RoSpriteEnhancer.Core.Upscaling
{
    public class Waifu2xOptions
    {
        public string ExecutablePath { get; set; } = "";
        public string ModelPath { get; set; } = "models-cunet";
        public int NoiseLevel { get; set; } = -1; // Standard: -1 (Knackig scharfe Kanten)
        public int ScaleFactor { get; set; } = 2;
        public int TimeoutSeconds { get; set; } = 60;
        public bool ForceCpuMode { get; set; } = false;
    }

    public static class Waifu2xRunner
    {
        private static readonly SemaphoreSlim ConcurrencyLimit = new(4, 4);
        public static string LastError { get; private set; } = "";

        public static Bitmap? Upscale(Bitmap src, Waifu2xOptions opts)
        {
            LastError = "";

            // Auto-Detect Executable in local folders if path is empty
            if (string.IsNullOrWhiteSpace(opts.ExecutablePath) || !File.Exists(opts.ExecutablePath))
            {
                string localExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "waifu2x-ncnn-vulkan.exe");
                string toolsExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "waifu2x", "waifu2x-ncnn-vulkan.exe");
                if (File.Exists(localExe)) opts.ExecutablePath = localExe;
                else if (File.Exists(toolsExe)) opts.ExecutablePath = toolsExe;
                else
                {
                    LastError = "waifu2x-ncnn-vulkan.exe nicht gefunden. Bitte Pfad in der UI angeben.";
                    return null;
                }
            }

            string exeDir = Path.GetDirectoryName(opts.ExecutablePath) ?? "";
            string effectiveModelPath = opts.ModelPath;
            string directModelDir = Path.Combine(exeDir, opts.ModelPath);
            string subModelDir = Path.Combine(exeDir, "models", opts.ModelPath);

            if (!Directory.Exists(directModelDir))
            {
                if (Directory.Exists(subModelDir))
                {
                    effectiveModelPath = Path.Combine("models", opts.ModelPath);
                }
                else
                {
                    LastError = $"Modell-Ordner '{opts.ModelPath}' in '{exeDir}' nicht gefunden!";
                    return null;
                }
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "RoSpriteEnhancer_Waifu2x");
            Directory.CreateDirectory(tempDir);
            string id = Guid.NewGuid().ToString("N");
            string inPath  = Path.Combine(tempDir, $"in_{id}.png");
            string outPath = Path.Combine(tempDir, $"out_{id}.png");

            bool acquired = false;
            var errorOutput = new StringBuilder();

            try
            {
                src.Save(inPath, System.Drawing.Imaging.ImageFormat.Png);

                acquired = ConcurrencyLimit.Wait(TimeSpan.FromSeconds(opts.TimeoutSeconds + 10));
                if (!acquired)
                {
                    LastError = "Waifu2x Timeout (GPU blockiert).";
                    return null;
                }

                string gpuArg = opts.ForceCpuMode ? "-g -1 " : "";
                string args = $"-i \"{inPath}\" -o \"{outPath}\" -n {opts.NoiseLevel} -s {opts.ScaleFactor} -m \"{effectiveModelPath}\" {gpuArg}";

                var psi = new ProcessStartInfo
                {
                    FileName = opts.ExecutablePath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = exeDir,
                };

                using var proc = new Process { StartInfo = psi };
                proc.ErrorDataReceived += (_, e) => { if (e.Data != null) errorOutput.AppendLine(e.Data); };
                proc.OutputDataReceived += (_, e) => { if (e.Data != null) errorOutput.AppendLine(e.Data); };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                bool exited = proc.WaitForExit(opts.TimeoutSeconds * 1000);
                if (!exited)
                {
                    try { proc.Kill(entireProcessTree: true); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Waifu2xRunner] Kill: {ex.Message}"); }
                    LastError = "Waifu2x Prozess reagierte nicht (Timeout).";
                    return null;
                }

                if (proc.ExitCode != 0)
                {
                    LastError = $"Waifu2x Exit-Code {proc.ExitCode}: {errorOutput.ToString().Trim()}";
                    return null;
                }

                if (!File.Exists(outPath))
                {
                    LastError = $"Waifu2x hat keine Ausgabedatei erzeugt: {errorOutput.ToString().Trim()}";
                    return null;
                }

                using (var loadedRaw = new Bitmap(outPath))
{
    // STRIKTES 32-BIT ARGB CASTING (Verhindert CS-Pixelformat-Versatz & Streifen)
    Bitmap dst32 = new Bitmap(loadedRaw.Width, loadedRaw.Height, PixelFormat.Format32bppArgb);
    using (var g = Graphics.FromImage(dst32))
    {
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(loadedRaw, 0, 0, loadedRaw.Width, loadedRaw.Height);
    }
    return dst32;
}
            }
            catch (Exception ex)
            {
                LastError = $"Waifu2x Fehler: {ex.Message}";
                return null;
            }
            finally
            {
                if (acquired) ConcurrencyLimit.Release();
                TryDelete(inPath);
                TryDelete(outPath);
            }
        }

        private static void TryDelete(string path)
                {
                    try { if (File.Exists(path)) File.Delete(path); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Waifu2xRunner] Cleanup {path}: {ex.Message}"); }
                }
            }
        }


