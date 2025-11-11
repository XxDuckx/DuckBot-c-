using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DuckBot.Core.Services
{
    public static class RepositoryService
    {
        public record Template(string Name, string Game, string Author, string Url);

        private static readonly HttpClient _httpClient = new();
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
        private const string DefaultManifestUrl = "https://updates.duckbot.app/templates.json";

        public static Task<List<Template>> GetTemplatesAsync(CancellationToken cancellationToken = default) => FetchTemplatesAsync(DefaultManifestUrl, cancellationToken);

        public static List<Template> GetTemplates() => GetTemplatesAsync().GetAwaiter().GetResult();

        public static async Task<List<Template>> FetchTemplatesAsync(string manifestUrl, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(Path.Combine("data", "repository"));
            string cachePath = Path.Combine("data", "repository", "templates.json");

            if (!string.IsNullOrWhiteSpace(manifestUrl))
            {
                try
                {
                    using var response = await _httpClient.GetAsync(manifestUrl, cancellationToken).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                        await File.WriteAllTextAsync(cachePath, json, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch
                {
                    // ignore network errors
                }
            }

            if (File.Exists(cachePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(cachePath, cancellationToken).ConfigureAwait(false);
                    var items = JsonSerializer.Deserialize<List<Template>>(json, _json);
                    if (items is { Count: > 0 })
                        return items;
                }
                catch
                {
                    // ignore cache issues
                }
            }

            return new()
            {
                new Template("Fast Farm", "West Game", "Community", "https://github.com/XxDuckx/DuckBot"),
                new Template("Build Helper", "West Game", "DuckBot", "https://github.com/XxDuckx/DuckBot")
            };
        }

        public static void Contribute(string localPath)
        {
            if (string.IsNullOrWhiteSpace(localPath) || !File.Exists(localPath))
                throw new FileNotFoundException("Template file not found", localPath);

            string destDir = Path.Combine("data", "repository", "contrib");
            Directory.CreateDirectory(destDir);
            string destPath = Path.Combine(destDir, Path.GetFileName(localPath));
            File.Copy(localPath, destPath, overwrite: true);
        }
    }
}