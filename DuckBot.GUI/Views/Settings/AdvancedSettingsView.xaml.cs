using System.Windows;
using System.Windows.Controls;
using DuckBot.Core.Services;

namespace DuckBot.GUI.Views.Settings
{
    public partial class AdvancedSettingsView : UserControl
    {
        public AdvancedSettingsView()
        {
            InitializeComponent();
            LoadValues();
        }

        private void LoadValues()
        {
            var s = SettingsManager.Current.Advanced;
            RestartReconnected.IsChecked = s.RestartReconnectedInstances;
            KillBridge.IsChecked = s.KillBridgeIfOffline;
            RestartGame.IsChecked = s.RestartGameIfHomeScreen;
            SkipResCheck.IsChecked = s.SkipResolutionCheck;
            SkipUpdates.IsChecked = s.SkipBotUpdatesDuringRun;
            RestartAfterTimeout.IsChecked = s.RestartAfterScriptTimeout;
            RemoveCooldown.IsChecked = s.RemoveCooldownScripts;
            CloseUnused.IsChecked = s.CloseUnusedEmulators;
            DelayBox.Text = s.ConditionCheckDelayMs.ToString();
            ManifestUrl.Text = s.UpdateManifestUrl;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var s = SettingsManager.Current.Advanced;
            s.RestartReconnectedInstances = RestartReconnected.IsChecked == true;
            s.KillBridgeIfOffline = KillBridge.IsChecked == true;
            s.RestartGameIfHomeScreen = RestartGame.IsChecked == true;
            s.SkipResolutionCheck = SkipResCheck.IsChecked == true;
            s.SkipBotUpdatesDuringRun = SkipUpdates.IsChecked == true;
            s.RestartAfterScriptTimeout = RestartAfterTimeout.IsChecked == true;
            s.RemoveCooldownScripts = RemoveCooldown.IsChecked == true;
            s.CloseUnusedEmulators = CloseUnused.IsChecked == true;
            if (int.TryParse(DelayBox.Text, out int val)) s.ConditionCheckDelayMs = val;
            if (!string.IsNullOrWhiteSpace(ManifestUrl.Text)) s.UpdateManifestUrl = ManifestUrl.Text;
            SettingsManager.Save();
            MessageBox.Show("Advanced settings saved.", "DuckBot");
        }
    }
}
