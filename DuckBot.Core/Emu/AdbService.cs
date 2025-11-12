using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DuckBot.Core.Services;

namespace DuckBot.Core.Emu
{
    public sealed class AdbService : IAdbService
    {
        private readonly IEmulatorService _emulatorService;

        public AdbService(IEmulatorService emulatorService)
        {
            _emulatorService = emulatorService;
        }

        public Task RefreshAsync(bool force = false, CancellationToken cancellationToken = default)
            => _emulatorService.RefreshAsync(force, cancellationToken);

        public Task<IReadOnlyList<string>> ListInstancesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
            => _emulatorService.GetInstanceNamesAsync(forceRefresh, cancellationToken);

        public Task<bool> LaunchInstanceAsync(string instanceName, CancellationToken cancellationToken = default)
            => _emulatorService.LaunchInstanceAsync(instanceName, cancellationToken);

        public Task<bool> StopInstanceAsync(string instanceName, CancellationToken cancellationToken = default)
            => _emulatorService.StopInstanceAsync(instanceName, cancellationToken);

        public Task<bool> FocusInstanceAsync(string instanceName, CancellationToken cancellationToken = default)
            => _emulatorService.FocusInstanceAsync(instanceName, cancellationToken);

        public Task<bool> TapAsync(string instanceName, int x, int y, CancellationToken cancellationToken = default)
            => _emulatorService.TapAsync(instanceName, x, y, cancellationToken);

        public Task<bool> SwipeAsync(string instanceName, int x1, int y1, int x2, int y2, int durationMs, CancellationToken cancellationToken = default)
            => _emulatorService.SwipeAsync(instanceName, x1, y1, x2, y2, durationMs, cancellationToken);

        public Task<bool> InputTextAsync(string instanceName, string text, CancellationToken cancellationToken = default)
            => _emulatorService.InputTextAsync(instanceName, text, cancellationToken);

        public Task<bool> KeyEventAsync(string instanceName, int keyCode, CancellationToken cancellationToken = default)
            => _emulatorService.KeyEventAsync(instanceName, keyCode, cancellationToken);

        public Task<bool> ShellAsync(string instanceName, string command, CancellationToken cancellationToken = default)
            => _emulatorService.ShellAsync(instanceName, command, cancellationToken);

        public Task<(bool Success, byte[] Data)> CaptureRawScreenshotAsync(string instanceName, CancellationToken cancellationToken = default)
            => _emulatorService.CaptureRawScreenshotAsync(instanceName, cancellationToken);
    }
}