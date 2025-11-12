using System.Threading;
using System.Threading.Tasks;

namespace DuckBot.Core.Services
{
    /// <summary>
    /// Abstraction for emulator / automation actions. MVP: methods are stubs.
    /// Implement your real emulator integration behind this interface.
    /// </summary>
    public interface IEmulatorService
    {
        Task ClickAsync(string instance, int x, int y, CancellationToken cancellationToken);
        Task SendTextAsync(string instance, string text, CancellationToken cancellationToken);
        Task DelayAsync(int milliseconds, CancellationToken cancellationToken);
    }
}