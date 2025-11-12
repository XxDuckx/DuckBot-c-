using DuckBot.Core.Scripting;
using DuckBot.Data.Scripts;
using DuckBot.Data.Models;
using DuckBot.Data.Scripts;
using DuckBot.Scripting;
using DuckBot.Scripting.Bridges;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.IO;


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
            if (string.IsNullOrWhiteSpace(bot.Instance))
            {
                LogService.Warn($"Bot '{bot.Name}' has no emulator instance assigned.");
                return;
            }

            var cts = new CancellationTokenSource();
            _running[bot.Id] = cts;
            _status[bot.Id] = "Starting";

            LogService.Info($"Starting bot '{bot.Name}' on {bot.Instance}");

            Task.Run(async () =>
            {
                try
                {
                    using var ocr = new OcrBridge(bot.Instance);
                    var engine = CreateEngine(bot, cts.Token, ocr);
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
            => CreateEngine(bot, token, null);

        private static JsEngine CreateEngine(BotProfile bot, CancellationToken token, OcrBridge? sharedOcr)
        {
            var util = new Util(LogService.Info, () => token);
            var adb = new AdbBridge(bot.Instance, bot.Name);
            var cv = new CvBridge(bot.Instance, bot.Game);
            var ocr = sharedOcr ?? new OcrBridge(bot.Instance);

            var eng = new JsEngine(util, adb, cv, ocr);
            eng.OnPrint += (s) => LogService.Info($"[{bot.Name}] {s}");
            // future: add adb/cv/ocr bridges here
            return eng;
        }

        private static string LoadScriptFor(BotProfile bot)
        {
            if (bot.Scripts is { Count: > 0 })
            {
                var builder = new System.Text.StringBuilder();
                foreach (var scriptSetting in bot.Scripts)
                {
                    if (!scriptSetting.Enabled) continue;
                    string path = Path.Combine(ScriptIO.GetGameDirectory(bot.Game), $"{San(scriptSetting.Name)}.json");
                    if (!File.Exists(path))
                    {
                        LogService.Warn($"Script '{scriptSetting.Name}' not found for bot '{bot.Name}'.");
                        continue;
                    }

                    var model = ScriptIO.Load(path);
                    var vars = scriptSetting.Variables ?? new System.Collections.Generic.Dictionary<string, object>();
                    builder.AppendLine(ScriptTranspiler.Transpile(model, vars));
                }

                if (builder.Length > 0)
                    return builder.ToString();
            }

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