using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DuckBot.Core.Infrastructure;
using DuckBot.Core.Logging;
using DuckBot.Core.Services;

namespace DuckBot.GUI.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly string _username;
        private readonly IAdbService _adbService;
        private readonly IAppLogger _logger;

        public DashboardView(string username)
        {
            InitializeComponent();
            AppServices.ConfigureDefaults();
            _username = username;
            _adbService = AppServices.AdbService;
            _logger = AppServices.Logger;
            WelcomeText.Text = $"Welcome, {_username}";
            Loaded += async (_, _) => await LoadInstancesAsync();
        }

        private async Task LoadInstancesAsync()
        {
            try
            {
                var instances = await _adbService.ListInstancesAsync(forceRefresh: true);
                InstanceList.ItemsSource = instances;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load emulator instances: {ex.Message}");
                InstanceList.ItemsSource = Array.Empty<string>();
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadInstancesAsync();
            MessageBox.Show("Instance list refreshed!", "DuckBot");
        }
    }
}