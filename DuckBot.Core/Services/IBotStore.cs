using System.Collections.Generic;
using System.Threading.Tasks;
using DuckBot.Core.Models;

namespace DuckBot.Core.Services
{
    public interface IBotStore
    {
        Task<IList<BotRecord>> LoadAsync();
        Task SaveAsync(IList<BotRecord> bots);
    }
}