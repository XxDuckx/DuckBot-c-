using System;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DuckBot.Core.Services;
using DuckBot.Core.Models;
using DuckBot.GUI.ViewModels;

namespace DuckBot.GUI
{
    public partial class App : Application
    {
        public IHost Host { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Host = Host.CreateDefaultBuilder()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddSingleton<IEmulatorService, EmulatorService>();
                    services.AddSingleton<VariableEngine>();
                    services.AddSingleton<IBotStore, FileBotStore>();
                    services.AddSingleton<IBotRunner, SimpleBotRunner>();
                    services.AddSingleton<IWindowManager, DuckBot.GUI.Services.WindowManager>();
                    services.AddSingleton<BotViewModel>();
                })
                .Build();

            Host.Start();

            var store = Host.Services.GetRequiredService<IBotStore>();
            var vm = Host.Services.GetRequiredService<BotViewModel>();
            try
            {
                var records = store.LoadAsync().GetAwaiter().GetResult();

                // Map BotRecord -> BotEntry view models (GUI type)
                var list = records.Select(r => new BotEntry
                {
                    Name = r.Name,
                    Game = r.Game,
                    Instance = r.Instance,
                    Script = r.Script ?? new DuckBot.Data.Models.ScriptModel { Name = r.Name ?? "New Script", Steps = new(), Variables = new() }
                }).ToList();

                vm.LoadBots(list);
            }
            catch (Exception ex)
            {
                vm.AddLogPublic($"Failed to load bots: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                var store = Host.Services.GetRequiredService<IBotStore>();
                var vm = Host.Services.GetRequiredService<BotViewModel>();

                // map BotEntry -> BotRecord for persistence
                var records = vm.GetSerializableBots().Select(b => new BotRecord
                {
                    Name = b.Name,
                    Game = b.Game,
                    Instance = b.Instance,
                    Script = b.Script
                }).ToList();

                store.SaveAsync(records).GetAwaiter().GetResult();
            }
            catch { /* best-effort */ }

            Host?.StopAsync().GetAwaiter().GetResult();
            Host?.Dispose();
            base.OnExit(e);
        }
    }
}