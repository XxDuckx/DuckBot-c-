using System.IO;
using System.Text.Json;
using DuckBot.Data.Models;

namespace DuckBot.Core.Scripts
{
    public static class ScriptIO
    {
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public static void Save(ScriptModel script, string path)
        {
            string json = JsonSerializer.Serialize(script, Options);
            File.WriteAllText(path, json);
        }

        public static ScriptModel Load(string path)
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ScriptModel>(json, Options) ?? new ScriptModel();
        }

        public static List<ScriptModel> LoadAllFromGame(string game)
        {
            string dir = Path.Combine("data", "scripts", game);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            return Directory.GetFiles(dir, "*.json")
                            .Select(f => Load(f))
                            .ToList();
        }
    }
}
