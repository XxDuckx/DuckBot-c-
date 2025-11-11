using System.IO;
using System.Text.Json;
using DuckBot.Core.Models;

namespace DuckBot.Core.Services
{
    public static class SettingsManager
    {
        private static readonly string ConfigPath = Path.Combine("data", "config.json");

        public static SettingsModel Current { get; private set; } = new();

        public static void Load()
        {
            if (!File.Exists(ConfigPath))
            {
                Save(); // create default config
                return;
            }

            var json = File.ReadAllText(ConfigPath);
            Current = JsonSerializer.Deserialize<SettingsModel>(json) ?? new SettingsModel();
        }

        public static void Save()
        {
            Directory.CreateDirectory("data");
            var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
    }
}
