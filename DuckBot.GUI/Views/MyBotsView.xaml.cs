using DuckBot.Core.Emu;
using DuckBot.Core.Infrastructure;
using DuckBot.Core.Logging;
using DuckBot.Core.Services;
using DuckBot.Data.Models;
using DuckBot.Data.Storage;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DuckBot.GUI.Views
{
    public partial class MyBotsView : UserControl
    {
        public ObservableCollection<BotProfile> Bots { get; } = new();
        public ObservableCollection<string> AvailableInstances { get; } = new();

        private readonly IAdbService _adbService;
        private readonly IBotRunnerService _botRunner;
        private readonly IAppLogger _logger;

        public MyBotsView()
        {
            InitializeComponent();
            DataContext = this;

            AppServices.ConfigureDefaults();
            _adbService = AppServices.AdbService;
            _botRunner = AppServices.BotRunner;
            _logger = AppServices.Logger;

            foreach (var bot in BotStore.LoadAll()) Bots.Add(bot);
            InstanceRegistry.Current.RebuildFromBots(Bots.Select(b => (b.Instance, b.Id)));

            BotGrid.ItemsSource = Bots;
            Loaded += async (_, _) => await RefreshInstancesAsync(forceRefresh: true);
        }

        private async Task RefreshInstancesAsync(bool forceRefresh = false)
        {
            try
            {
                var instances = await _adbService.ListInstancesAsync(forceRefresh);
                AvailableInstances.Clear();
                foreach (var name in instances) AvailableInstances.Add(name);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to refresh emulator instances: {ex.Message}");
                MessageBox.Show("Unable to refresh LDPlayer instances. Check logs for details.", "DuckBot", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CreateBot_Click(object sender, RoutedEventArgs e)
        {
            var bot = new BotProfile { Name = "New Bot" };
            Bots.Add(bot);
            BotStore.Save(bot);
        }

        private void EditBot_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is BotProfile bot)
            {
                var win = new BotEditorWindow(bot)
                {
                    Owner = Window.GetWindow(this)
                };
                if (win.ShowDialog() == true)
                {
                    InstanceRegistry.Current.RebuildFromBots(Bots.Select(b => (b.Instance, b.Id)));
                    BotGrid.Items.Refresh();
                }
            }
        }

        private void DeleteBot_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is BotProfile bot)
            {
                if (MessageBox.Show($"Delete '{bot.Name}'?", "DuckBot", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    InstanceRegistry.Current.ReleaseByBot(bot.Id);
                    BotStore.Delete(bot);
                    Bots.Remove(bot);
                }
            }
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = BotGrid.SelectedItems.Cast<BotProfile>().ToList();
            if (selected.Count == 0) return;
            if (MessageBox.Show($"Delete {selected.Count} bot(s)?", "DuckBot", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            foreach (var bot in selected)
            {
                InstanceRegistry.Current.ReleaseByBot(bot.Id);
                BotStore.Delete(bot);
                Bots.Remove(bot);
            }
        }

        private async void StartSelected_Click(object sender, RoutedEventArgs e)
        {
            foreach (BotProfile bot in BotGrid.SelectedItems)
            {
                if (string.IsNullOrWhiteSpace(bot.Instance))
                {
                    _logger.Warn($"Bot '{bot.Name}' has no instance assigned.");
                    continue;
                }

                if (_botRunner.IsRunning(bot.Id))
                {
                    _logger.Warn($"Bot '{bot.Name}' already running.");
                    continue;
                }

                await _botRunner.StartAsync(bot);
                _logger.Info($"Started {bot.Name}");
            }
            BotGrid.Items.Refresh();
        }

        private async void StopSelected_Click(object sender, RoutedEventArgs e)
        {
            foreach (BotProfile bot in BotGrid.SelectedItems)
            {
                if (!_botRunner.IsRunning(bot.Id))
                {
                    _logger.Warn($"Bot '{bot.Name}' not running.");
                    continue;
                }

                await _botRunner.StopAsync(bot);
            }
            BotGrid.Items.Refresh();
        }

        private async void QuickLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (BotGrid.SelectedItem is BotProfile bot && !string.IsNullOrWhiteSpace(bot.Instance))
            {
                bool ok = await _adbService.LaunchInstanceAsync(bot.Instance);
                MessageBox.Show(ok ? $"Quick launched {bot.Instance}" : $"Could not launch {bot.Instance}", "DuckBot");
            }
            else
            {
                MessageBox.Show("Select a bot with an assigned instance first.", "DuckBot");
            }
        }

        private async void FixEmulators_Click(object sender, RoutedEventArgs e)
        {
            await RefreshInstancesAsync(forceRefresh: true);
            MessageBox.Show("Emulator list refreshed.", "DuckBot");
        }

        private async void RefreshInst_Click(object sender, RoutedEventArgs e)
            => await RefreshInstancesAsync(forceRefresh: true);

        private void Instance_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.DataContext is BotProfile bot)
            {
                var chosen = combo.SelectedItem as string ?? string.Empty;
                if (!InstanceRegistry.Current.TryReserve(chosen, bot.Id))
                {
                    MessageBox.Show($"Instance '{chosen}' is already used by another bot.", "DuckBot");
                    combo.SelectedItem = bot.Instance;
                    return;
                }

                InstanceRegistry.Current.ReleaseByBot(bot.Id);
                bot.Instance = chosen;
                InstanceRegistry.Current.TryReserve(chosen, bot.Id);
                BotStore.Save(bot);
            }
        }

        private void QuickEditor_Click(object sender, RoutedEventArgs e)
        {
            if (BotGrid.SelectedItem is BotProfile bot)
            {
                var win = new BotEditorWindow(bot)
                {
                    Owner = Window.GetWindow(this)
                };
                if (win.ShowDialog() == true)
                {
                    InstanceRegistry.Current.RebuildFromBots(Bots.Select(b => (b.Instance, b.Id)));
                    BotGrid.Items.Refresh();
                }
            }
        }

        private async void PlayBot_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is BotProfile bot)
            {
                if (string.IsNullOrWhiteSpace(bot.Instance))
                {
                    MessageBox.Show("Assign an instance before starting this bot.", "DuckBot");
                    return;
                }
                if (_botRunner.IsRunning(bot.Id))
                {
                    MessageBox.Show("This bot is already running.", "DuckBot");
                    return;
                }
                await _botRunner.StartAsync(bot);
                _logger.Info($"Started bot {bot.Name}");
            }
        }

        private async void StopBot_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is BotProfile bot)
            {
                if (!_botRunner.IsRunning(bot.Id))
                {
                    MessageBox.Show("This bot is not currently running.", "DuckBot");
                    return;
                }
                await _botRunner.StopAsync(bot);
                _logger.Info($"Stopped bot {bot.Name}");
            }
        }
    }
}