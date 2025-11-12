using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DuckBot.Core.Infrastructure;
using DuckBot.Core.Logging;
using DuckBot.Core.Services;
using DuckBot.Data.Models;
using DuckBot.Data.Scripts;
using DuckBot.Data.Storage;

namespace DuckBot.GUI.Views
{
    public partial class LiveRunnerView : UserControl
    {
        public class RunningRow
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Game { get; set; } = string.Empty;
            public string Instance { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }

        public ObservableCollection<RunningRow> Items { get; } = new();
        private string? _lastScreenshotPath;
        private readonly IBotRunnerService _botRunner;
        private readonly IAppLogger _logger;

        public LiveRunnerView()
        {
            InitializeComponent();
            AppServices.ConfigureDefaults();
            _botRunner = AppServices.BotRunner;
            _logger = AppServices.Logger;
            RunningGrid.ItemsSource = Items;
            RefreshTable();
        }

        private void RefreshTable()
        {
            Items.Clear();
            var bots = BotStore.LoadAll();
            foreach (var bot in bots)
            {
                var running = _botRunner.IsRunning(bot.Id);
                var status = _botRunner.GetStatus(bot.Id);
                Items.Add(new RunningRow
                {
                    Id = bot.Id,
                    Name = bot.Name,
                    Game = bot.Game,
                    Instance = bot.Instance,
                    Status = running ? status : "Stopped"
                });
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshTable();

        private async void StopSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = RunningGrid.SelectedItems.Cast<RunningRow>().ToList();
            if (selected.Count == 0) return;

            var bots = BotStore.LoadAll().ToDictionary(b => b.Id, b => b);
            foreach (var row in selected)
            {
                if (bots.TryGetValue(row.Id, out var bot))
                {
                    await _botRunner.StopAsync(bot);
                    _logger.Info($"Stopped bot {bot.Name} from Live Runner.");
                }
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

        private async void StopAll_Click(object sender, RoutedEventArgs e)
        {
            await _botRunner.StopAllAsync();
            _logger.Info("Requested stop for all bots from Live Runner view.");
            RefreshTable();
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
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