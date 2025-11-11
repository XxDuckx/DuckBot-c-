using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DuckBot.Core.Models;
using DuckBot.Core.Services;

namespace DuckBot.GUI.Views
{
    public partial class UpdatesView : UserControl
    {
        private UpdateManifest? _manifest;

        public UpdatesView()
        {
            InitializeComponent();
            CurrentVersionText.Text = UpdateService.CurrentVersion.ToString();
        }

        private async void Check_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdatesAsync();
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                StatusText.Text = "Checking for updates...";
                string manifestUrl = SettingsManager.Current.Advanced.UpdateManifestUrl;
                var result = await UpdateService.CheckForUpdatesAsync(manifestUrl);
                _manifest = result.Manifest;

                LatestVersionText.Text = result.Manifest.Version;
                ReleaseDateText.Text = result.Manifest.PublishedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                ReleaseNotes.Text = string.IsNullOrWhiteSpace(result.Manifest.Notes)
                    ? "No release notes provided."
                    : result.Manifest.Notes;

                DownloadButton.IsEnabled = result.UpdateAvailable && !string.IsNullOrWhiteSpace(result.Manifest.PackageUrl);
                NotesButton.IsEnabled = !string.IsNullOrWhiteSpace(result.Manifest.NotesUrl);

                StatusText.Text = result.UpdateAvailable
                    ? (result.Supported ? "Update available." : "New update requires a newer DuckBot build.")
                    : "You are running the latest version.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Update check failed: {ex.Message}";
            }
        }

        private async void Download_Click(object sender, RoutedEventArgs e)
        {
            if (_manifest == null)
            {
                await CheckForUpdatesAsync();
                if (_manifest == null) return;
            }

            try
            {
                StatusText.Text = "Downloading update...";
                string targetDir = Path.Combine("data", "updates");
                Directory.CreateDirectory(targetDir);
                var progress = new Progress<double>(p => StatusText.Text = $"Downloading update... {(int)(p * 100)}%");
                string? file = await UpdateService.DownloadPackageAsync(_manifest, targetDir, progress);
                if (file != null)
                {
                    StatusText.Text = $"Downloaded to {file}";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer",
                        Arguments = $"/select,\"{Path.GetFullPath(file)}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    StatusText.Text = "Manifest does not provide a download URL.";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Download failed: {ex.Message}";
            }
        }

        private void OpenNotes_Click(object sender, RoutedEventArgs e)
        {
            if (_manifest == null || string.IsNullOrWhiteSpace(_manifest.NotesUrl))
            {
                StatusText.Text = "No release notes URL available.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = _manifest.NotesUrl,
                UseShellExecute = true
            });
        }
    }
}
