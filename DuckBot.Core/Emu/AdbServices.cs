using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DuckBot.Core.Emu
{
    public static class AdbService
    {
        private static readonly List<string> _instances = new();
        private static List<string> _paths = new();

        public static void Refresh()
        {
            _paths = LdPlayerDetector.DetectInstallPaths();
            _instances.Clear();
            foreach (var path in _paths)
                foreach (var inst in LdPlayerDetector.GetInstances(path))
                    _instances.Add(inst);
        }

        public static List<string> ListInstances()
        {
            if (_instances.Count == 0) Refresh();
            return _instances.ToList();
        }

        public static bool LaunchInstance(string instance)
        {
            var dnconsole = FindConsoleFor(instance);
            if (dnconsole == null) return false;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = dnconsole,
                    Arguments = $"launch --name {instance}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool StopInstance(string instance)
        {
            var dnconsole = FindConsoleFor(instance);
            if (dnconsole == null) return false;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = dnconsole,
                    Arguments = $"quit --name {instance}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string? FindConsoleFor(string instance)
        {
            foreach (var path in _paths)
            {
                var exe = Path.Combine(path, "dnconsole.exe");
                if (File.Exists(exe)) return exe;
            }
            return null;
        }
    }
}
