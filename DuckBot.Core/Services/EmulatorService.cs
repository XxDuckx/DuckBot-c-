using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using DuckBot.Core.Emu;
using DuckBot.Core.Logging;

namespace DuckBot.Core.Services
{
    public sealed class EmulatorService : IEmulatorService
    {
        public sealed record EmulatorInstall(string DisplayName, string RootPath, string ConsolePath, string AdbPath);

        public sealed record EmulatorInstance(string Name, int Index, string? AdbId, bool IsRunning, EmulatorInstall Install)
        {
            public override string ToString() => $"{Name} ({Install.DisplayName})";
        }

        private readonly IEmulatorDetector _detector;
        private readonly IAppLogger _logger;
        private readonly SemaphoreSlim _mutex = new(1, 1);
        private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(15);

        private List<EmulatorInstall> _installs = new();
        private List<EmulatorInstance> _instances = new();
        private DateTime _lastRefresh = DateTime.MinValue;
        private readonly HashSet<string> _missingInstalls = new(StringComparer.OrdinalIgnoreCase);

        public EmulatorService(IEmulatorDetector detector, IAppLogger logger)
        {
            _detector = detector;
            _logger = logger;
        }

        public async Task RefreshAsync(bool force = false, CancellationToken cancellationToken = default)
        {
            await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!force && (DateTime.UtcNow - _lastRefresh) < _cacheDuration && _instances.Count > 0)
                {
                    return;
                }

                _installs = new List<EmulatorInstall>();
                _instances = new List<EmulatorInstance>();

                var paths = await _detector.DetectInstallPathsAsync(cancellationToken).ConfigureAwait(false);
                foreach (var path in paths)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var install = CreateInstall(path);
                    if (install == null)
                    {
                        continue;
                    }

                    _installs.Add(install);
                    var instances = await QueryInstancesAsync(install, cancellationToken).ConfigureAwait(false);
                    _instances.AddRange(instances);
                }

                _lastRefresh = DateTime.UtcNow;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<IReadOnlyList<EmulatorInstanceInfo>> GetInstancesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            await RefreshAsync(forceRefresh, cancellationToken).ConfigureAwait(false);
            await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return _instances.Select(x => new EmulatorInstanceInfo(x.Name, x.Index, x.AdbId, x.IsRunning, x.Install.DisplayName)).ToList();
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<IReadOnlyList<string>> GetInstanceNamesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            var instances = await GetInstancesAsync(forceRefresh, cancellationToken).ConfigureAwait(false);
            return instances.Select(i => i.Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        }

        public async Task<bool> LaunchInstanceAsync(string instanceName, CancellationToken cancellationToken = default)
            => await WithInstanceAsync(instanceName, inst => ExecuteConsoleAsync(inst.Install, $"launch --name \"{inst.Name}\"", cancellationToken), cancellationToken).ConfigureAwait(false);

        public async Task<bool> StopInstanceAsync(string instanceName, CancellationToken cancellationToken = default)
            => await WithInstanceAsync(instanceName, inst => ExecuteConsoleAsync(inst.Install, $"quit --name \"{inst.Name}\"", cancellationToken), cancellationToken).ConfigureAwait(false);

        public async Task<bool> FocusInstanceAsync(string instanceName, CancellationToken cancellationToken = default)
        {
            return await WithInstanceAsync(instanceName, async inst =>
            {
                if (string.IsNullOrWhiteSpace(inst.AdbId)) return false;
                await RunAdbAsync(inst, "shell input keyevent 224", cancellationToken).ConfigureAwait(false);
                await RunAdbAsync(inst, "shell input keyevent 3", cancellationToken).ConfigureAwait(false);
                return true;
            }, cancellationToken).ConfigureAwait(false);
        }

        public Task<bool> TapAsync(string instanceName, int x, int y, CancellationToken cancellationToken = default)
            => ShellAsync(instanceName, $"input tap {x} {y}", cancellationToken);

        public Task<bool> SwipeAsync(string instanceName, int x1, int y1, int x2, int y2, int durationMs, CancellationToken cancellationToken = default)
            => ShellAsync(instanceName, $"input swipe {x1} {y1} {x2} {y2} {durationMs}", cancellationToken);

        public async Task<bool> InputTextAsync(string instanceName, string text, CancellationToken cancellationToken = default)
        {
            string encoded = EncodeText(text);
            return await ShellAsync(instanceName, $"input text {encoded}", cancellationToken).ConfigureAwait(false);
        }

        public Task<bool> KeyEventAsync(string instanceName, int keyCode, CancellationToken cancellationToken = default)
            => ShellAsync(instanceName, $"input keyevent {keyCode}", cancellationToken);

        public Task<bool> ShellAsync(string instanceName, string command, CancellationToken cancellationToken = default)
            => WithInstanceAsync(instanceName, inst => RunAdbAsync(inst, $"shell {command}", cancellationToken), cancellationToken);

        public async Task<(bool Success, byte[] Data)> CaptureRawScreenshotAsync(string instanceName, CancellationToken cancellationToken = default)
        {
            var inst = await GetInstanceAsync(instanceName, cancellationToken).ConfigureAwait(false);
            if (inst == null)
            {
                return (false, Array.Empty<byte>());
            }

            var (success, _, data) = await RunAdbProcessAsync(inst, "exec-out screencap -p", captureBinary: true, cancellationToken).ConfigureAwait(false);
            return (success, data);
        }

        private async Task<EmulatorInstance?> GetInstanceAsync(string instanceName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(instanceName)) return null;
            var instances = await GetInstancesInternalAsync(cancellationToken).ConfigureAwait(false);
            return instances.FirstOrDefault(i => string.Equals(i.Name, instanceName, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<List<EmulatorInstance>> GetInstancesInternalAsync(CancellationToken cancellationToken)
        {
            await RefreshAsync(false, cancellationToken).ConfigureAwait(false);
            await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return _instances.ToList();
            }
            finally
            {
                _mutex.Release();
            }
        }

        private async Task<bool> WithInstanceAsync(string instanceName, Func<EmulatorInstance, Task<bool>> action, CancellationToken cancellationToken)
        {
            var instance = await GetInstanceAsync(instanceName, cancellationToken).ConfigureAwait(false);
            if (instance == null)
            {
                _logger.Warn($"Instance '{instanceName}' not found.");
                return false;
            }

            try
            {
                return await action(instance).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error($"[{instanceName}] operation failed: {ex.Message}");
                return false;
            }
        }

        private EmulatorInstall? CreateInstall(string path)
        {
            try
            {
                string console = Path.Combine(path, "dnconsole.exe");
                if (!File.Exists(console))
                {
                    console = Path.Combine(path, "ldconsole.exe");
                    if (!File.Exists(console))
                    {
                        _logger.Warn($"LDPlayer console executable missing for '{path}'.");
                        return null;
                    }
                }

                string adb = FindAdbExecutable(path);
                if (!File.Exists(adb))
                {
                    _logger.Warn($"ADB executable not found for LDPlayer install at '{path}'.");
                    return null;
                }

                string name = path.Contains("LDPlayer9", StringComparison.OrdinalIgnoreCase)
                    ? "LDPlayer 9"
                    : path.Contains("LDPlayer4", StringComparison.OrdinalIgnoreCase) || path.Contains("4.0", StringComparison.OrdinalIgnoreCase)
                        ? "LDPlayer 4"
                        : "LDPlayer";

                return new EmulatorInstall(name, path, console, adb);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to register LDPlayer installation '{path}': {ex.Message}");
                return null;
            }
        }

        private static string FindAdbExecutable(string path)
        {
            var candidates = new[]
            {
                Path.Combine(path, "adb.exe"),
                Path.Combine(path, "adb", "adb.exe"),
                Path.Combine(path, "tools", "adb.exe"),
                Path.Combine(path, "tool", "adb.exe"),
            };

            return candidates.FirstOrDefault(File.Exists) ?? candidates.Last();
        }
        private void LogMissingInstall(string path, string resource)
        {
            if (_missingInstalls.Add($"{path}|{resource}"))
            {
                _logger.Warn($"LDPlayer installation at '{path}' skipped: missing {resource}.");
            }
        }


        private async Task<List<EmulatorInstance>> QueryInstancesAsync(EmulatorInstall install, CancellationToken cancellationToken)
        {
            var list = new List<EmulatorInstance>();
            try
            {
                var output = await ExecuteConsoleAsync(install, "list2", cancellationToken, captureOutput: true).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(output)) return list;

                foreach (var block in SplitBlocks(output))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var map = ParseBlock(block);
                    if (!map.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name)) continue;

                    int index = map.TryGetValue("index", out var idxStr) && int.TryParse(idxStr, out var idx)
                        ? idx
                        : list.Count;
                    bool running = map.TryGetValue("status", out var status) && status is "1" or "running";
                    string? adbId = ResolveAdbId(map, index);
                    list.Add(new EmulatorInstance(name.Trim(), index, adbId, running, install));
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to enumerate LDPlayer instances for '{install.RootPath}': {ex.Message}");
            }

            return list;
        }

        private static string? ResolveAdbId(Dictionary<string, string> map, int index)
        {
            if (map.TryGetValue("adb_id", out var adb)) return adb;
            if (map.TryGetValue("adb", out var adb2)) return adb2;
            if (map.TryGetValue("port", out var portStr) && int.TryParse(portStr, out var port)) return $"127.0.0.1:{port}";
            return $"127.0.0.1:{5555 + index}";
        }

        private async Task<string> ExecuteConsoleAsync(EmulatorInstall install, string arguments, CancellationToken cancellationToken, bool captureOutput = false)
        {
            var (success, output, _) = await RunConsoleProcessAsync(install, arguments, captureOutput, cancellationToken).ConfigureAwait(false);
            if (!success)
            {
                _logger.Warn($"[{install.DisplayName}] console command '{arguments}' returned non-zero exit code.");
            }

            return output;
        }

        private async Task<bool> ExecuteConsoleAsync(EmulatorInstall install, string arguments, CancellationToken cancellationToken)
        {
            var (success, _, _) = await RunConsoleProcessAsync(install, arguments, captureOutput: false, cancellationToken).ConfigureAwait(false);
            return success;
        }

        private async Task<(bool Success, string Output, byte[] Data)> RunConsoleProcessAsync(EmulatorInstall install, string arguments, bool captureOutput, CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo
            {
                FileName = install.ConsolePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = captureOutput,
                RedirectStandardError = captureOutput,
                CreateNoWindow = true,
                WorkingDirectory = install.RootPath,
                StandardOutputEncoding = Encoding.UTF8
            };

            var (success, output, error, data) = await RunProcessAsync(psi, captureOutput, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.Warn($"[{install.DisplayName}] console error: {error}");
            }

            return (success, output, data);
        }

        private async Task<bool> RunAdbAsync(EmulatorInstance instance, string arguments, CancellationToken cancellationToken)
        {
            var (success, _, _) = await RunAdbProcessAsync(instance, arguments, captureBinary: false, cancellationToken).ConfigureAwait(false);
            return success;
        }

        private async Task<(bool Success, string Output, byte[] Data)> RunAdbProcessAsync(EmulatorInstance instance, string arguments, bool captureBinary, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(instance.AdbId))
            {
                _logger.Warn($"ADB id unavailable for instance '{instance.Name}'.");
                return (false, string.Empty, Array.Empty<byte>());
            }

            var psi = new ProcessStartInfo
            {
                FileName = instance.Install.AdbPath,
                Arguments = $"-s {instance.AdbId} {arguments}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = captureBinary ? null : Encoding.UTF8,
                WorkingDirectory = instance.Install.RootPath
            };

            var (success, output, error, data) = await RunProcessAsync(psi, true, cancellationToken, captureBinary).ConfigureAwait(false);
            if (!success)
            {
                _logger.Warn($"ADB command '{arguments}' failed for '{instance.Name}'.");
            }
            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.Warn($"ADB stderr for '{instance.Name}': {error}");
            }

            return (success, output, data);
        }

        private static async Task<(bool Success, string Output, string Error, byte[] Data)> RunProcessAsync(ProcessStartInfo psi, bool captureOutput, CancellationToken cancellationToken, bool captureBinary = false)
        {
            using var process = new Process { StartInfo = psi, EnableRaisingEvents = false };
            try
            {
                if (!process.Start())
                {
                    return (false, string.Empty, "Failed to start process.", Array.Empty<byte>());
                }
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 740)
            {
                return (false, string.Empty, "The requested operation requires elevation. Configure LDPlayer to run without administrator privileges.", Array.Empty<byte>());
            }
            catch (Win32Exception ex)
            {
                return (false, string.Empty, ex.Message, Array.Empty<byte>());
            }

            cancellationToken.ThrowIfCancellationRequested();

            Task<byte[]> stdoutTask = captureOutput
                ? ReadStreamAsync(process.StandardOutput.BaseStream, captureBinary, cancellationToken)
                : Task.FromResult(Array.Empty<byte>());
            Task<string> stderrTask = captureOutput
                ? process.StandardError.ReadToEndAsync()
                : Task.FromResult(string.Empty);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);

            bool success = process.ExitCode == 0;
            string textOutput = psi.StandardOutputEncoding != null
                ? psi.StandardOutputEncoding.GetString(stdout)
                : Encoding.UTF8.GetString(stdout);

            return (success, textOutput.Trim(), stderr.Trim(), stdout);
        }

        private static async Task<byte[]> ReadStreamAsync(Stream stream, bool captureBinary, CancellationToken cancellationToken)
        {
            await using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            return ms.ToArray();
        }

        private static IEnumerable<string> SplitBlocks(string text)
        {
            var current = new StringBuilder();
            using var reader = new StringReader(text);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line))
                {
                    if (current.Length > 0)
                    {
                        yield return current.ToString();
                        current.Clear();
                    }
                }
                else
                {
                    if (line.StartsWith("--------", StringComparison.Ordinal)) continue;
                    current.AppendLine(line);
                }
            }

            if (current.Length > 0)
            {
                yield return current.ToString();
            }
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
                {
                    sb.Append("%s");
                }
                else if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(ch);
                }
                else
                {
                    sb.AppendFormat("\\u{0:X4}", (int)ch);
                }
            }

            return sb.ToString();
        }
    }
}