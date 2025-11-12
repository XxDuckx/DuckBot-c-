using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DuckBot.Core.Emu
{
    public interface IEmulatorDetector
    {
        Task<IReadOnlyCollection<string>> DetectInstallPathsAsync(CancellationToken cancellationToken = default);
    }
}