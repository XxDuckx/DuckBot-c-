using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DuckBot.Data.Templates
{
    public static class StepTemplates
    {
        public sealed record StepTemplateInfo(
            string Type,
            string DisplayName,
            string Category,
            string Description,
            Dictionary<string, object> Defaults,
            Dictionary<string, string> ParameterHints);

        private static readonly Dictionary<string, StepTemplateInfo> _library =
     new(StringComparer.OrdinalIgnoreCase)
     {
        { "TAP", Create("TAP", "Tap", "Input",
            "Simulate a tap on the LDPlayer screen.",
            new() { { "x", 0 }, { "y", 0 }, { "delay", 500 } },
            new() { { "x", "Horizontal coordinate (supports ${x} variables)" }, { "y", "Vertical coordinate" }, { "delay", "Delay after tap in milliseconds" } }) },

        { "WAIT", Create("WAIT", "Wait", "Flow",
            "Pause script execution for a period of time.",
            new() { { "delay", 1000 } },
            new() { { "delay", "Duration to wait in milliseconds" } }) },

        { "INPUT", Create("INPUT", "Input Text", "Input",
            "Types text into the active field.",
            new() { { "text", string.Empty } },
            new() { { "text", "Text or variable placeholder to type" } }) },

        { "IF_IMAGE", Create("IF_IMAGE", "If Image", "Vision",
            "Branch when an image is detected on screen.",
            new() { { "imagePath", string.Empty }, { "confidence", 0.9f } },
            new() { { "imagePath", "Relative path to reference image" }, { "confidence", "Match confidence between 0 and 1" } }) },

        { "LOG", Create("LOG", "Log", "Diagnostics",
            "Write a message to the DuckBot log.",
            new() { { "message", string.Empty } },
            new() { { "message", "Message contents" } }) },

        { "LOOP", Create("LOOP", "Loop", "Flow",
            "Repeat the nested steps a number of times.",
            new() { { "count", 1 } },
            new() { { "count", "Number of iterations" } }) },

        { "CUSTOM_JS", Create("CUSTOM_JS", "Custom JS", "Advanced",
            "Execute custom JavaScript within LDPlayer.",
            new() { { "code", string.Empty } },
            new() { { "code", "JavaScript snippet to run" } }) }
     };


        private static StepTemplateInfo Create(
            string type,
            string displayName,
            string category,
            string description,
            Dictionary<string, object> defaults,
            Dictionary<string, string>? hints = null)
        {
            return new StepTemplateInfo(
                type,
                displayName,
                category,
                description,
                defaults,
                hints ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        private static readonly HashSet<string> _loadedGames = new();
        public static IReadOnlyDictionary<string, StepTemplateInfo> Library => _library;

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
                _library[tpl.Type] = tpl;
            }
        }

        private static IEnumerable<StepTemplateInfo> LoadExternalTemplates(string game)
        {
            string gameFolder = game.Replace(" ", string.Empty);
            string dir = Path.Combine("Games", gameFolder, "templates");
            if (!Directory.Exists(dir)) return Array.Empty<StepTemplateInfo>();

            var list = new List<StepTemplateInfo>();
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

                    string display = root.TryGetProperty("display", out var displayProp) && displayProp.ValueKind == JsonValueKind.String
                        ? displayProp.GetString()!
                        : type.Replace('_', ' ').ToLowerInvariant();
                    if (!string.IsNullOrWhiteSpace(display))
                    {
                        display = char.ToUpperInvariant(display[0]) + (display.Length > 1 ? display[1..] : string.Empty);
                    }
                    else
                    {
                        display = type;
                    }

                    string category = root.TryGetProperty("category", out var categoryProp) && categoryProp.ValueKind == JsonValueKind.String
                        ? categoryProp.GetString()!
                        : "Custom";

                    string description = root.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String
                        ? descProp.GetString()!
                        : $"{display} step";

                    JsonElement parameters = root.TryGetProperty("defaults", out var defaultsProp) ? defaultsProp : root;
                    var map = parameters.EnumerateObject().ToDictionary(p => p.Name, p => ConvertValue(p.Value));

                    var hints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (root.TryGetProperty("hints", out var hintsProp) && hintsProp.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var item in hintsProp.EnumerateObject())
                        {
                            if (item.Value.ValueKind == JsonValueKind.String)
                            {
                                hints[item.Name] = item.Value.GetString()!;
                            }
                        }
                    }

                    list.Add(new StepTemplateInfo(type, display, category, description, map, hints));
                }
                catch
                {
                    // ignore invalid template
                }
            }

            return list;
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