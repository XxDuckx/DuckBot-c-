using System.Threading;
using System.Threading.Tasks;

namespace DuckBot.Core.Services
{
    /// <summary>
    /// Safe stub emulator: does nothing but delay. Replace with real implementation.
    /// </summary>
    public class EmulatorStub : IEmulatorService
    {
        public Task ClickAsync(string instance, int x, int y, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SendTextAsync(string instance, string text, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task DelayAsync(int milliseconds, CancellationToken cancellationToken)
            => Task.Delay(milliseconds, cancellationToken);
    }
}