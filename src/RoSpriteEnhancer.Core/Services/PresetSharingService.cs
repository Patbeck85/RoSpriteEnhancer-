using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RoSpriteEnhancer.Core.Services
{
    /// <summary>
    /// Ergebnis eines Preset-Sharing-Vorgangs.
    /// </summary>
    public record PresetShareResult
    {
        public bool Success { get; init; }
        public string? PresetId { get; init; }
        public string? DownloadUrl { get; init; }
        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// Informationen über ein geteiltes Preset im Repository.
    /// </summary>
    public record PresetShareInfo
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public string Author { get; init; } = "";
        public string DownloadUrl { get; init; } = "";
        public DateTime CreatedAt { get; init; }
        public int DownloadCount { get; init; }
    }

    /// <summary>
    /// Ermöglicht das Teilen von Presets über GitHub Gists.
    /// Presets werden als einzelne Dateien in einem öffentlichen Gist gespeichert.
    /// Ein GitHub Personal Access Token ist für den Upload erforderlich.
    /// </summary>
    public static class PresetSharingService
    {
        private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

        static PresetSharingService()
        {
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "RoSpriteEnhancer-PresetSharing");
        }

        /// <summary>
        /// GitHub Personal Access Token für Gist-Uploads (erforderlich für Upload).
        /// Konfigurierbar über die App-Einstellungen.
        /// </summary>
        public static string? GitHubToken { get; set; }

        /// <summary>
        /// Lädt ein Preset als öffentliches GitHub Gist hoch.
        /// </summary>
        public static async Task<PresetShareResult> UploadPresetAsync(string presetName, string presetJson, string? description = null)
        {
            try
            {
                if (string.IsNullOrEmpty(GitHubToken))
                {
                    return new PresetShareResult
                    {
                        Success = false,
                        ErrorMessage = "GitHub Token nicht konfiguriert. Bitte in den Einstellungen ein GitHub Personal Access Token hinterlegen."
                    };
                }

                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GitHubToken);

                var gistPayload = new
                {
                    description = $"RoSpriteEnhancer Preset: {presetName}" + (string.IsNullOrEmpty(description) ? "" : $" - {description}"),
                    @public = true,
                    files = new Dictionary<string, object>
                    {
                        [$"{presetName}.json"] = new { content = presetJson }
                    }
                };

                var json = JsonSerializer.Serialize(gistPayload, new JsonSerializerOptions { WriteIndented = true });
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpClient.PostAsync("https://api.github.com/gists", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    string gistId = root.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
                    string htmlUrl = root.TryGetProperty("html_url", out var url) ? url.GetString() ?? "" : "";

                    return new PresetShareResult
                    {
                        Success = true,
                        PresetId = gistId,
                        DownloadUrl = htmlUrl
                    };
                }

                var errorBody = await response.Content.ReadAsStringAsync();
                return new PresetShareResult
                {
                    Success = false,
                    ErrorMessage = $"GitHub API-Fehler: {(int)response.StatusCode} {response.ReasonPhrase} - {errorBody}"
                };
            }
            catch (Exception ex)
            {
                return new PresetShareResult
                {
                    Success = false,
                    ErrorMessage = $"Upload-Fehler: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Lädt ein Preset von einem GitHub Gist herunter.
        /// </summary>
        public static async Task<(string? PresetJson, string? ErrorMessage)> DownloadPresetAsync(string presetId)
        {
            try
            {
                var response = await HttpClient.GetAsync($"https://api.github.com/gists/{presetId}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("files", out var filesEl))
                    {
                        foreach (var file in filesEl.EnumerateObject())
                        {
                            if (file.Value.TryGetProperty("content", out var contentEl))
                            {
                                return (contentEl.GetString(), null);
                            }
                        }
                    }

                    return (null, "Keine Preset-Datei im Gist gefunden.");
                }

                return (null, $"GitHub API-Fehler: {(int)response.StatusCode} {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                return (null, $"Download-Fehler: {ex.Message}");
            }
        }

        /// <summary>
        /// Listet öffentliche Gists des authentifizierten Benutzers auf.
        /// Erfordert einen gültigen GitHub Token.
        /// </summary>
        public static async Task<List<PresetShareInfo>> ListAvailablePresetsAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(GitHubToken))
                {
                    return new List<PresetShareInfo>();
                }

                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GitHubToken);

                var response = await HttpClient.GetAsync("https://api.github.com/gists");

                if (!response.IsSuccessStatusCode)
                    return new List<PresetShareInfo>();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var presets = new List<PresetShareInfo>();
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        string id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                        string description = item.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? "" : "";
                        string htmlUrl = item.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() ?? "" : "";
                        string createdAtStr = item.TryGetProperty("created_at", out var dateEl) ? dateEl.GetString() ?? "" : "";

                        // Preset-Name aus der Beschreibung oder Dateinamen extrahieren
                        string name = description;
                        if (item.TryGetProperty("files", out var filesEl2))
                        {
                            foreach (var file in filesEl2.EnumerateObject())
                            {
                                name = file.Name;
                                break;
                            }
                        }

                        DateTime createdAt = DateTime.UtcNow;
                        if (!string.IsNullOrEmpty(createdAtStr) && DateTime.TryParse(createdAtStr, out var parsed))
                            createdAt = parsed;

                        presets.Add(new PresetShareInfo
                        {
                            Id = id,
                            Name = name,
                            Description = description,
                            Author = "GitHub User",
                            DownloadUrl = htmlUrl,
                            CreatedAt = createdAt,
                            DownloadCount = 0
                        });
                    }
                }

                return presets;
            }
            catch
            {
                return new List<PresetShareInfo>();
            }
        }

        /// <summary>
        /// Speichert ein heruntergeladenes Preset lokal im Preset-Ordner.
        /// </summary>
        public static bool SaveDownloadedPreset(string presetJson, string presetName, string presetsDir)
        {
            try
            {
                Directory.CreateDirectory(presetsDir);
                using var doc = JsonDocument.Parse(presetJson);
                var root = doc.RootElement;

                string name = presetName;
                if (root.TryGetProperty("name", out var nameEl))
                    name = nameEl.GetString() ?? presetName;

                string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
                string filePath = Path.Combine(presetsDir, safeName + ".json");

                File.WriteAllText(filePath, presetJson);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}