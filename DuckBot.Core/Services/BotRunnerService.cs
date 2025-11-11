using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DuckBot.Core.Services;
using DuckBot.Data.Models;
using DuckBot.Scripting;
using DuckBot.Scripting.Bridges;

namespace DuckBot.Core.Services
{
    public static class BotRunnerService
    {
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> _running = new();
        private static readonly ConcurrentDictionary<string, string> _status = new(); // botId -> status

        public static bool IsRunning(string botId) => _running.ContainsKey(botId);
        public static string GetStatus(string botId) => _status.TryGetValue(botId, out var s) ? s : "Idle";

        public static void Start(BotProfile bot)
        {
            if (_running.ContainsKey(bot.Id)) return;

            var cts = new CancellationTokenSource();
            _running[bot.Id] = cts;
            _status[bot.Id] = "Starting";

            LogService.Info($"Starting bot '{bot.Name}' on {bot.Instance}");

            Task.Run(async () =>
            {
                try
                {
                    var engine = CreateEngine(bot, cts.Token);
                    string script = LoadScriptFor(bot);
                    _status[bot.Id] = "Running";
                    await engine.RunAsync(script, cts.Token);
                }
                catch (TaskCanceledException) { /* normal stop */ }
                catch (System.Exception ex)
                {
                    LogService.Error($"[{bot.Name}] crash: {ex.Message}");
                }
                finally
                {
                    _status[bot.Id] = "Idle";
                    Stop(bot);
                    LogService.Info($"Bot '{bot.Name}' stopped.");
                }
            }, cts.Token);
        }

        public static void Stop(BotProfile bot)
        {
            if (_running.TryRemove(bot.Id, out var cts))
            {
                cts.Cancel();
            }
        }

        public static void StopAll()
        {
            foreach (var kv in _running) kv.Value.Cancel();
            _running.Clear();
            _status.Clear();
            LogService.Info("All bots stopped.");
        }

        private static JsEngine CreateEngine(BotProfile bot, CancellationToken token)
        {
            var eng = new JsEngine(new Util(LogService.Info, () => token));
            eng.OnPrint += (s) => LogService.Info($"[{bot.Name}] {s}");
            // future: add adb/cv/ocr bridges here
            return eng;
        }

        private static string LoadScriptFor(BotProfile bot)
        {
            // pick first enabled script, else a default loop
            var s = bot.Scripts?.Find(x => x.Enabled) ?? new ScriptSetting { Name = "default" };
            string gamesRoot = Path.Combine(AppContext.BaseDirectory, "Games", bot.Game.Replace(" ", ""));
            string jsPath = Path.Combine(gamesRoot, "scripts", $"{San(s.Name)}.js");
            if (File.Exists(jsPath))
                return File.ReadAllText(jsPath);

            // fallback JS stub
            return @"
print('DuckBot JS runner online');
for (let i=1;i<=5;i++){
  print('Step ' + i);
  util.sleep(1000);
}
print('Done.');
";
        }

        private static string San(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Trim();
        }
    }
}
