using System.Threading;
using System.Threading.Tasks;
using DuckBot.Data.Models;

namespace DuckBot.Core.Services
{
    public interface IBotRunner
    {
        Task StartAsync(ScriptModel script, string instance, IProgress<string> progress, CancellationToken cancellationToken);
    }
}