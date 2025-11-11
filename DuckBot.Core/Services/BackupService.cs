using System.IO;
using System.IO.Compression;

namespace DuckBot.Core.Services
{
    public static class BackupService
    {
        public static void CreateBackup(string targetPath)
        {
            if (File.Exists(targetPath)) File.Delete(targetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            ZipFile.CreateFromDirectory("data", targetPath);
        }

        public static void RestoreBackup(string zipPath)
        {
            if (!File.Exists(zipPath)) return;
            ZipFile.ExtractToDirectory(zipPath, "data", overwriteFiles: true);
        }

        public static void OpenBackupFolder()
        {
            System.Diagnostics.Process.Start("explorer.exe", Path.GetFullPath("data/backups"));
        }
    }
}
