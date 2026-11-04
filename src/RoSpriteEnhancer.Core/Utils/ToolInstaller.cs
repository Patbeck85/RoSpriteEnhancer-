using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace RoSpriteEnhancer.Core.Utils
{
    /// <summary>
    /// Zentrale Installation und Erkennung von externen KI-Tools (Waifu2x, RealCUGAN, RealESRGAN, GFPGAN).
    /// Vermeidet Code-Duplikat zwischen MainWindow und SettingsWindow.
    /// </summary>
    public static class ToolInstaller
    {
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

        /// <summary>
        /// Lädt ein KI-Tool automatisch von GitHub herunter, wenn es noch nicht vorhanden ist.
        /// </summary>
        public static async Task EnsureInstalledAsync(string toolName, string downloadUrl, string exeName)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string toolDir = Path.Combine(baseDir, "tools", toolName);
            string exePath = Path.Combine(toolDir, exeName);

            if (File.Exists(exePath)) return;

            Directory.CreateDirectory(toolDir);
            string zipPath = Path.Combine(baseDir, "tools", $"{toolName}.zip");

            using var client = new HttpClient();
            byte[] data = await client.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(zipPath, data);

            Directory.CreateDirectory(toolDir);
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, toolDir, true);
            if (File.Exists(zipPath)) File.Delete(zipPath);

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
        }
    }
}
