using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DuckBot.Core.Emu
{
    public static class LdPlayerDetector
    {
        public static List<string> DetectInstallPaths()
        {
            var paths = new List<string>();

            // LDPlayer 9
            var ld9 = TryGetPath(@"SOFTWARE\LDPlayer9");
            if (!string.IsNullOrEmpty(ld9)) paths.Add(ld9);

            // LDPlayer 5
            var ld5 = TryGetPath(@"SOFTWARE\LDPlayer");
            if (!string.IsNullOrEmpty(ld5)) paths.Add(ld5);

            // Fallbacks
            if (Directory.Exists(@"C:\LDPlayer\LDPlayer9")) paths.Add(@"C:\LDPlayer\LDPlayer9");
            if (Directory.Exists(@"C:\LDPlayer4.0\LDPlayer")) paths.Add(@"C:\LDPlayer4.0\LDPlayer");

            return paths.Distinct().ToList();

            static string? TryGetPath(string key)
            {
                using var reg = Registry.LocalMachine.OpenSubKey(key);
                if (reg == null) return null;
                return reg.GetValue("InstallDir")?.ToString();
            }
        }

        public static List<string> GetInstances(string installPath)
        {
            try
            {
                var exe = Path.Combine(installPath, "dnconsole.exe");
                if (!File.Exists(exe)) return new List<string>();

                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = "list2",
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                var p = Process.Start(psi);
                string? output = p?.StandardOutput.ReadToEnd();
                p?.WaitForExit();

                var lines = output?.Split('\n')
                                   .Select(x => x.Trim())
                                   .Where(x => x.StartsWith("name"))
                                   .ToList() ?? new();
                return lines.Select(x => x.Split('=')[1].Trim()).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
