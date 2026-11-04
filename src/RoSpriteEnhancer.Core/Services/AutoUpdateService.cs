using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoSpriteEnhancer.Core.Services
{
    /// <summary>
    /// Ergebnis einer Auto-Update-Prüfung.
    /// </summary>
    public record UpdateCheckResult
    {
        public bool UpdateAvailable { get; init; }
        public string? CurrentVersion { get; init; }
        public string? LatestVersion { get; init; }
        public string? DownloadUrl { get; init; }
        public string? ReleaseNotes { get; init; }
        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// Prüft bei GitHub Releases auf neue Versionen und bietet Download an.
    /// </summary>
    public static class AutoUpdateService
    {
        private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(15) };

        static AutoUpdateService()
        {
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "RoSpriteEnhancer-AutoUpdater");
        }

        /// <summary>
        /// GitHub-Repository für Release-Checks (Owner/Repo).
        /// Konfigurierbar über App-Einstellungen.
        /// </summary>
        public static string GitHubRepo { get; set; } = "YggdrasilStudio/RoSpriteEnhancer";

        /// <summary>
        /// Aktuelle Version der App (aus Assembly).
        /// </summary>
        public static string CurrentVersion { get; set; } = "1.21.0";

        /// <summary>
        /// Prüft ob ein Update verfügbar ist.
        /// </summary>
        public static async Task<UpdateCheckResult> CheckForUpdateAsync(bool includePrerelease = false)
        {
            try
            {
                var url = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";
                if (includePrerelease)
                    url = $"https://api.github.com/repos/{GitHubRepo}/releases";

                var response = await HttpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return new UpdateCheckResult
                    {
                        UpdateAvailable = false,
                        CurrentVersion = CurrentVersion,
                        ErrorMessage = $"GitHub API: {(int)response.StatusCode} {response.ReasonPhrase}"
                    };
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string? latestVersion = null;
                string? downloadUrl = null;
                string? releaseNotes = null;
                bool isPrerelease = false;

                if (root.TryGetProperty("tag_name", out var tagEl))
                    latestVersion = tagEl.GetString();

                if (root.TryGetProperty("prerelease", out var preEl))
                    isPrerelease = preEl.GetBoolean();

                if (includePrerelease && isPrerelease)
                {
                    // Bei Pre-Release: ersten non-draft Release nehmen
                    if (root.TryGetProperty("draft", out var draftEl) && draftEl.GetBoolean())
                    {
                        return new UpdateCheckResult
                        {
                            UpdateAvailable = false,
                            CurrentVersion = CurrentVersion,
                            ErrorMessage = "Nur Draft-Releases verfügbar"
                        };
                    }
                }

                if (root.TryGetProperty("html_url", out var urlEl))
                    downloadUrl = urlEl.GetString();

                if (root.TryGetProperty("body", out var bodyEl))
                    releaseNotes = bodyEl.GetString();

                if (string.IsNullOrEmpty(latestVersion))
                {
                    return new UpdateCheckResult
                    {
                        UpdateAvailable = false,
                        CurrentVersion = CurrentVersion,
                        ErrorMessage = "Keine Versionsinformation im Release gefunden"
                    };
                }

                bool updateAvailable = IsNewerVersion(latestVersion, CurrentVersion);

                return new UpdateCheckResult
                {
                    UpdateAvailable = updateAvailable,
                    CurrentVersion = CurrentVersion,
                    LatestVersion = latestVersion,
                    DownloadUrl = downloadUrl,
                    ReleaseNotes = releaseNotes
                };
            }
            catch (Exception ex)
            {
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    CurrentVersion = CurrentVersion,
                    ErrorMessage = $"Update-Check Fehler: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Vergleicht zwei SemVer-Versionen.
        /// Gibt true zurück, wenn newVersion > currentVersion.
        /// </summary>
        private static bool IsNewerVersion(string newVersion, string currentVersion)
        {
            try
            {
                var newParts = ParseVersion(newVersion);
                var curParts = ParseVersion(currentVersion);

                for (int i = 0; i < Math.Max(newParts.Length, curParts.Length); i++)
                {
                    int nv = i < newParts.Length ? newParts[i] : 0;
                    int cv = i < curParts.Length ? curParts[i] : 0;
                    if (nv > cv) return true;
                    if (nv < cv) return false;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static int[] ParseVersion(string version)
        {
            var clean = version.TrimStart('v');
            var parts = clean.Split('.');
            var result = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                result[i] = int.TryParse(parts[i], out int val) ? val : 0;
            }
            return result;
        }

        /// <summary>
        /// Öffnet die Download-URL im Standard-Browser.
        /// </summary>
        public static void OpenDownloadUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        /// <summary>
        /// Lädt ein Update herunter und speichert es als temporäre Datei.
        /// </summary>
        public static async Task<string?> DownloadUpdateAsync(string downloadUrl, string? targetPath = null)
        {
            if (string.IsNullOrEmpty(downloadUrl)) return null;

            try
            {
                string target = targetPath ?? Path.Combine(
                    Path.GetTempPath(), $"RoSpriteEnhancer_Update_{Guid.NewGuid():N}.exe");

                using var response = await HttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode) return null;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None);
                await contentStream.CopyToAsync(fileStream);

                return target;
            }
            catch { return null; }
        }

        /// <summary>
        /// Installiert ein heruntergeladenes Update durch Ersetzen der aktuellen EXE.
        /// Erfordert Neustart der Anwendung.
        /// </summary>
        public static bool InstallUpdate(string downloadedPath, string? targetExePath = null)
        {
            if (string.IsNullOrEmpty(downloadedPath) || !File.Exists(downloadedPath)) return false;

            try
            {
                string target = targetExePath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(target)) return false;

                // Erstelle ein Batch-Skript zum Ersetzen nach Prozess-Ende
                string batchPath = Path.Combine(Path.GetTempPath(), $"rosprite_update_{Guid.NewGuid():N}.bat");
                var batchLines = new List<string>
                {
                    "@echo off",
                    ":loop",
                    "tasklist /fi \"imagename eq RoSpriteEnhancer.exe\" 2>nul | findstr /i \"RoSpriteEnhancer.exe\" >nul",
                    "if not errorlevel 1 goto loop",
                    $"del /f /q \"{target}\"",
                    $"copy /y \"{downloadedPath}\" \"{target}\"",
                    $"del /f /q \"{downloadedPath}\"",
                    $"del /f /q \"{batchPath}\"",
                    "start \"\" \"{target}\"",
                    "exit"
                };
                File.WriteAllLines(batchPath, batchLines);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = batchPath,
                    UseShellExecute = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });

                return true;
            }
            catch { return false; }
        }
    }
}