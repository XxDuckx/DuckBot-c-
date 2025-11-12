using DuckBot.Core.Emu;
using DuckBot.Core.Logging;
using DuckBot.Core.Scripting;
using DuckBot.Core.Services;
using DuckBot.Data.Models;
using DuckBot.Data.Scripts;
using DuckBot.Scripting;
using DuckBot.Scripting.Bridges;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DuckBot.Core.Services
{
    public sealed class BotRunnerService : IBotRunnerService
    {
        private readonly IAppLogger _logger;
        private readonly IAdbService _adbService;
        private readonly ConcurrentDictionary<string, RunningBot> _running = new();
        private readonly ConcurrentDictionary<string, string> _status = new();

        private sealed record RunningBot(Task Execution, CancellationTokenSource Cancellation);

        public BotRunnerService(IAppLogger logger, IAdbService adbService)
        {
            _logger = logger;
            _adbService = adbService;
        }

        public bool IsRunning(string botId) => _running.ContainsKey(botId);

        public string GetStatus(string botId) => _status.TryGetValue(botId, out var status) ? status : "Idle";

        public IReadOnlyDictionary<string, string> StatusSnapshot() => new Dictionary<string, string>(_status);

        public Task StartAsync(BotProfile bot)
        {
            if (bot == null) throw new ArgumentNullException(nameof(bot));

            if (string.IsNullOrWhiteSpace(bot.Instance))
            {
                _logger.Warn($"Bot '{bot.Name}' has no emulator instance assigned.");
                return Task.CompletedTask;
            }

            if (_running.ContainsKey(bot.Id))
            {
                _logger.Warn($"Bot '{bot.Name}' is already running.");
                return Task.CompletedTask;
            }

            var cts = new CancellationTokenSource();
            var runningBot = new RunningBot(Task.Run(() => RunBotAsync(bot, cts.Token), cts.Token), cts);
            if (!_running.TryAdd(bot.Id, runningBot))
            {
                cts.Cancel();
                return Task.CompletedTask;
            }

            _status[bot.Id] = "Starting";
            _logger.Info($"Starting bot '{bot.Name}' on {bot.Instance}");
            return Task.CompletedTask;
        }

        public Task StopAsync(BotProfile bot)
        {
            if (bot == null) throw new ArgumentNullException(nameof(bot));

            if (_running.TryRemove(bot.Id, out var running))
            {
                running.Cancellation.Cancel();
                running.Cancellation.Dispose();
            }

            _status[bot.Id] = "Stopping";
            return Task.CompletedTask;
        }

        public Task StopAllAsync()
        {
            foreach (var kv in _running)
            {
                kv.Value.Cancellation.Cancel();
                kv.Value.Cancellation.Dispose();
            }

            _running.Clear();
            _status.Clear();
            _logger.Info("All bots stopped.");
            return Task.CompletedTask;
        }

        private async Task RunBotAsync(BotProfile bot, CancellationToken cancellationToken)
        {
            try
            {
                using var ocr = new OcrBridge(bot.Instance, _adbService, _logger);
                using var cv = new CvBridge(bot.Instance, bot.Game, _adbService, _logger);
                var adbBridge = new AdbBridge(bot.Instance, bot.Name, _adbService, _logger);

                var util = new Util(_logger.Info, () => cancellationToken);
                var engine = new JsEngine(util, adbBridge, cv, ocr);
                engine.OnPrint += message => _logger.Info($"[{bot.Name}] {message}");

                string script = await LoadScriptForAsync(bot, cancellationToken).ConfigureAwait(false);
                _status[bot.Id] = "Running";
                await engine.RunAsync(script, cancellationToken).ConfigureAwait(false);
                _logger.Info($"Bot '{bot.Name}' finished execution.");
            }
            catch (OperationCanceledException)
            {
                _logger.Info($"Bot '{bot.Name}' canceled.");
            }
            catch (Exception ex)
            {
                _logger.Error($"[{bot.Name}] crash: {ex.Message}");
            }
            finally
            {
                if (_running.TryRemove(bot.Id, out var running))
                {
                    running.Cancellation.Dispose();
                }
                _status[bot.Id] = "Idle";
            }
        }

        private async Task<string> LoadScriptForAsync(BotProfile bot, CancellationToken cancellationToken)
        {
            if (bot.Scripts is { Count: > 0 })
            {
                var builder = new StringBuilder();
                foreach (var scriptSetting in bot.Scripts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!scriptSetting.Enabled) continue;

                    string path = Path.Combine(ScriptIO.GetGameDirectory(bot.Game), $"{Sanitize(scriptSetting.Name)}.json");
                    if (!File.Exists(path))
                    {
                        _logger.Warn($"Script '{scriptSetting.Name}' not found for bot '{bot.Name}'.");
                        continue;
                    }

                    var model = await ScriptIO.LoadAsync(path, cancellationToken).ConfigureAwait(false);
                    var vars = scriptSetting.Variables ?? new Dictionary<string, object>();
                    builder.AppendLine(ScriptTranspiler.Transpile(model, vars));
                }

                if (builder.Length > 0)
                    return builder.ToString();
            }

            return "print('DuckBot JS runner online');\nutil.sleep(1000);\nprint('Done.');";
        }

        private static string Sanitize(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Trim();
        }

    }
}
