using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace RoSpriteEnhancer.Core.Utils
{
    /// <summary>
    /// Definition eines KI-Tools mit allen Informationen für den automatischen Download.
    /// </summary>
    public class AiToolDefinition
    {
        public string ToolName { get; set; } = "";
        public string ExeName { get; set; } = "";
        public string Subfolder { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string? ModelUrl { get; set; }          // Optional: zusätzliches Modell (z.B. .pth)
        public string? ModelFileName { get; set; }     // Optional: Name der Modell-Datei
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsExecutable { get; set; } = true; // true = .exe, false = nur Modell-Datei
    }

    /// <summary>
    /// Zentrale KI-Tool-Verwaltung mit automatischem Download und Fortschrittsanzeige.
    /// Wird von der UI aufgerufen, wenn ein KI-Modell ausgewählt wird und noch nicht installiert ist.
    /// </summary>
    public static class AiToolManager
    {
        /// <summary>
        /// Event für Fortschrittsmeldungen während des Downloads.
        /// </summary>
        public static event EventHandler<(string ToolName, double Percent, string Status)>? DownloadProgressChanged;

        private static readonly List<AiToolDefinition> _tools = new()
        {
            // ── Waifu2x ──
            new AiToolDefinition
            {
                ToolName = "waifu2x",
                ExeName = "waifu2x-ncnn-vulkan.exe",
                Subfolder = "waifu2x",
                DownloadUrl = "https://github.com/nihui/waifu2x-ncnn-vulkan/releases/download/20220728/waifu2x-ncnn-vulkan-20220728-windows.zip",
                DisplayName = "Waifu2x Anime AI",
                Description = "Anime-Upscaling mit scharfen Kanten (2×/4×)",
                IsExecutable = true
            },
            // ── RealCUGAN ──
            new AiToolDefinition
            {
                ToolName = "realcugan",
                ExeName = "realcugan-ncnn-vulkan.exe",
                Subfolder = "realcugan",
                DownloadUrl = "https://github.com/nihui/realcugan-ncnn-vulkan/releases/download/20220728/realcugan-ncnn-vulkan-20220728-windows.zip",
                DisplayName = "RealCUGAN Anime AI",
                Description = "Profi-Remastering mit satten Farbverläufen (2×/4×)",
                IsExecutable = true
            },
            // ── RealESRGAN ──
            new AiToolDefinition
            {
                ToolName = "realesrgan",
                ExeName = "realesrgan-ncnn-vulkan.exe",
                Subfolder = "realesrgan",
                DownloadUrl = "https://github.com/nihui/realesrgan-ncnn-vulkan/releases/download/20220728/realesrgan-ncnn-vulkan-20220728-windows.zip",
                ModelUrl = "https://github.com/xinntao/Real-ESRGAN/releases/download/v0.1.0/RealESRGAN_x4plus_anime_6B.pth",
                ModelFileName = "realesrgan-x4plus-anime.pth",
                DisplayName = "RealESRGAN Anime AI",
                Description = "Beste Anime-Sprite-Qualität mit Detail-Erhaltung (2×/4×)",
                IsExecutable = true
            },
            // ── GFPGAN ──
            new AiToolDefinition
            {
                ToolName = "gfpgan",
                ExeName = "gfpgan-ncnn-vulkan.exe",
                Subfolder = "gfpgan",
                DownloadUrl = "https://github.com/nihui/gfpgan-ncnn-vulkan/releases/download/20220728/gfpgan-ncnn-vulkan-20220728-windows.zip",
                ModelUrl = "https://github.com/TencentARC/GFPGAN/releases/download/v1.3.0/GFPGANv1.3.pth",
                ModelFileName = "GFPGANv1.3.pth",
                DisplayName = "GFPGAN Gesicht & Haar",
                Description = "Gesichts- und Haar-Restaurierung fuer Details (1x/2x)",
                IsExecutable = true
            }
        };

        /// <summary>
        /// Gibt alle verfügbaren KI-Tool-Definitionen zurück.
        /// </summary>
        public static IReadOnlyList<AiToolDefinition> GetAllTools() => _tools;

        /// <summary>
        /// Gibt eine Tool-Definition anhand des Tool-Namens zurück.
        /// </summary>
        public static AiToolDefinition? GetTool(string toolName)
        {
            foreach (var t in _tools)
                if (t.ToolName.Equals(toolName, StringComparison.OrdinalIgnoreCase))
                    return t;
            return null;
        }

        /// <summary>
        /// Prüft, ob ein KI-Tool (Executable + optionales Modell) installiert ist.
        /// </summary>
        public static bool IsToolInstalled(AiToolDefinition tool)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string toolDir = Path.Combine(baseDir, "tools", tool.Subfolder);
            string exePath = Path.Combine(toolDir, tool.ExeName);

            if (!File.Exists(exePath))
                return false;

            // Wenn ein Modell definiert ist, prüfe auch das
            if (!string.IsNullOrEmpty(tool.ModelUrl) && !string.IsNullOrEmpty(tool.ModelFileName))
            {
                string modelPath = Path.Combine(toolDir, tool.ModelFileName);
                if (!File.Exists(modelPath))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Sucht ein Tool-Executable an gängigen Orten.
        /// </summary>
        public static string? FindExecutable(string toolSubfolder, string exeName)
        {
            if (string.IsNullOrWhiteSpace(toolSubfolder) || string.IsNullOrWhiteSpace(exeName))
                return null;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string currentDir = Directory.GetCurrentDirectory();
            string[] searchBases = new[]
            {
                baseDir, currentDir,
                Path.Combine(baseDir, ".."), Path.Combine(baseDir, "..", ".."),
                Path.Combine(baseDir, "..", "..", ".."), Path.Combine(currentDir, "..")
            };

            foreach (var dir in searchBases)
            {
                try
                {
                    string candidate = Path.GetFullPath(Path.Combine(dir, "tools", toolSubfolder, exeName));
                    if (File.Exists(candidate)) return candidate;
                    string directCandidate = Path.GetFullPath(Path.Combine(dir, exeName));
                    if (File.Exists(directCandidate)) return directCandidate;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ToolInstaller] AutoDetectExecutable Fehler: {ex.Message}");
                }
            }
            return null;
        }

        /// Laedt ein KI-Tool (Executable + optionales Modell) automatisch von GitHub herunter.
        /// Meldet Fortschritt ueber DownloadProgressChanged und IProgress.
        public static async Task EnsureInstalledAsync(AiToolDefinition tool, IProgress<double>? progress = null)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string toolDir = Path.Combine(baseDir, "tools", tool.Subfolder);
            string exePath = Path.Combine(toolDir, tool.ExeName);

            // Bereits installiert?
            if (File.Exists(exePath) &&
                (string.IsNullOrEmpty(tool.ModelUrl) || File.Exists(Path.Combine(toolDir, tool.ModelFileName ?? ""))))
            {
                progress?.Report(100.0);
                DownloadProgressChanged?.Invoke(null, (tool.DisplayName, 100.0, "Bereits installiert"));
                return;
            }

            Directory.CreateDirectory(toolDir);
            string zipPath = Path.Combine(baseDir, "tools", $"{tool.ToolName}.zip");

            try
            {
                // ── 1. Executable herunterladen ──
                DownloadProgressChanged?.Invoke(null, (tool.DisplayName, 0.0, $"Lade {tool.DisplayName}..."));
                progress?.Report(0.0);

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(30);

                // Download mit Fortschritt
                var response = await client.GetAsync(tool.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var downloadedBytes = 0L;

                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    downloadedBytes += bytesRead;

                    if (totalBytes > 0)
                    {
                        var percent = (double)downloadedBytes / totalBytes * 50; // 50% für Download
                        DownloadProgressChanged?.Invoke(null, (tool.DisplayName, percent, $"Lade {tool.DisplayName}... ({percent:F0}%)"));
                        progress?.Report(percent);
                    }
                }

                // ── 2. Extrahieren ──
                DownloadProgressChanged?.Invoke(null, (tool.DisplayName, 55.0, "Entpacke..."));
                progress?.Report(55.0);

                Directory.CreateDirectory(toolDir);
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, toolDir, true);
                if (File.Exists(zipPath)) File.Delete(zipPath);

                // Verschachtelte Ordner flach ziehen
                var subs = Directory.GetDirectories(toolDir);
                if (subs.Length == 1)
                {
                    foreach (var f in Directory.GetFiles(subs[0]))
                        File.Move(f, Path.Combine(toolDir, Path.GetFileName(f)), true);
                    foreach (var d in Directory.GetDirectories(subs[0]))
                    {
                        string targetD = Path.Combine(toolDir, Path.GetFileName(d));
                        if (Directory.Exists(targetD)) Directory.Delete(targetD, true);
                        Directory.Move(d, targetD);
                    }
                    Directory.Delete(subs[0], true);
                }

                DownloadProgressChanged?.Invoke(null, (tool.DisplayName, 70.0, "Executable installiert"));
                progress?.Report(70.0);

                // ── 3. Optionales Modell herunterladen ──
                if (!string.IsNullOrEmpty(tool.ModelUrl) && !string.IsNullOrEmpty(tool.ModelFileName))
                {
                    string modelPath = Path.Combine(toolDir, tool.ModelFileName);
                    if (!File.Exists(modelPath))
                    {
                        DownloadProgressChanged?.Invoke(null, (tool.DisplayName, 75.0, $"Lade Modell ({tool.ModelFileName})..."));
                        progress?.Report(75.0);

                        var modelData = await client.GetByteArrayAsync(tool.ModelUrl);
                        await File.WriteAllBytesAsync(modelPath, modelData);

                        DownloadProgressChanged?.Invoke(null, (tool.DisplayName, 95.0, "Modell installiert"));
                        progress?.Report(95.0);
                    }
                }

                DownloadProgressChanged?.Invoke(null, (tool.DisplayName, 100.0, "Installation abgeschlossen"));
                progress?.Report(100.0);
            }
            catch (Exception ex)
            {
                DownloadProgressChanged?.Invoke(null, (tool.DisplayName, 0.0, $"Fehler: {ex.Message}"));
                throw new Exception($"KI-Tool '{tool.DisplayName}' konnte nicht installiert werden: {ex.Message}", ex);
            }
        }
    }
}
