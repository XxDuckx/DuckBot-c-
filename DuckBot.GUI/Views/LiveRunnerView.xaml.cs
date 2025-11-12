using DuckBot.Core.Services;
using DuckBot.Data.Models;
using DuckBot.Data.Scripts;
using DuckBot.Data.Storage;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
        private string? _lastScreenshotPath;

        public LiveRunnerView()
        {
            InitializeComponent();
            RunningGrid.ItemsSource = Items;
            RefreshTable();
        }

        private void RefreshTable()
        {
            Items.Clear();
            var bots = BotStore.LoadAll();

            foreach (var bot in bots)
            {
                var running = BotRunnerService.IsRunning(bot);
                Items.Add(new RunningRow
                {
                    Id = bot.Id,
                    Name = bot.Name,
                    Game = bot.Game,
                    Instance = bot.Instance,
                    Status = running ? "Running" : "Stopped"
                });
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshTable();
        }

        private void StopSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = RunningGrid.SelectedItems.Cast<RunningRow>().ToList();
            if (selected.Count == 0) return;

            foreach (var row in selected)
            {
                var bot = BotStore.LoadAll().FirstOrDefault(b => b.Id == row.Id);
                if (bot != null)
                    BotRunnerService.Stop(bot);
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

            var capture = ScreenshotService.CaptureOrPlaceholder(row.Instance, $"{row.Name} @ {DateTime.Now:HH:mm:ss}");
            Shot.Source = capture;

            var bot = BotStore.LoadAll().FirstOrDefault(b => b.Id == row.Id);
            if (bot != null)
            {
                string fileName = ImageManager.SaveCrop(capture, bot.Game, "screenshot");
                _lastScreenshotPath = Path.Combine(ImageManager.GetImageDir(bot.Game), fileName);
                ShotInfo.Text = $"Instance: {row.Instance} • Saved {fileName}";
            }
            else
            {
                _lastScreenshotPath = null;
                ShotInfo.Text = $"Instance: {row.Instance}";
            }
        }

        private void StopAll_Click(object sender, RoutedEventArgs e)
        {
            BotRunnerService.StopAll();
            RefreshTable();
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            // If we have a saved screenshot, open its folder
            if (!string.IsNullOrWhiteSpace(_lastScreenshotPath) && File.Exists(_lastScreenshotPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer",
                    Arguments = $"/select,\"{Path.GetFullPath(_lastScreenshotPath)}\"",
                    UseShellExecute = true
                });
                return;
            }

            // Otherwise open the current bot's image folder
            if (RunningGrid.SelectedItem is RunningRow row)
            {
                var bot = BotStore.LoadAll().FirstOrDefault(b => b.Id == row.Id);
                if (bot != null)
                {
                    string dir = ImageManager.GetImageDir(bot.Game);
                    Directory.CreateDirectory(dir);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer",
                        Arguments = Path.GetFullPath(dir),
                        UseShellExecute = true
                    });
                }
            }
        }
    }
}
