using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DuckBot.Core.Services;

namespace DuckBot.GUI.ViewModels
{
    public partial class BotViewModel : ObservableObject
    {
        private readonly IBotRunner _runner;
        private readonly IBotStore _store;
        private readonly IWindowManager _windowManager;

        public ObservableCollection<BotEntry> Bots { get; } = new();
        public ObservableCollection<string> AvailableInstances { get; } = new();
        public ObservableCollection<string> Logs { get; } = new();

        // per-bot running tokens
        private readonly ConcurrentDictionary<BotEntry, CancellationTokenSource> _running = new();

        public BotViewModel(IBotRunner runner, IBotStore store, IWindowManager windowManager)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));

            // sample instances
            AvailableInstances.Add("Instance A");
            AvailableInstances.Add("Instance B");
            AvailableInstances.Add("None");

            // sample bots fallback
            if (!Bots.Any())
            {
                Bots.Add(new BotEntry { Name = "Test Bot 1", Game = "West Game", Instance = AvailableInstances.FirstOrDefault() });
            }
        }

        public void LoadBots(IList<BotEntry> bots)
        {
            Bots.Clear();
            foreach (var b in bots) Bots.Add(b);
        }

        public IList<BotEntry> GetSerializableBots() => Bots.ToList();

        private void AddLog(string line)
        {
            App.Current?.Dispatcher?.Invoke(() =>
            {
                Logs.Add($"{DateTime.Now:HH:mm:ss} {line}");
                if (Logs.Count > 1000) Logs.RemoveAt(0);
            });
        }

        [RelayCommand]
        public async Task StartBotAsync(BotEntry bot)
        {
            if (bot == null) return;
            if (_running.ContainsKey(bot))
            {
                AddLog($"'{bot.Name}' is already running.");
                return;
            }

            var cts = new CancellationTokenSource();
            if (!_running.TryAdd(bot, cts))
            {
                AddLog($"Failed to start '{bot.Name}'.");
                return;
            }

            AddLog($"Starting '{bot.Name}'...");
            var progress = new Progress<string>(s => AddLog(s));
            try
            {
                await _runner.StartAsync(bot.Script, bot.Instance ?? "None", progress, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                AddLog($"'{bot.Name}' cancelled.");
            }
            catch (Exception ex)
            {
                AddLog($"'{bot.Name}' error: {ex.Message}");
            }
            finally
            {
                _running.TryRemove(bot, out _);
            }
        }

        [RelayCommand]
        public void StopBot(BotEntry bot)
        {
            if (bot == null) return;
            if (_running.TryRemove(bot, out var cts))
            {
                cts.Cancel();
                AddLog($"Stopping '{bot.Name}'...");
            }
            else
            {
                AddLog($"'{bot.Name}' is not running.");
            }
        }

        [RelayCommand]
        public void AddBot()
        {
            Bots.Add(new BotEntry { Name = $"Bot{Bots.Count + 1}", Game = "West Game", Instance = AvailableInstances.FirstOrDefault() });
            AddLog("Added new bot.");
        }

        [RelayCommand]
        public void DeleteBot(BotEntry bot)
        {
            if (bot == null) return;
            StopBot(bot);
            Bots.Remove(bot);
            AddLog($"Deleted '{bot.Name}'.");
        }

        [RelayCommand]
        public async Task StartSelectedAsync(System.Collections.IList items)
        {
            if (items == null) return;
            foreach (var o in items.Cast<BotEntry>().ToList())
                await StartBotAsync(o);
        }

        [RelayCommand]
        public void StopSelected(System.Collections.IList items)
        {
            if (items == null) return;
            foreach (var o in items.Cast<BotEntry>().ToList())
                StopBot(o);
        }

        [RelayCommand]
        public void DeleteSelected(System.Collections.IList items)
        {
            if (items == null) return;
            foreach (var o in items.Cast<BotEntry>().ToList())
                DeleteBot(o);
        }

        [RelayCommand]
        public async Task QuickLaunchAsync()
        {
            var bot = Bots.FirstOrDefault();
            if (bot != null) await StartBotAsync(bot);
        }

        // New: open the quick editor window for a bot
        [RelayCommand]
        public void OpenEditor(BotEntry? bot)
        {
            var b = bot ?? Bots.FirstOrDefault();
            if (b == null) return;
            _windowManager.OpenEditor(b);
        }

        // New: refresh instances (stub)
        [RelayCommand]
        public void RefreshInstances()
        {
            AddLog("Refreshed instances (MVP).");
        }

        // New: fix emulators (stub)
        [RelayCommand]
        public void FixEmulators()
        {
            AddLog("Fix emulators (MVP stub).");
        }

        // Editor helpers (commands used by editor)
        [RelayCommand]
        public void AddVariable(BotEntry? bot)
        {
            var b = bot ?? Bots.FirstOrDefault();
            if (b == null) return;
            b.Script.Variables.Add(new DuckBot.Data.Models.ScriptVariable { Key = $"var{b.Script.Variables.Count + 1}", Default = "" });
        }

        [RelayCommand]
        public void RemoveVariable(DuckBot.Data.Models.ScriptVariable variable)
        {
            foreach (var b in Bots)
            {
                if (b.Script.Variables.Remove(variable))
                {
                    AddLog($"Removed variable {variable.Key}.");
                    return;
                }
            }
        }

        public void AddLogPublic(string s) => AddLog(s);
    }
}