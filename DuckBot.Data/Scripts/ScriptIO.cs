using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DuckBot.Data.Models;

namespace DuckBot.Data.Scripts
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

        public static async Task<ScriptModel> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            await using var stream = File.OpenRead(path);
            var model = await JsonSerializer.DeserializeAsync<ScriptModel>(stream, Options, cancellationToken).ConfigureAwait(false);
            return model ?? new ScriptModel();
        }

        public static List<ScriptModel> LoadAllFromGame(string game)
        {
            string dir = GetGameDirectory(game);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            return Directory.GetFiles(dir, "*.json")
                            .Select(Load)
                            .ToList();
        }

        public static string GetGameDirectory(string game)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(game.Length);
            foreach (char ch in game)
            {
                if (invalid.Contains(ch)) continue;
                if (ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar)
                {
                    builder.Append('_');
                }
                else
                {
                    builder.Append(ch);
                }
            }

            string safe = builder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(safe)) safe = "Game";
            return Path.Combine("data", "scripts", safe);
        }
    }
}