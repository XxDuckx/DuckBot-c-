using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DuckBot.Core.Services;

namespace DuckBot.GUI.Views.Settings
{
    public partial class GeneralSettingsView : UserControl
    {
        public GeneralSettingsView()
        {
            InitializeComponent();
            LoadValues();
        }

        private void LoadValues()
        {
            var s = SettingsManager.Current.General;
            SetComboSelection(EmulatorSelect, s.Emulator);
            AskOnStart.IsChecked = s.AskEmulatorOnStart;
            RestartOnBoot.IsChecked = s.RestartOnBoot;
            AutoSignIn.IsChecked = s.AutoSignInLastAccount;
            ClearCache.IsChecked = s.ClearCache;
            CloseOnStop.IsChecked = s.CloseEmulatorsOnStop;
            SetComboSelection(ThemeSelect, s.Theme);
            AutoSave.IsChecked = s.AutoSaveConfigs;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var s = SettingsManager.Current.General;
            s.Emulator = (EmulatorSelect.SelectedItem as ComboBoxItem)?.Content.ToString() ?? EmulatorSelect.Text;
            s.AskEmulatorOnStart = AskOnStart.IsChecked == true;
            s.RestartOnBoot = RestartOnBoot.IsChecked == true;
            s.AutoSignInLastAccount = AutoSignIn.IsChecked == true;
            s.ClearCache = ClearCache.IsChecked == true;
            s.CloseEmulatorsOnStop = CloseOnStop.IsChecked == true;
            s.Theme = (ThemeSelect.SelectedItem as ComboBoxItem)?.Content.ToString() ?? ThemeSelect.Text;
            s.AutoSaveConfigs = AutoSave.IsChecked == true;
            SettingsManager.Save();
            MessageBox.Show("General settings saved.", "DuckBot", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void SetComboSelection(ComboBox combo, string value)
        {
            var item = combo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => string.Equals(i.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase));
            if (item != null)
                combo.SelectedItem = item;
            else
                combo.Text = value;
        }

        private void DownloadTool_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/XxDuckx/DuckBot",
                UseShellExecute = true
            });
        }

        private void ClearCooldowns_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Cooldown clearing will be integrated with the runtime in a future update.", "DuckBot");
        }
    }
}
