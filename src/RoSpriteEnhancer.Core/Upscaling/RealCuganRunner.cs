using System.Drawing.Imaging;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace RoSpriteEnhancer.Core.Upscaling
{
    public class RealCuganOptions
    {
        public string ExecutablePath { get; set; } = "";
        public string ModelPath { get; set; } = "models-se"; // models-se (schnell) oder models-pro (höchste Qualität)
        public int DenoiseLevel { get; set; } = -1;          // -1 (scharf) bis 3 (glatt)
        public int ScaleFactor { get; set; } = 2;            // 2x oder 4x
        public int TimeoutSeconds { get; set; } = 60;
        public bool ForceCpuMode { get; set; } = false;
    }

    public static class RealCuganRunner
    {
        private static readonly SemaphoreSlim ConcurrencyLimit = new(1, 1);
        public static string LastError { get; private set; } = "";

        public static Bitmap? Upscale(Bitmap src, RealCuganOptions opts)
        {
            LastError = "";

            if (string.IsNullOrWhiteSpace(opts.ExecutablePath) || !File.Exists(opts.ExecutablePath))
            {
                string localExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "realcugan-ncnn-vulkan.exe");
                string toolsExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "realcugan", "realcugan-ncnn-vulkan.exe");
                if (File.Exists(localExe)) opts.ExecutablePath = localExe;
                else if (File.Exists(toolsExe)) opts.ExecutablePath = toolsExe;
                else
                {
                    LastError = "realcugan-ncnn-vulkan.exe nicht gefunden. Bitte Pfad in der UI angeben.";
                    return null;
                }
            }

            string exeDir = Path.GetDirectoryName(opts.ExecutablePath) ?? "";

            string absoluteModelDir = Path.Combine(exeDir, opts.ModelPath);
            if (!Directory.Exists(absoluteModelDir))
            {
                string subDir = Path.Combine(exeDir, "models", opts.ModelPath);
                if (Directory.Exists(subDir)) absoluteModelDir = subDir;
                else
                {
                    var found = Directory.GetDirectories(exeDir, "models*", SearchOption.AllDirectories);
                    if (found.Length > 0) absoluteModelDir = found[0];
                    else
                    {
                        LastError = $"Kein RealCUGAN Modell-Ordner in '{exeDir}' gefunden!";
                        return null;
                    }
                }
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "RoSpriteEnhancer_RealCugan");
            Directory.CreateDirectory(tempDir);
            string id = Guid.NewGuid().ToString("N");
            string inPath  = Path.Combine(tempDir, $"in_{id}.png");
            string outPath = Path.Combine(tempDir, $"out_{id}.png");

            bool acquired = false;

            try
            {
                src.Save(inPath, System.Drawing.Imaging.ImageFormat.Png);

                acquired = ConcurrencyLimit.Wait(TimeSpan.FromSeconds(opts.TimeoutSeconds + 10));
                if (!acquired)
                {
                    LastError = "RealCUGAN Timeout.";
                    return null;
                }

                string gpuArg = opts.ForceCpuMode ? "-g -1 " : "";
                string args = $"-i \"{inPath}\" -o \"{outPath}\" -n {opts.DenoiseLevel} -s {opts.ScaleFactor} -m \"{absoluteModelDir}\" {gpuArg}";

                var psi = new ProcessStartInfo
                {
                    FileName = opts.ExecutablePath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = true,
                    WorkingDirectory = exeDir,
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    LastError = "RealCUGAN Prozess konnte nicht gestartet werden.";
                    return null;
                }

                bool exited = proc.WaitForExit(opts.TimeoutSeconds * 1000);
                if (!exited)
                {
                    try { proc.Kill(entireProcessTree: true); } catch (Exception ex)
                {
                    Debug.WriteLine($"[RealCuganRunner] Fehler: {ex.Message}");
                }
                    LastError = "RealCUGAN Prozess-Timeout.";
                    return null;
                }

                if (!File.Exists(outPath))
                {
                    LastError = "RealCUGAN hat keine Ausgabedatei erzeugt.";
                    return null;
                }

                using (var loadedRaw = new Bitmap(outPath))
{
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
                LastError = $"RealCUGAN Fehler: {ex.Message}";
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
            try { if (File.Exists(path)) File.Delete(path); } catch (Exception ex)
                {
                    Debug.WriteLine($"[RealCuganRunner] Fehler: {ex.Message}");
                }
        }
    }
}


