using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DuckBot.Data.Models;

namespace DuckBot.Data.Storage
{
    public static class BotStore
    {
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
        public static string Root => Path.Combine(AppContext.BaseDirectory, "data", "bots");

        public static IEnumerable<BotProfile> LoadAll()
        {
            Directory.CreateDirectory(Root);
            foreach (var file in Directory.GetFiles(Root, "*.json"))
            {
                var txt = File.ReadAllText(file);
                BotProfile? bot = JsonSerializer.Deserialize<BotProfile>(txt, _json);
                if (bot != null) yield return bot;
            }
        }

        public static void Save(BotProfile bot)
        {
            Directory.CreateDirectory(Root);
            var path = Path.Combine(Root, $"{Sanitize(bot.Name)}_{bot.Id}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(bot, _json));
        }

        public static void Delete(BotProfile bot)
        {
            Directory.CreateDirectory(Root);
            foreach (var f in Directory.GetFiles(Root, $"*{bot.Id}*.json"))
                File.Delete(f);
        }

        private static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Trim();
        }
    }
}
