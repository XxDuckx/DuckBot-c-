using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DuckBot.Core.Models;

namespace DuckBot.Core.Services
{
    /// <summary>
    /// File-backed store in %APPDATA%\DuckBot\bots.json (Core-only types).
    /// </summary>
    public class FileBotStore : IBotStore
    {
        private readonly string _path;

        public FileBotStore()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "DuckBot");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "bots.json");
        }

        public async Task<IList<BotRecord>> LoadAsync()
        {
            if (!File.Exists(_path)) return new List<BotRecord>();

            var json = await File.ReadAllTextAsync(_path).ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var list = JsonSerializer.Deserialize<List<BotRecord>>(json, opts);
                return list ?? new List<BotRecord>();
            }
            catch
            {
                return new List<BotRecord>();
            }
        }

        public async Task SaveAsync(IList<BotRecord> bots)
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(bots, opts);
            await File.WriteAllTextAsync(_path, json).ConfigureAwait(false);
        }
    }
}