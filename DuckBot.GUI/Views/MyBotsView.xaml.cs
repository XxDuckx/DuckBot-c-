using DuckBot.Core.Emu;
using DuckBot.Core.Services;
using DuckBot.Data.Models;
using DuckBot.Data.Storage;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DuckBot.GUI.Views
{
    public partial class MyBotsView : UserControl
    {
        public ObservableCollection<BotProfile> Bots { get; } = new();
        public ObservableCollection<string> AvailableInstances { get; } = new();

        public MyBotsView()
        {
            InitializeComponent();
            DataContext = this;

            // Load bots and rebuild instance lock table
            foreach (var b in BotStore.LoadAll()) Bots.Add(b);
            InstanceRegistry.Current.RebuildFromBots(Bots.Select(b => (b.Instance, b.Id)));

            RefreshInstancesUi();
            BotGrid.ItemsSource = Bots;
        }

        private void RefreshInstancesUi()
        {
            AvailableInstances.Clear();
            foreach (var name in AdbService.ListInstances()) AvailableInstances.Add(name);
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
                var win = new BotEditorWindow(bot);
                win.Owner = Window.GetWindow(this);
                if (win.ShowDialog() == true)
                {
                    // saved in dialog
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
            var sel = BotGrid.SelectedItems.Cast<BotProfile>().ToList();
            if (sel.Count == 0) return;
            if (MessageBox.Show($"Delete {sel.Count} bot(s)?", "DuckBot", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            foreach (var b in sel)
            {
                InstanceRegistry.Current.ReleaseByBot(b.Id);
                BotStore.Delete(b);
                Bots.Remove(b);
            }
        }
        private void StartSelected_Click(object sender, RoutedEventArgs e)
        {
            foreach (BotProfile bot in BotGrid.SelectedItems)
            {
                if (string.IsNullOrWhiteSpace(bot.Instance))
                {
                    LogService.Warn($"Bot '{bot.Name}' has no instance assigned.");
                    continue;
                }

                if (BotRunnerService.IsRunning(bot.Id))
                {
                    LogService.Warn($"Bot '{bot.Name}' already running.");
                    continue;
                }

                BotRunnerService.Start(bot);
                LogService.Info($"Started {bot.Name}");
            }
            BotGrid.Items.Refresh();
        }

        private void StopSelected_Click(object sender, RoutedEventArgs e)
        {
            foreach (BotProfile bot in BotGrid.SelectedItems)
            {
                if (!BotRunnerService.IsRunning(bot.Id))
                {
                    LogService.Warn($"Bot '{bot.Name}' not running.");
                    continue;
                }

                BotRunnerService.Stop(bot);
            }
            BotGrid.Items.Refresh();
        }


        private void QuickLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (BotGrid.SelectedItem is BotProfile bot && !string.IsNullOrWhiteSpace(bot.Instance))
            {
                bool ok = AdbService.LaunchInstance(bot.Instance);
                MessageBox.Show(ok
                    ? $"Quick launched {bot.Instance}"
                    : $"Could not launch {bot.Instance}");
            }
            else
            {
                MessageBox.Show("Select a bot with an assigned instance first.");
            }
        }

        private void FixEmulators_Click(object sender, RoutedEventArgs e)
        {
            AdbService.Refresh();
            RefreshInstancesUi();
            MessageBox.Show("Emulator list refreshed.");
        }

        private void Instance_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.DataContext is BotProfile bot)
            {
                var chosen = cb.SelectedItem as string ?? "";
                // try reserve
                if (!InstanceRegistry.Current.TryReserve(chosen, bot.Id))
                {
                    MessageBox.Show($"Instance '{chosen}' is already used by another bot.", "DuckBot");
                    // revert UI to previous value
                    cb.SelectedItem = bot.Instance;
                    return;
                }
                // release previous, assign new
                InstanceRegistry.Current.ReleaseByBot(bot.Id);
                bot.Instance = chosen;
                InstanceRegistry.Current.TryReserve(chosen, bot.Id);
                BotStore.Save(bot);
            }
        }

        private void RefreshInst_Click(object sender, RoutedEventArgs e) => RefreshInstancesUi();

        private void QuickEditor_Click(object sender, RoutedEventArgs e)
        {
            if (BotGrid.SelectedItem is BotProfile bot)
            {
                var win = new BotEditorWindow(bot);
                win.Owner = Window.GetWindow(this);
                if (win.ShowDialog() == true)
                {
                    InstanceRegistry.Current.RebuildFromBots(Bots.Select(b => (b.Instance, b.Id)));
                    BotGrid.Items.Refresh();
                }
            }
        }

        private void PlayBot_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is BotProfile bot)
            {
                if (string.IsNullOrWhiteSpace(bot.Instance))
                {
                    MessageBox.Show("Assign an instance before starting this bot.", "DuckBot");
                    return;
                }
                if (BotRunnerService.IsRunning(bot.Id))
                {
                    MessageBox.Show("This bot is already running.", "DuckBot");
                    return;
                }
                BotRunnerService.Start(bot);
                LogService.Info($"Started bot {bot.Name}");
            }
        }

        private void StopBot_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is BotProfile bot)
            {
                if (!BotRunnerService.IsRunning(bot.Id))
                {
                    MessageBox.Show("This bot is not currently running.", "DuckBot");
                    return;
                }
                BotRunnerService.Stop(bot);
                LogService.Info($"Stopped bot {bot.Name}");
            }
        }
    }
}
