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
            EmulatorSelect.Text = s.Emulator;
            AskOnStart.IsChecked = s.AskEmulatorOnStart;
            RestartOnBoot.IsChecked = s.RestartOnBoot;
            AutoSignIn.IsChecked = s.AutoSignInLastAccount;
            ClearCache.IsChecked = s.ClearCache;
            CloseOnStop.IsChecked = s.CloseEmulatorsOnStop;
            ThemeSelect.Text = s.Theme;
            AutoSave.IsChecked = s.AutoSaveConfigs;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var s = SettingsManager.Current.General;
            s.Emulator = (EmulatorSelect.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "LDPlayer9";
            s.AskEmulatorOnStart = AskOnStart.IsChecked == true;
            s.RestartOnBoot = RestartOnBoot.IsChecked == true;
            s.AutoSignInLastAccount = AutoSignIn.IsChecked == true;
            s.ClearCache = ClearCache.IsChecked == true;
            s.CloseEmulatorsOnStop = CloseOnStop.IsChecked == true;
            s.Theme = (ThemeSelect.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Dark";
            s.AutoSaveConfigs = AutoSave.IsChecked == true;
            SettingsManager.Save();
            MessageBox.Show("General settings saved.", "DuckBot", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
