using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DuckBot.Core.Services;
using DuckBot.Data.Models;
using DuckBot.Data.Storage;

namespace DuckBot.GUI.Views
{
    public partial class LiveRunnerView : UserControl
    {
        public class RunningRow
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Game { get; set; } = "";
            public string Instance { get; set; } = "";
            public string Status { get; set; } = "";
        }

        public ObservableCollection<RunningRow> Items { get; } = new();

        public LiveRunnerView()
        {
            InitializeComponent();
            RunningGrid.ItemsSource = Items;
            RefreshTable();
        }

        private void RefreshTable()
        {
            Items.Clear();
            foreach (var bot in BotStore.LoadAll())
            {
                var status = BotRunnerService.GetStatus(bot.Id);
                if (status != "Idle" || BotRunnerService.IsRunning(bot.Id))
                {
                    Items.Add(new RunningRow
                    {
                        Id = bot.Id,
                        Name = bot.Name,
                        Game = bot.Game,
                        Instance = bot.Instance,
                        Status = status
                    });
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshTable();

        private void StopSelected_Click(object sender, RoutedEventArgs e)
        {
            var sel = RunningGrid.SelectedItems.Cast<RunningRow>().ToList();
            if (sel.Count == 0) return;
            foreach (var row in sel)
            {
                var bot = BotStore.LoadAll().FirstOrDefault(b => b.Id == row.Id);
                if (bot != null) BotRunnerService.Stop(bot);
            }
            RefreshTable();
        }

        private void Capture_Click(object sender, RoutedEventArgs e)
        {
            if (RunningGrid.SelectedItem is not RunningRow row)
            {
                MessageBox.Show("Select a running bot first.");
                return;
            }
            // Placeholder screenshot:
            Shot.Source = ScreenshotService.GeneratePlaceholder($"{row.Name} @ {DateTime.Now:HH:mm:ss}");
            ShotInfo.Text = $"Instance: {row.Instance}";
        }
    }
}
