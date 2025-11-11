using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DuckBot.Data.Templates
{
    public static class StepTemplates
    {
        private static readonly Dictionary<string, Dictionary<string, object>> _library = new()
        {
            { "TAP", new() { { "x", 0 }, { "y", 0 }, { "delay", 500 } } },
            { "WAIT", new() { { "delay", 1000 } } },
            { "INPUT", new() { { "text", "" } } },
            { "IF_IMAGE", new() { { "imagePath", "" }, { "confidence", 0.9f } } },
            { "LOG", new() { { "message", "" } } },
            { "LOOP", new() { { "count", 1 } } },
            { "CUSTOM_JS", new() { { "code", "" } } }
        };

        private static readonly HashSet<string> _loadedGames = new();
        public static IReadOnlyDictionary<string, Dictionary<string, object>> Library => _library;

        public static void EnsureGameTemplates(string game)
        {
            lock (_loadedGames)
            {
                string key = game.Trim();
                if (_loadedGames.Contains(key)) return;
                _loadedGames.Add(key);
            }

            foreach (var tpl in LoadExternalTemplates(game))
            {
                _library[tpl.Key] = tpl.Value;
            }
        }

        private static Dictionary<string, Dictionary<string, object>> LoadExternalTemplates(string game)
        {
            var result = new Dictionary<string, Dictionary<string, object>>();
            string gameFolder = game.Replace(" ", string.Empty);
            string dir = Path.Combine("Games", gameFolder, "templates");
            if (!Directory.Exists(dir)) return result;

            foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
            {
                try
                {
                    using var stream = File.OpenRead(file);
                    using var doc = JsonDocument.Parse(stream);
                    var root = doc.RootElement;

                    string type = Path.GetFileNameWithoutExtension(file).ToUpperInvariant();
                    if (root.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String)
                    {
                        type = typeProp.GetString()!.ToUpperInvariant();
                    }

                    JsonElement parameters = root.TryGetProperty("defaults", out var defaultsProp) ? defaultsProp : root;
                    var map = parameters.EnumerateObject().ToDictionary(p => p.Name, p => ConvertValue(p.Value));
                    result[type] = map;
                }
                catch
                {
                    // ignore invalid template
                }
            }

            return result;
        }

        private static object ConvertValue(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt64(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertValue).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertValue(p.Value)),
            _ => string.Empty
        };
    }
}