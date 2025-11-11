using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DuckBot.Core.Services;
using DuckBot.Data.Models;
using DuckBot.Data.Storage;
using DuckBot.Core.Scripts;

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
            var placeholder = ScreenshotService.GeneratePlaceholder($"{row.Name} @ {DateTime.Now:HH:mm:ss}");
            Shot.Source = placeholder;
            var bot = BotStore.LoadAll().FirstOrDefault(b => b.Id == row.Id);
            if (bot != null)
            {
                var fileName = ImageManager.SaveCrop(placeholder, bot.Game, "screenshot");
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