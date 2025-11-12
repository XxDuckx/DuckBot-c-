using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DuckBot.Core.Services
{
    public interface IEmulatorService
    {
        Task RefreshAsync(bool force = false, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EmulatorInstanceInfo>> GetInstancesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> GetInstanceNamesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
        Task<bool> LaunchInstanceAsync(string instanceName, CancellationToken cancellationToken = default);
        Task<bool> StopInstanceAsync(string instanceName, CancellationToken cancellationToken = default);
        Task<bool> FocusInstanceAsync(string instanceName, CancellationToken cancellationToken = default);
        Task<bool> TapAsync(string instanceName, int x, int y, CancellationToken cancellationToken = default);
        Task<bool> SwipeAsync(string instanceName, int x1, int y1, int x2, int y2, int durationMs, CancellationToken cancellationToken = default);
        Task<bool> InputTextAsync(string instanceName, string text, CancellationToken cancellationToken = default);
        Task<bool> KeyEventAsync(string instanceName, int keyCode, CancellationToken cancellationToken = default);
        Task<bool> ShellAsync(string instanceName, string command, CancellationToken cancellationToken = default);
        Task<(bool Success, byte[] Data)> CaptureRawScreenshotAsync(string instanceName, CancellationToken cancellationToken = default);
    }
}