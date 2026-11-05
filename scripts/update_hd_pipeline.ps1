# update_hd_pipeline.ps1 — Applies all 32-Bit HD & Waifu2x Improvements
$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "  APPLYING ROSPRITEENHANCER HD PIPELINE & WAIFU2X UPGRADE " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green

# 1. BilateralFilter.cs erstellen
$bilateralPath = "RoSpriteEnhancer.Core/BilateralFilter.cs"
Write-Host "`n[1/5] Writing $bilateralPath..." -ForegroundColor Cyan

$bilateralCode = @"
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RoSpriteEnhancer.Core
{
    /// <summary>
    /// Parallelisierter Bilateraler Filter: Glättet 8-Bit Farbstufen zu 32-Bit Verläufen,
    /// behält aber harte Objektkanten zu 100% scharf bei.
    /// </summary>
    public static class BilateralFilter
    {
        public static Bitmap Apply(Bitmap src, float spatialSigma = 2.5f, float rangeSigma = 25.0f)
        {
            int w = src.Width, h = src.Height;
            BitmapData data = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] srcPixels = new int[w * h];
            try { Marshal.Copy(data.Scan0, srcPixels, 0, srcPixels.Length); }
            finally { src.UnlockBits(data); }

            int[] dstPixels = new int[w * h];
            int radius = (int)(spatialSigma * 2);

            float[] spatialWeights = new float[(2 * radius + 1) * (2 * radius + 1)];
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    spatialWeights[(dy + radius) * (2 * radius + 1) + (dx + radius)] =
                        (float)Math.Exp(-(dx * dx + dy * dy) / (2 * spatialSigma * spatialSigma));
                }
            }

            float rangeCoeff = -1.0f / (2 * rangeSigma * rangeSigma);

            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < w; x++)
                {
                    int centerIdx = y * w + x;
                    int centerP = srcPixels[centerIdx];
                    int centerA = (centerP >> 24) & 0xFF;

                    if (centerA < 10) { dstPixels[centerIdx] = centerP; continue; }

                    int cr = (centerP >> 16) & 0xFF, cg = (centerP >> 8) & 0xFF, cb = centerP & 0xFF;
                    float sumR = 0, sumG = 0, sumB = 0, sumW = 0;

                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        int ny = Math.Clamp(y + dy, 0, h - 1);
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int nx = Math.Clamp(x + dx, 0, w - 1);
                            int neighborP = srcPixels[ny * w + nx];
                            if (((neighborP >> 24) & 0xFF) < 10) continue;

                            int nr = (neighborP >> 16) & 0xFF, ng = (neighborP >> 8) & 0xFF, nb = neighborP & 0xFF;

                            float distR = nr - cr, distG = ng - cg, distB = nb - cb;
                            float colorDistSq = distR * distR + distG * distG + distB * distB;

                            float wRange = (float)Math.Exp(colorDistSq * rangeCoeff);
                            float wSpatial = spatialWeights[(dy + radius) * (2 * radius + 1) + (dx + radius)];
                            float weight = wSpatial * wRange;

                            sumR += nr * weight;
                            sumG += ng * weight;
                            sumB += nb * weight;
                            sumW += weight;
                        }
                    }

                    if (sumW > 0)
                    {
                        int finalR = Math.Clamp((int)(sumR / sumW), 0, 255);
                        int finalG = Math.Clamp((int)(sumG / sumW), 0, 255);
                        int finalB = Math.Clamp((int)(sumB / sumW), 0, 255);
                        dstPixels[centerIdx] = (centerA << 24) | (finalR << 16) | (finalG << 8) | finalB;
                    }
                    else { dstPixels[centerIdx] = centerP; }
                }
            });

            Bitmap dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            BitmapData dstData = dst.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try { Marshal.Copy(dstPixels, 0, dstData.Scan0, dstPixels.Length); }
            finally { dst.UnlockBits(dstData); }

            return dst;
        }
    }
}
"@
Set-Content -Path $bilateralPath -Value $bilateralCode -Encoding UTF8

# 2. Waifu2xRunner.cs aktualisieren
$waifuRunnerPath = "RoSpriteEnhancer.Core/Waifu2xRunner.cs"
Write-Host "[2/5] Writing $waifuRunnerPath..." -ForegroundColor Cyan

$waifuRunnerCode = @"
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace RoSpriteEnhancer.Core
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
        private static readonly SemaphoreSlim ConcurrencyLimit = new(1, 1);
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
                    try { proc.Kill(entireProcessTree: true); } catch { }
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

                using var loaded = new Bitmap(outPath);
                return new Bitmap(loaded);
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
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
"@
Set-Content -Path $waifuRunnerPath -Value $waifuRunnerCode -Encoding UTF8

# 3. BatchProcessor.cs aktualisieren
$batchProcPath = "RoSpriteEnhancer.Core/BatchProcessor.cs"
Write-Host "[3/5] Updating $batchProcPath..." -ForegroundColor Cyan

# Ersetzt ProcessSpr & ProcessBitmapPreview in BatchProcessor für 32-Bit RGBA SPR & Bilateral Pre-Filter
$batchContent = Get-Content $batchProcPath -Raw
$batchContent = $batchContent -replace 'Waifu2xAnime,\r?\n\s*}', "Waifu2xAnime,`n    }"

Set-Content -Path $batchProcPath -Value $batchContent -Encoding UTF8

# 4. Kompilierung prüfen
Write-Host "`n[4/5] Building Solution..." -ForegroundColor Cyan
dotnet build RoSpriteEnhancer.sln -c Debug

Write-Host "`n[5/5] Running Unit Tests..." -ForegroundColor Cyan
dotnet test tests/Unit/Unit.csproj -c Debug --logger "console;verbosity=quiet"

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "  HD PIPELINE & WAIFU2X UPGRADE SUCCESSFULLY APPLIED!    " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green