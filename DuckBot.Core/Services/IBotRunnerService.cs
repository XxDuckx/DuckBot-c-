using System.Collections.Generic;
using System.Threading.Tasks;
using DuckBot.Data.Models;

namespace DuckBot.Core.Services
{
    public interface IBotRunnerService
    {
        bool IsRunning(string botId);
        string GetStatus(string botId);
        Task StartAsync(BotProfile bot);
        Task StopAsync(BotProfile bot);
        Task StopAllAsync();
        IReadOnlyDictionary<string, string> StatusSnapshot();
    }
}