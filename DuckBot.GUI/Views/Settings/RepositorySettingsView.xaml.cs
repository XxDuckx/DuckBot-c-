using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DuckBot.Core.Services;
using Microsoft.Win32;

namespace DuckBot.GUI.Views.Settings
{
    public partial class RepositorySettingsView : UserControl
    {
        public RepositorySettingsView()
        {
            InitializeComponent();
            _ = LoadTemplatesAsync();
        }

        private async Task LoadTemplatesAsync()
        {
            TemplateList.Items.Clear();
            TemplateList.Items.Add("Loading...");
            var templates = await RepositoryService.GetTemplatesAsync();
            TemplateList.Items.Clear();
            foreach (var t in templates)
                TemplateList.Items.Add($"{t.Name} ({t.Game}) - by {t.Author}");
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadTemplatesAsync();
        }

        private void ContributeBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Script Templates (*.json)|*.json",
                Title = "Select template to share"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    RepositoryService.Contribute(dialog.FileName);
                    MessageBox.Show("Template copied to local contribution folder.", "DuckBot");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not add template: {ex.Message}", "DuckBot");
                }
            }
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/XxDuckx/DuckBot",
                UseShellExecute = true
            });
        }
    }
}