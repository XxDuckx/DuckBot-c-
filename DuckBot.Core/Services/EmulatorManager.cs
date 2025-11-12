using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DuckBot.Core.Emu;

namespace DuckBot.Core.Services
{
    public static class EmulatorManager
    {
        public sealed record EmulatorInstall(string DisplayName, string RootPath, string ConsolePath, string AdbPath);

        public sealed record EmulatorInstance(
            string Name,
            int Index,
            string? AdbId,
            bool IsRunning,
            EmulatorInstall Install)
        {
            public override string ToString() => $"{Name} ({Install.DisplayName})";
        }

        private static readonly object _lock = new();
        private static readonly List<EmulatorInstall> _installs = new();
        private static readonly List<EmulatorInstance> _instances = new();
        private static DateTime _lastRefresh = DateTime.MinValue;
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(15);

        public static IReadOnlyList<EmulatorInstance> Instances
        {
            get
            {
                EnsureFresh();
                lock (_lock)
                {
                    return _instances.ToList();
                }
            }
        }

        public static void Refresh()
        {
            lock (_lock)
            {
                _installs.Clear();
                _instances.Clear();

                foreach (var path in LdPlayerDetector.DetectInstallPaths())
                {
                    var install = CreateInstall(path);
                    if (install == null) continue;
                    _installs.Add(install);
                    foreach (var instance in QueryInstances(install))
                    {
                        _instances.Add(instance);
                    }
                }

                _lastRefresh = DateTime.UtcNow;
            }
        }

        public static List<string> GetInstanceNames()
            => Instances.Select(i => i.Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();

        public static bool TryGetInstance(string instanceName, out EmulatorInstance instance)
        {
            instance = null!;
            if (string.IsNullOrWhiteSpace(instanceName)) return false;

            EnsureFresh();
            lock (_lock)
            {
                instance = _instances.FirstOrDefault(i => string.Equals(i.Name, instanceName, StringComparison.OrdinalIgnoreCase));
                return instance != null;
            }
        }

        public static bool TryLaunch(string instanceName)
            => TryWithInstance(instanceName, inst => ExecuteConsole(inst.Install, $"launch --name \"{inst.Name}\""));

        public static bool TryStop(string instanceName)
            => TryWithInstance(instanceName, inst => ExecuteConsole(inst.Install, $"quit --name \"{inst.Name}\""));

        public static bool TryFocus(string instanceName)
        {
            return TryWithInstance(instanceName, inst =>
            {
                if (string.IsNullOrWhiteSpace(inst.AdbId)) return false;
                RunAdb(inst, "shell input keyevent 224", out _, out _);
                RunAdb(inst, "shell input keyevent 3", out _, out _);
                return true;
            });
        }

        public static bool Tap(string instanceName, int x, int y)
            => TryShell(instanceName, $"input tap {x} {y}");

        public static bool Swipe(string instanceName, int x1, int y1, int x2, int y2, int durationMs)
            => TryShell(instanceName, $"input swipe {x1} {y1} {x2} {y2} {durationMs}");

        public static bool InputText(string instanceName, string text)
        {
            string encoded = EncodeText(text);
            return TryShell(instanceName, $"input text {encoded}");
        }

        public static bool KeyEvent(string instanceName, int keyCode)
            => TryShell(instanceName, $"input keyevent {keyCode}");

        public static bool TryShell(string instanceName, string command)
            => TryWithInstance(instanceName, inst => RunAdb(inst, $"shell {command}", out _, out _));

        public static bool TryExecOut(string instanceName, string command, out byte[] data)
        {
            data = Array.Empty<byte>();
            if (!TryGetInstance(instanceName, out var inst)) return false;
            return RunAdb(inst, command, out _, out data);
        }

        private static void EnsureFresh()
        {
            lock (_lock)
            {
                if ((DateTime.UtcNow - _lastRefresh) < _cacheDuration && _instances.Count > 0) return;
            }
            Refresh();
        }

        private static bool TryWithInstance(string instanceName, Func<EmulatorInstance, bool> action)
        {
            if (!TryGetInstance(instanceName, out var instance))
            {
                LogService.Warn($"Instance '{instanceName}' not found.");
                return false;
            }
            try
            {
                return action(instance);
            }
            catch (Exception ex)
            {
                LogService.Error($"[{instanceName}] operation failed: {ex.Message}");
                return false;
            }
        }

        private static EmulatorInstall? CreateInstall(string path)
        {
            try
            {
                string console = Path.Combine(path, "dnconsole.exe");
                if (!File.Exists(console))
                {
                    console = Path.Combine(path, "ldconsole.exe");
                    if (!File.Exists(console))
                        return null;
                }

                string adb = Path.Combine(path, "adb.exe");
                if (!File.Exists(adb))
                {
                    adb = Path.Combine(path, "adb", "adb.exe");
                    if (!File.Exists(adb))
                        adb = Path.Combine(path, "tools", "adb.exe");
                }

                if (!File.Exists(adb))
                {
                    LogService.Warn($"ADB executable not found for LDPlayer install at '{path}'.");
                    return null;
                }

                string name = path.Contains("LDPlayer9", StringComparison.OrdinalIgnoreCase)
                    ? "LDPlayer 9"
                    : path.Contains("LDPlayer4", StringComparison.OrdinalIgnoreCase)
                        ? "LDPlayer 4"
                        : "LDPlayer";

                return new EmulatorInstall(name, path, console, adb);
            }
            catch (Exception ex)
            {
                LogService.Error($"Failed to register LDPlayer installation '{path}': {ex.Message}");
                return null;
            }
        }

        private static IEnumerable<EmulatorInstance> QueryInstances(EmulatorInstall install)
        {
            var result = new List<EmulatorInstance>();
            try
            {
                var output = ExecuteConsole(install, "list2", captureOutput: true);
                if (string.IsNullOrWhiteSpace(output)) return result;

                foreach (var block in SplitBlocks(output))
                {
                    var map = ParseBlock(block);
                    if (!map.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name)) continue;

                    int index = map.TryGetValue("index", out var idxStr) && int.TryParse(idxStr, out var idx)
                        ? idx
                        : result.Count;
                    bool running = map.TryGetValue("status", out var status) && status is "1" or "running";
                    string? adbId = null;
                    if (map.TryGetValue("adb_id", out var adb)) adbId = adb;
                    else if (map.TryGetValue("adb", out var adb2)) adbId = adb2;
                    else if (map.TryGetValue("port", out var portStr) && int.TryParse(portStr, out var port)) adbId = $"127.0.0.1:{port}";
                    if (string.IsNullOrWhiteSpace(adbId))
                    {
                        adbId = $"127.0.0.1:{5555 + index}";
                    }

                    result.Add(new EmulatorInstance(name.Trim(), index, adbId, running, install));
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"Failed to enumerate LDPlayer instances for '{install.RootPath}': {ex.Message}");
            }

            return result;
        }

        private static string ExecuteConsole(EmulatorInstall install, string arguments, bool captureOutput = false)
        {
            var psi = new ProcessStartInfo
            {
                FileName = install.ConsolePath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = captureOutput,
                RedirectStandardError = captureOutput,
                WorkingDirectory = install.RootPath
            };

            using var proc = Process.Start(psi);
            if (proc == null) throw new InvalidOperationException("Failed to start ldconsole process.");
            string output = captureOutput ? proc.StandardOutput.ReadToEnd() : string.Empty;
            if (captureOutput)
            {
                string error = proc.StandardError.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(error)) LogService.Warn(error.Trim());
            }
            proc.WaitForExit();
            return output;
        }

        private static bool ExecuteConsole(EmulatorInstall install, string arguments)
        {
            try
            {
                ExecuteConsole(install, arguments, captureOutput: false);
                return true;
            }
            catch (Exception ex)
            {
                LogService.Error($"[{install.DisplayName}] console command '{arguments}' failed: {ex.Message}");
                return false;
            }
        }

        private static bool RunAdb(EmulatorInstance instance, string arguments, out string output, out byte[] data)
        {
            output = string.Empty;
            data = Array.Empty<byte>();

            if (string.IsNullOrWhiteSpace(instance.AdbId))
            {
                LogService.Warn($"ADB id unavailable for instance '{instance.Name}'.");
                return false;
            }

            var psi = new ProcessStartInfo
            {
                FileName = instance.Install.AdbPath,
                Arguments = $"-s {instance.AdbId} {arguments}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                WorkingDirectory = instance.Install.RootPath
            };

            try
            {
                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    LogService.Error($"Failed to start ADB process for '{instance.Name}'.");
                    return false;
                }

                using var ms = new MemoryStream();
                proc.StandardOutput.BaseStream.CopyTo(ms);
                data = ms.ToArray();
                output = Encoding.UTF8.GetString(data);

                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (!string.IsNullOrWhiteSpace(error))
                    LogService.Warn($"ADB error for '{instance.Name}': {error.Trim()}\nCommand: {arguments}");

                return proc.ExitCode == 0;
            }
            catch (Exception ex)
            {
                LogService.Error($"ADB execution failed for '{instance.Name}': {ex.Message}");
                return false;
            }
        }

        private static IEnumerable<string> SplitBlocks(string text)
        {
            var sb = new StringBuilder();
            using var reader = new StringReader(text);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line))
                {
                    if (sb.Length > 0)
                    {
                        yield return sb.ToString();
                        sb.Clear();
                    }
                }
                else
                {
                    if (line.StartsWith("--------", StringComparison.Ordinal)) continue;
                    sb.AppendLine(line);
                }
            }
            if (sb.Length > 0) yield return sb.ToString();
        }

        private static Dictionary<string, string> ParseBlock(string block)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using var reader = new StringReader(block);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                int idx = line.IndexOf('=');
                if (idx < 0) idx = line.IndexOf(':');
                if (idx <= 0) continue;
                string key = line[..idx].Trim();
                string value = line[(idx + 1)..].Trim();
                dict[key] = value;
            }
            return dict;
        }

        private static string EncodeText(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var sb = new StringBuilder(value.Length);
            foreach (char ch in value)
            {
                if (ch == ' ')
                    sb.Append("%s");
                else if (char.IsLetterOrDigit(ch))
                    sb.Append(ch);
                else
                    sb.AppendFormat("\\u{0:X4}", (int)ch);
            }
            return sb.ToString();
        }
    }
}
