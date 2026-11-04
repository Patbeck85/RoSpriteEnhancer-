using System.IO;
using System.Text.Json;
using Xunit;

namespace RoSpriteEnhancer.Core.Tests.Unit;

public class PresetSharingServiceTests_Old
{
    [Fact]
    public void GistPayloadSerialization_CreatesValidJson()
    {
        var gistPayload = new
        {
            description = "RoSpriteEnhancer Preset: Test Preset",
            public = true,
            files = new Dictionary<string, object>
            {
                ["Test Preset.json"] = new { content = "{\"test\":true}" }
            }
        };

        var json = JsonSerializer.Serialize(gistPayload, new JsonSerializerOptions { WriteIndented = true });
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("description", out var desc));
        Assert.True(root.TryGetProperty("public", out var isPublic));
        Assert.True(root.TryGetProperty("files", out var files));
        Assert.True(files.TryGetProperty("Test Preset.json", out var file));
        Assert.True(file.TryGetProperty("content", out var content));
        Assert.Equal("{\"test\":true}", content.GetString());
    }

    [Fact]
    public void GistResponseParsing_ExtractsIdAndUrl()
    {
        var json = @"{
            ""id"": ""abc123def456"",
            ""html_url"": ""https://gist.github.com/user/abc123def456"",
            ""files"": {}
        }";
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string gistId = root.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
        string htmlUrl = root.TryGetProperty("html_url", out var url) ? url.GetString() ?? "" : "";

        Assert.Equal("abc123def456", gistId);
        Assert.Equal("https://gist.github.com/user/abc123def456", htmlUrl);
    }

    [Fact]
    public void GistDownloadParsing_ExtractsFileContent()
    {
        var json = @"{
            ""files"": {
                ""mypreset.json"": {
                    ""content"": ""{\"scale\":2,\"algorithm\":\"xbrz\"}""
                }
            }
        }";
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string? content = null;
        if (root.TryGetProperty("files", out var filesEl))
        {
            foreach (var file in filesEl.EnumerateObject())
            {
                if (file.Value.TryGetProperty("content", out var contentEl))
                {
                    content = contentEl.GetString();
                }
            }
        }

        Assert.Equal("{\"scale\":2,\"algorithm\":\"xbrz\"}", content);
    }

    [Fact]
    public void GistListParsing_ExtractsPresetInfos()
    {
        var json = @"[
            {
                ""id"": ""gist1"",
                ""description"": ""My Preset"",
                ""html_url"": ""https://gist.github.com/user/gist1"",
                ""created_at"": ""2024-01-15T10:30:00Z"",
                ""files"": {
                    ""mypreset.json"": { ""content"": ""{}"\" }
                }
            }
        ]";
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var presets = new System.Collections.Generic.List<(string Id, string Name, string Description, string DownloadUrl, DateTime CreatedAt)>();
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                string id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                string description = item.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? "" : "";
                string htmlUrl = item.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() ?? "" : "";
                string createdAtStr = item.TryGetProperty("created_at", out var dateEl) ? dateEl.GetString() ?? "" : "";
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

                presets.Add((id, name, description, htmlUrl, createdAt));
            }
        }

        Assert.Single(presets);
        Assert.Equal("gist1", presets[0].Id);
        Assert.Equal("mypreset.json", presets[0].Name);
        Assert.Equal("My Preset", presets[0].Description);
        Assert.Equal("https://gist.github.com/user/gist1", presets[0].DownloadUrl);
    }
}