using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DuckBot.Core.Models;

namespace DuckBot.Core.Services
{
    public static class UpdateService
    {
        private static readonly HttpClient _httpClient = new();
        private static readonly ConcurrentDictionary<string, UpdateManifest> _cache = new();
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        public static Version CurrentVersion { get; } = typeof(UpdateService).Assembly.GetName().Version ?? new Version(0, 0, 0, 0);

        public static async Task<UpdateManifest?> FetchManifestAsync(string manifestUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(manifestUrl)) return null;
            if (_cache.TryGetValue(manifestUrl, out var cached)) return cached;

            try
            {
                await using var stream = await OpenStreamAsync(manifestUrl, cancellationToken).ConfigureAwait(false);
                var manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
                if (manifest is null) return null;
                if (manifest.PublishedAt == default)
                {
                    // allow ISO8601 string fallback stored in Notes?
                    manifest.PublishedAt = DateTimeOffset.UtcNow;
                }
                _cache[manifestUrl] = manifest;
                PersistManifest(manifestUrl, manifest);
                return manifest;
            }
            catch
            {
                // try load from disk fallback
                if (TryLoadPersisted(manifestUrl, out var manifest))
                    return manifest;
                return null;
            }
        }

        public static async Task<UpdateCheckResult> CheckForUpdatesAsync(string manifestUrl, CancellationToken cancellationToken = default)
        {
            var manifest = await FetchManifestAsync(manifestUrl, cancellationToken).ConfigureAwait(false);
            if (manifest is null)
            {
                return UpdateCheckResult.NoData();
            }

            Version latest = ParseVersion(manifest.Version);
            bool available = latest > CurrentVersion;
            bool supported = string.IsNullOrWhiteSpace(manifest.MinimumVersion) || CurrentVersion >= ParseVersion(manifest.MinimumVersion);
            return new UpdateCheckResult(CurrentVersion, latest, available && supported, manifest, supported);
        }

        public static async Task<string?> DownloadPackageAsync(UpdateManifest manifest, string targetDirectory, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            if (manifest.PackageUrl is null) return null;
            Directory.CreateDirectory(targetDirectory);
            string fileName = Path.GetFileName(new Uri(manifest.PackageUrl).LocalPath);
            if (string.IsNullOrWhiteSpace(fileName)) fileName = $"duckbot_{manifest.Version}.zip";
            string targetPath = Path.Combine(targetDirectory, fileName);

            try
            {
                using var response = await _httpClient.GetAsync(manifest.PackageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength ?? -1L;
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[81920];
                long read = 0;
                int bytes;
                while ((bytes = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await fs.WriteAsync(buffer.AsMemory(0, bytes), cancellationToken).ConfigureAwait(false);
                    read += bytes;
                    if (total > 0)
                    {
                        progress?.Report(read / (double)total);
                    }
                }

                progress?.Report(1);
                return targetPath;
            }
            catch
            {
                if (File.Exists(targetPath)) File.Delete(targetPath);
                throw;
            }
        }

        private static async Task<Stream> OpenStreamAsync(string manifestUrl, CancellationToken cancellationToken)
        {
            if (Uri.TryCreate(manifestUrl, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return await _httpClient.GetStreamAsync(uri, cancellationToken).ConfigureAwait(false);
            }

            string path = manifestUrl.Replace("file://", string.Empty, StringComparison.OrdinalIgnoreCase);
            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(path);
            }
            return File.OpenRead(path);
        }

        private static Version ParseVersion(string? value)
        {
            if (Version.TryParse(value, out var version)) return version;
            return new Version(0, 0, 0, 0);
        }

        private static void PersistManifest(string manifestUrl, UpdateManifest manifest)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine("data", "updates"));
                string fileName = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(manifestUrl))
                    .Replace('+', '-').Replace('/', '_');
                string path = Path.Combine("data", "updates", $"{fileName}.json");
                File.WriteAllText(path, JsonSerializer.Serialize(manifest, _jsonOptions));
            }
            catch
            {
                // ignore persistence failures
            }
        }

        private static bool TryLoadPersisted(string manifestUrl, out UpdateManifest? manifest)
        {
            manifest = null;
            try
            {
                string fileName = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(manifestUrl))
                    .Replace('+', '-').Replace('/', '_');
                string path = Path.Combine("data", "updates", $"{fileName}.json");
                if (!File.Exists(path)) return false;
                var json = File.ReadAllText(path);
                manifest = JsonSerializer.Deserialize<UpdateManifest>(json, _jsonOptions);
                return manifest != null;
            }
            catch
            {
                return false;
            }
        }
    }

    public readonly record struct UpdateCheckResult(Version CurrentVersion, Version LatestVersion, bool UpdateAvailable, UpdateManifest Manifest, bool Supported)
    {
        public static UpdateCheckResult NoData() => new(UpdateService.CurrentVersion, UpdateService.CurrentVersion, false, new UpdateManifest
        {
            Version = UpdateService.CurrentVersion.ToString(),
            Notes = "No manifest available.",
            PublishedAt = DateTimeOffset.UtcNow
        }, true);
    }
}
