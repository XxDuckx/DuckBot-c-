using System;

namespace DuckBot.Core.Models
{
    public class UpdateManifest
    {
        public string Version { get; set; } = "0.0.0";
        public string? MinimumVersion { get; set; }
        public string? Notes { get; set; }
        public string? NotesUrl { get; set; }
        public string? PackageUrl { get; set; }
        public DateTimeOffset PublishedAt { get; set; }
        public string? Hash { get; set; }
    }
}
