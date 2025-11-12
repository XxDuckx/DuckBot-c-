using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DuckBot.Core.Emu
{
    public sealed class LdPlayerDetector : IEmulatorDetector
    {
        private static readonly string[] KnownInstallPaths =
        {
            @"C:\\LDPlayer9",
            @"C:\\LDPlayer\\LDPlayer9",
            @"C:\\LDPlayer4.0\\LDPlayer",
            @"C:\\LDPlayer\\LDPlayer4",
        };

        public Task<IReadOnlyCollection<string>> DetectInstallPathsAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() => (IReadOnlyCollection<string>)DetectInternal(cancellationToken), cancellationToken);
        }

        private static IReadOnlyCollection<string> DetectInternal(CancellationToken cancellationToken)
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var key in new[] { @"SOFTWARE\\LDPlayer9", @"SOFTWARE\\LDPlayer" })
            {
                cancellationToken.ThrowIfCancellationRequested();
                string? path = TryReadRegistry(key);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    paths.Add(NormalizePath(path));
                }
            }

            foreach (var fallback in KnownInstallPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Directory.Exists(fallback))
                {
                    paths.Add(NormalizePath(fallback));
                }
            }

            return paths.ToList();
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;
            return Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        private static string? TryReadRegistry(string key)
        {
            try
            {
                using var reg = Registry.LocalMachine.OpenSubKey(key);
                return reg?.GetValue("InstallDir")?.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}