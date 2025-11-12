using System;
using System.Windows;
using System.Windows.Threading;
using DuckBot.Core.Infrastructure;

namespace DuckBot.GUI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppServices.ConfigureDefaults();
            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            AppServices.Logger.Error($"Unhandled UI exception: {e.Exception.Message}");
            MessageBox.Show("An unexpected error occurred. Check the log output for details.", "DuckBot", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}