using System;
using System.IO;
using System.IO.Compression;

namespace DuckBot.Core.Services
{
    public static class BackupService
    {
        public static void CreateBackup(string targetPath)
        {
            var directory = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("Invalid backup path", nameof(targetPath));

            Directory.CreateDirectory(directory);
            if (File.Exists(targetPath)) File.Delete(targetPath);

            using var archive = ZipFile.Open(targetPath, ZipArchiveMode.Create);
            AddDirectoryToArchive(archive, Path.Combine(AppContext.BaseDirectory, "data"), "data");
            AddDirectoryToArchive(archive, Path.Combine(AppContext.BaseDirectory, "Games"), "Games");
        }

        public static void RestoreBackup(string zipPath, bool hardRestore)
        {
            if (!File.Exists(zipPath)) return;

            string dataDir = Path.Combine(AppContext.BaseDirectory, "data");
            string gamesDir = Path.Combine(AppContext.BaseDirectory, "Games");

            if (hardRestore)
            {
                if (Directory.Exists(dataDir)) Directory.Delete(dataDir, recursive: true);
                if (Directory.Exists(gamesDir)) Directory.Delete(gamesDir, recursive: true);
            }

            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue; // directory entry

                string destination = Path.Combine(AppContext.BaseDirectory, entry.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                entry.ExtractToFile(destination, overwrite: true);
            }
        }

        public static void OpenBackupFolder(string backupDir)
        {
            Directory.CreateDirectory(backupDir);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = Path.GetFullPath(backupDir),
                UseShellExecute = true
            });
        }

        private static void AddDirectoryToArchive(ZipArchive archive, string sourceDir, string prefix)
        {
            if (!Directory.Exists(sourceDir)) return;

            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(sourceDir, file);
                var entryPath = Path.Combine(prefix, relative).Replace('\\', '/');
                archive.CreateEntryFromFile(file, entryPath, CompressionLevel.Optimal);
            }
        }
    }
}